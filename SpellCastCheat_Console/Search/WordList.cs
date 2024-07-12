namespace SpellCastCheat_Console
{
    public class WordList
    {
        public List<string> Words { get; private set; }

        public WordList()
        {
            Words = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data/words.txt")).ToList();
        }
    }
}
