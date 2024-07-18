using SpellCastCheat.BusinessLogic;

public class Program
{
    private static readonly Dictionary<string, DateTime> _fileProcessTracker = new Dictionary<string, DateTime>();
    private static readonly object _lock = new object();
    private static AppConfig config;

    public static void Main(string[] args)
    {
        // Configure as you want for you
        config = new AppConfig()
        {
            WordFilteringMode = WordFilteringMode.Simple,
            ResultsCount = 3,
            SwapWordsMinLength = 6,
            SwapWordsMaxLength = 12,
            DoubleSwapWordsMinLength = 11,
            DoubleSwapWordsMaxLength = 16,
        };

        // Set path for putting SpellCast board screenshots
        string folderPath = Path.Combine(Environment.CurrentDirectory, "Images");
        Directory.CreateDirectory(folderPath); // Ensure the directory exists

        // Initialize FileSystemWatcher
        FileSystemWatcher watcher = new FileSystemWatcher
        {
            Path = folderPath,
            Filter = "*.*",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        watcher.Created += OnNewImage;

        // Prevent the application from closing
        Console.WriteLine("Listening for new images. Press 'q' to quit.");
        while (Console.Read() != 'q') { }
    }

    private static void OnNewImage(object source, FileSystemEventArgs e)
    {
        string[] supportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
        string fileExtension = Path.GetExtension(e.FullPath).ToLower();

        if (Array.Exists(supportedExtensions, extension => extension == fileExtension))
        {
            lock (_lock)
            {
                _fileProcessTracker[e.FullPath] = DateTime.Now;
            }

            Timer timer = new Timer(ProcessImage, e.FullPath, 500, Timeout.Infinite);
        }
    }

    private static void ProcessImage(object state)
    {
        string imagePath = (string)state;
        DateTime lastWriteTime;

        lock (_lock)
        {
            if (!_fileProcessTracker.TryGetValue(imagePath, out lastWriteTime))
            {
                return;
            }

            if ((DateTime.Now - lastWriteTime).TotalMilliseconds < 500)
            {
                // Ignore as it is too soon
                return;
            }

            _fileProcessTracker.Remove(imagePath);
        }

        try
        {
            var imageProcessor = new ImageService(imagePath);
            var grid = imageProcessor.ParseBoard();
            var board = new BoardModel(grid);
            var wordFinder = new WordSearchService(board, new WordListModel());

            board.DisplayBoard();

            // Find the best N words without swap
            config.WordSearchMode = WordSearchMode.Simple;
            var bestWords = wordFinder.Search(config);

            for (int i = 0; i < bestWords.Count; i++)
            {
                var wordResult = bestWords[i];
                Console.WriteLine($"Word {i + 1}: {wordResult.Word} with score: {wordResult.Score}");
            }

            // Save and open concatenated result image
            string outputImagePath = Path.Combine("Images/Results", $"no_swap_result_{DateTime.Now.Ticks}.png");
            imageProcessor.SaveResultsImages(outputImagePath, board.Grid, bestWords);

            // Find the best N words with swap
            config.WordSearchMode = WordSearchMode.Swap;
            bestWords = wordFinder.Search(config);

            for (int i = 0; i < bestWords.Count; i++)
            {
                var wordResult = bestWords[i];
                Console.WriteLine($"Word {i + 1}: {wordResult.Word} with score: {wordResult.Score}");
            }

            // Save and open concatenated result image
            outputImagePath = Path.Combine("Images/Results", $"swap_result_{DateTime.Now.Ticks}.png");
            imageProcessor.SaveResultsImages(outputImagePath, board.Grid, bestWords);

            // Find the best N words with double-swap
            //config.WordSearchMode = WordSearchMode.DoubleSwap;
            //bestWords = wordFinder.Search(config);

            //for (int i = 0; i < bestWords.Count; i++)
            //{
            //    var wordResult = bestWords[i];
            //    Console.WriteLine($"Word {i + 1}: {wordResult.Word} with score: {wordResult.Score}");
            //}

            //// Save and open concatenated result image
            //outputImagePath = Path.Combine("Images/Results", $"double_swap_result_{DateTime.Now.Ticks}.png");
            //imageProcessor.SaveResultsImages(outputImagePath, board.Grid, bestWords);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Can't process image, try to load another one. Ex: {ex.Message}");
        }
    }
}
