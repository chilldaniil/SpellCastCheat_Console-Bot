namespace SpellCastCheat.BusinessLogic
{
    public enum WordFilteringMode
    {
        /// <summary>
        /// Simple words filtering depends on existing letters on board
        /// </summary>
        Simple = 0,
        /// <summary>
        /// Advanced words filtering depends on existing letters and availability of swap some letter which isn't presented on board
        /// </summary>
        Advanced = 1
    }
}
