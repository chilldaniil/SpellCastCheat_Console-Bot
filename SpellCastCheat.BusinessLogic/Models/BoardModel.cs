namespace SpellCastCheat.BusinessLogic
{
    public class BoardModel
    {
        public LetterModel[,] Grid { get; set; }

        public BoardModel(LetterModel[,] grid)
        {
            Grid = grid;
        }

        public void DisplayBoard()
        {
            for (int row = 0; row < Grid.GetLength(0); row++)
            {
                for (int col = 0; col < Grid.GetLength(1); col++)
                {
                    Console.Write(Grid[row, col].GetName().PadRight(10));
                }
                Console.WriteLine();
            }
        }
    }
}
