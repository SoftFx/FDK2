namespace TickTrader.FDK.Common
{
    public enum OrderTimeInForce
    {
        Other = -1, 

        GoodTillCancel,

        ImmediateOrCancel,

        GoodTillDate,

        OneCancelsTheOther
    }
}
