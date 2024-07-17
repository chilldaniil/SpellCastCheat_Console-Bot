namespace SpellCastCheat.BusinessLogic
{
    public class AppConfig
    {
        public WordFilteringMode WordFilteringMode { get; set; }
        public WordSearchMode WordSearchMode { get; set; }
        public int ResultsCount { get; set; }
        public int SwapWordsMinLength { get; set; }
        public int SwapWordsMaxLength { get; set; }
        public int DoubleSwapWordsMinLength { get; set; }
        public int DoubleSwapWordsMaxLength { get; set; }

        public AppConfig()
        {
            WordFilteringMode = WordFilteringMode.Simple;
            ResultsCount = 3;
            SwapWordsMinLength = 6;
            SwapWordsMaxLength = 10;
            DoubleSwapWordsMinLength = 11;
            DoubleSwapWordsMaxLength = 16;
        }
    }
}
