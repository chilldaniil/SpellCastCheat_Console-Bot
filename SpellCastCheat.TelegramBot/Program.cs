using Microsoft.Extensions.Configuration;
using SpellCastCheat.BusinessLogic;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

class Program
{
    private static WordListModel WordList = new WordListModel();
    private static ITelegramBotClient botClient;
    private static AppConfig appConfig;

    private const int MaxImageSizeBytes = 3 * 1024 * 1024; // 3MB

    static async Task Main(string[] args)
    {
        var botConfig = LoadConfiguration();
        string token = botConfig["TelegramBotToken"];

        appConfig = new AppConfig()
        {
            WordFilteringMode = WordFilteringMode.Simple,
            ResultsCount = 1,
            SwapWordsMinLength = 6,
            SwapWordsMaxLength = 12,
            DoubleSwapWordsMinLength = 11,
            DoubleSwapWordsMaxLength = 16,
        };

        botClient = new TelegramBotClient(token);

        var me = await botClient.GetMeAsync();

        botClient.OnMessage += Bot_OnMessage;
        botClient.StartReceiving();

        Console.ReadKey();

        botClient.StopReceiving();
    }

    private static IConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("settings.json", optional: false, reloadOnChange: true);

        return builder.Build();
    }

    private static async void Bot_OnMessage(object sender, MessageEventArgs e)
    {
        if (e.Message.Type == MessageType.Text && e.Message.Text == "/start")
        {
            await botClient.SendTextMessageAsync(
                chatId: e.Message.Chat.Id,
                text: "Send me your board screen and I will find for you the best words"
            );
        }
        else if (e.Message.Type == MessageType.Photo)
        {
            var fileId = e.Message.Photo[^1].FileId; // Get the highest resolution photo
            var file = await botClient.GetFileAsync(fileId);

            if (file.FileSize > MaxImageSizeBytes)
            {
                await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat.Id,
                    text: "The image size is too large. Please send an image smaller than 3MB."
                );
                return;
            }

            using (var imageStream = new MemoryStream())
            {
                await botClient.DownloadFileAsync(file.FilePath, imageStream);
                imageStream.Seek(0, SeekOrigin.Begin);

                await ProcessImageAsync(e.Message.Chat.Id, imageStream);
            }
        }
    }

    private static async Task ProcessImageAsync(long chatId, Stream imageStream)
    {
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        using (var fileStream = new FileStream(tempImagePath, FileMode.Create, FileAccess.Write))
        {
            imageStream.CopyTo(fileStream);
        }

        var imageProcessor = new ImageService(tempImagePath);
        var grid = imageProcessor.ParseBoard();
        var board = new BoardModel(grid);
        var wordFinder = new WordSearchService(board, WordList);

        // Find the best words without swap
        appConfig.WordSearchMode = WordSearchMode.Simple;
        var bestWords = wordFinder.Search(appConfig);
        var bestWordNoSwap = bestWords.FirstOrDefault();

        if (bestWordNoSwap != null)
        {
            string formattedWordNoSwap = string.Join(" ", bestWordNoSwap.Word.ToUpper().ToCharArray());
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Best word without swap is *\"{formattedWordNoSwap}\"* with score *{bestWordNoSwap.Score}*",
                parseMode: ParseMode.Markdown
            );

            var noSwapResultImage = GenerateResultImage(imageProcessor, board.Grid, new List<WordResultDTO> { bestWordNoSwap });
            await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(noSwapResultImage)
            );
        }

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Give me 10 seconds to provide results with a single swap..."
        );

        // Find the best word with a single swap
        appConfig.WordSearchMode = WordSearchMode.Swap;
        var bestWordsWithSwap = wordFinder.Search(appConfig);
        var bestWordWithSwap = bestWordsWithSwap.FirstOrDefault();

        if (bestWordWithSwap != null)
        {
            string formattedWordWithSwap = string.Join(" ", bestWordWithSwap.Word.ToUpper().ToCharArray());
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Best word with single swap is *\"{formattedWordWithSwap}\"* with score *{bestWordWithSwap.Score}*",
                parseMode: ParseMode.Markdown
            );

            var swapResultImage = GenerateResultImage(imageProcessor, board.Grid, new List<WordResultDTO> { bestWordWithSwap });
            await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(swapResultImage)
            );
        }

        // Clean up temporary files
        File.Delete(tempImagePath);
    }

    private static Stream GenerateResultImage(ImageService imageProcessor, LetterModel[,] grid, List<WordResultDTO> results)
    {
        string outputImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_result.png");
        imageProcessor.SaveResultsImages(outputImagePath, grid, results, false, false);

        var resultStream = new MemoryStream(File.ReadAllBytes(outputImagePath));
        File.Delete(outputImagePath);

        resultStream.Seek(0, SeekOrigin.Begin);
        return resultStream;
    }
}
