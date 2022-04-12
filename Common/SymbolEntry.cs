namespace TickTrader.FDK.Common
{
    public struct SymbolEntry
    {
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ushort MarketDepth { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Id={0}; MarketDepth={1};", Id, MarketDepth);
        }
    }
}
