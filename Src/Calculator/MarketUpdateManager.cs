using System;
using System.Collections.Generic;
using System.Linq;
using TickTrader.FDK.Calculator.Netting;

namespace TickTrader.FDK.Calculator
{
    sealed class MarketUpdateManager
    {
        readonly IDictionary<string, HashSet<SymbolNetting>> nettingMap = new Dictionary<string, HashSet<SymbolNetting>>();

        public MarketUpdateManager(NettingCalculationTypes type)
        {
            this.CalculationType = type;
        }

        public NettingCalculationTypes CalculationType { get; private set; }

        public IEnumerable<IMarginAccountInfo> Update(string symbol)
        {
            var nettings = this.nettingMap.GetOrAdd(symbol);

            foreach (var netting in nettings)
                netting.Recalculate(UpdateKind.QuoteUpdated);

            var affectedAccounts = nettings.Select(o => o.AccountInfo).ToList();
            return affectedAccounts;
        }

        public void Register(SymbolNetting netting)
        {
            foreach (var symbol in netting.DependOnSymbols.Distinct())
                this.nettingMap.GetOrAdd(symbol).Add(netting);
        }

        public void Unregister(SymbolNetting netting)
        {
            foreach (var symbol in netting.DependOnSymbols.Distinct())
                this.nettingMap.GetOrAdd(symbol).Remove(netting);
        }
    }
}
