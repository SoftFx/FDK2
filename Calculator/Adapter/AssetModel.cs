using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Adapter
{
    public class AssetModel : IAssetModel
    {
        public AssetModel(AssetInfo assetInfo)
        {
            Currency = assetInfo.Currency;
            Amount = (decimal)assetInfo.Balance;
        }

        public AssetModel(string currency)
        {
            Currency = currency;
        }

        public string Currency { get; }
        public decimal Amount { get; private set; }
        public decimal FreeAmount => Amount - Margin;
        public decimal Margin { get; set; }

        internal bool Update(decimal newAmount)
        {
            if (Amount != newAmount)
            {
                Amount = newAmount;
                return true;
            }
            return false;
        }
    }
}
