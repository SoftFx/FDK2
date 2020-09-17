using System;
using System.Collections.Generic;
using System.Linq;

namespace TickTrader.FDK.Calculator
{
    /// <summary>
    /// Manages state (configuration and rates) within accounts with the same configuration (group).
    /// Can be used as a child object of MarketManager or as stand-alone market state.
    /// </summary>
    public class MarketState : MarketStateBase
    {
        private readonly Dictionary<string, SymbolMarketNode> _smbMap = new Dictionary<string, SymbolMarketNode>();

        public MarketState(bool usageTracking = true) : base(usageTracking)
        {
        }

        public void Update(ISymbolRate rate)
        {
            var tracker = GetSymbolNode(rate.Symbol, true);
            tracker?.UpdateRate(rate);
            tracker?.FireChanged();
        }

        public void Update(IEnumerable<ISymbolRate> rates)
        {
            if (rates != null)
            {
                var affectedNodes = new HashSet<SymbolMarketNode>();

                foreach (ISymbolRate rate in rates)
                {
                    var tracker = GetSymbolNode(rate.Symbol, true);

                    if (tracker != null)
                    {
                        tracker.UpdateRate(rate);
                        affectedNodes.Add(tracker);
                    }
                }

                foreach (var node in affectedNodes)
                    node.FireChanged();
            }
        }

        public List<ISymbolRate> GetRatesSnapshot()
        {
            return _smbMap.Values.Select(s => s.Rate).Where(r => r != null).ToList();
        }

        internal override SymbolMarketNode GetSymbolNode(string smb, bool addIfMissing)
        {
            var smbNode = _smbMap.GetOrDefault(smb);

            if (smbNode == null && addIfMissing)
            {
                smbNode = new SymbolMarketNode(smb, null);
                _smbMap.Add(smb, smbNode);
            }

            return smbNode;
        }

        protected override IEnumerable<ISymbolInfo> ListEnabledNodes()
            => _smbMap.Values.Where(n => n.IsEnabled).Select(s => s.SymbolInfo);

        protected override void UpsertNode(ISymbolInfo smbInfo)
        {
            var node = _smbMap.GetOrDefault(smbInfo.Symbol);

            if (node == null)
            {
                node = new SymbolMarketNode(smbInfo.Symbol, smbInfo);
                _smbMap.Add(smbInfo.Symbol, node);
            }
            else
                node.Update(smbInfo);
        }

        protected override void DisableNode(ISymbolInfo smbInfo)
        {
            _smbMap.GetOrDefault(smbInfo.Symbol)?.Update(null);
        }

        protected override void OnCalculatorAdded(OrderCalculator calculator)
        {
            if (!UsageTrackingEnabled)
                calculator.AddUsage(); // add constant usage token
        }
    }
}
