namespace SpellCastCheat_Console
{
    public class LetterModel
    {
        public char Letter { get; set; }
        public int Points => GetLetterPoints(Letter) * (IsDoubleLetter ? 2 : 1) * (IsTripleLetter ? 3 : 1);
        public bool IsDoubleWord { get; set; }
        public bool IsDoubleLetter { get; set; }
        public bool IsTripleLetter { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int LetterCenterX { get; set; }
        public int LetterCenterY { get; set; }

        public LetterModel(char letter, int x, int y, bool isDoubleWord = false, bool isDoubleLetter = false, bool isTripleLetter = false)
        {
            Letter = letter;
            IsDoubleWord = isDoubleWord;
            IsDoubleLetter = isDoubleLetter;
            IsTripleLetter = isTripleLetter;
            X = x;
            Y = y;
        }

        private static int GetLetterPoints(char letter)
        {
            letter = char.ToUpper(letter);
            return letter switch
            {
                'A' or 'E' or 'I' or 'O' => 1,
                'N' or 'R' or 'S' or 'T' => 2,
                'D' or 'G' or 'L' => 3,
                'B' or 'H' or 'P' or 'M' or 'U' or 'Y' => 4,
                'C' or 'F' or 'V' or 'W' => 5,
                'K' => 6,
                'J' or 'X' => 7,
                'Q' or 'Z' => 8,
                _ => 0,
            };
        }

        public string GetName()
        {
            string special = "";
            if (IsDoubleWord) special += "2X ";
            if (IsDoubleLetter) special += "DL ";
            if (IsTripleLetter) special += "TL ";
            return $"{Letter} ({special.Trim()})";
        }
    }
}
