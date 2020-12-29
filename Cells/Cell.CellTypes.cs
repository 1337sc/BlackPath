namespace tgBot.Cells
{
    public abstract partial class Cell
    {
        public enum CellTypes
        {
            ErrType,
            Empty,
            GlanceTrap,
            EnterTrap,
            Char,
            Player,
            Darkness,
            Exit
        }
    }
}
