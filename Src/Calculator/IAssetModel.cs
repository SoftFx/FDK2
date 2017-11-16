namespace TickTrader.FDK.Calculator
{
    public enum AssetChangeTypes
    {
        Added,
        Removed,
        Replaced
    }

    public interface IAssetModel
    {
        string Currency { get; }
        decimal Amount { get; }
        decimal FreeAmount { get; }
        decimal LockedAmount { get; }
        decimal Margin { get; set; }
    }
}
