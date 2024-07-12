namespace SpellCastCheat_Console
{
    public enum WordSearchMode
    {
        /// <summary>
        /// Fast search of existing words - 100ms
        /// </summary>
        Simple = 0,
        /// <summary>
        /// Search for existing words with 1 letter swap availability - up to 15s
        /// </summary>
        Swap = 1,
        /// <summary>
        /// Idk why it's needed, but I saw another guy did it, so why am I worse?
        /// PS: Alright, that guy swaps two letters from board, not from all alphabet, after 10 minutes of calculation I've stopped the app.
        /// PSS: Did mix of swap one any letter from alphabet and one letter from board - about 7 minutes depends on searched word length configuration. But hudge words
        /// </summary>
        DoubleSwap = 2
    }
}
