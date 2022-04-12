namespace TickTrader.FDK.Calculator.Validation
{
    public class AssetsMovementParameters
    {
        public IOrderCalcInfo Order { get; set; }
        public decimal MarginMovement { get; set; }
        public decimal CommissionMovement { get; set; }
        public IAssetModel MarginAsset { get; set; }
        public IAssetModel CommissionAsset { get; set; }
    }
}