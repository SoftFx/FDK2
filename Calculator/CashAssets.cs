﻿   
namespace TickTrader.FDK.Calculator
{
    using System.Collections.Generic;
    using System.Linq;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Calculator.Adapter;

    sealed class CashAssets
    {
        readonly IDictionary<string, Asset> assets;

        public CashAssets(IEnumerable<Common.AssetInfo> assets, AccountEntry account, MarketState state)
        {
            var query = assets.Select(o => new Asset(account)
            {
                Currency = o.Currency,
                Volume = o.Balance,
                LockedVolume = o.LockedAmount,
                DepositCurrency = o.Balance,
                Rate = 1
            });

            this.assets = query.ToDictionary(o => o.Currency);

            this.CalculateLockedVolume(account, state);
        }

        void CalculateLockedVolume(AccountEntry account, MarketState state)
        {
            foreach (var asset in this.assets.Values)
                asset.LockedVolume = 0D;

            var cashAccount = new CashAccountInfo(account, this.assets.Values);

            var orders = cashAccount.Orders.ToList();

            using (var calculator = new CashAccountCalculator(cashAccount, state))
            {
            }
        }

        public IDictionary<string, Asset> AsDictionary()
        {
            return this.assets;
        }
    }
}