namespace SpellCastCheat_Console
{
    public class WordResultDTO
    {
        public string Word { get; set; }
        public int Score { get; set; }
        public List<PathDTO> Path { get; set; }
        public List<SwapDTO> Swaps { get; set; }

        public WordResultDTO()
        {
            Swaps = new List<SwapDTO>();
            Path = new List<PathDTO>();
            Word = string.Empty;
            Score = 0;
        }
    }
}
