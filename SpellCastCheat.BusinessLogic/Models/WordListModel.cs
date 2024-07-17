namespace SpellCastCheat.BusinessLogic
{
    public class WordListModel
    {
        public List<string> Words { get; private set; }

        public WordListModel()
        {
            Words = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data/words.txt")).ToList();
        }
    }
}
