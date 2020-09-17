using System;
using System.Collections.Generic;
using System.Linq;
using TickTrader.FDK.Calculator.Netting;
using TickTrader.FDK.Calculator.Rounding;
using TickTrader.FDK.Common;
using TickTrader.FDK.Extended;

namespace TickTrader.FDK.Calculator.Adapter
{
    public class AccountAdapter : IMarginAccountInfo, ICashAccountInfo
    {
        private Func<string, SymbolModel> _symbolProvider;
        private AccountCalculator _marginCalculator;
        private CashAccountCalculator _cashCalculator;
        private readonly IDictionary<string, PositionAccessor> _positionsDict = new Dictionary<string, PositionAccessor>();
        private readonly IDictionary<string, OrderAccessor> _ordersDict = new Dictionary<string, OrderAccessor>();
        private readonly IDictionary<string, AssetModel> _assetsDict = new Dictionary<string, AssetModel>();
        private readonly HashSet<string> _unknownSymbols = new HashSet<string>();

        public AccountAdapter(Func<string, SymbolModel> symbolProvider)
        {
            _symbolProvider = symbolProvider;
        }

        #region Properties
        public CalcError CalcWorstError { get; set; }
        public decimal BalanceRounded => FinancialRounding.Instance.RoundProfit(RoundingDigits, Balance);
        public decimal EquityRounded => FinancialRounding.Instance.RoundProfit(RoundingDigits, _marginCalculator?.Equity ?? 0);
        public decimal MarginRounded => FinancialRounding.Instance.RoundMargin(RoundingDigits, _marginCalculator?.Margin ?? 0);
        public decimal MarginLevelRounded => FinancialRounding.Instance.RoundProfit(4, _marginCalculator?.MarginLevel ?? 0);
        public decimal ProfitRounded => FinancialRounding.Instance.RoundProfit(RoundingDigits, _marginCalculator?.Profit ?? 0);
        public decimal CommissionRounded => FinancialRounding.Instance.RoundMargin(RoundingDigits, _marginCalculator?.Commission ?? 0);
        public decimal SwapRounded => FinancialRounding.Instance.RoundMargin(RoundingDigits, _marginCalculator?.Swap ?? 0);
        public decimal AgentCommissionRounded => FinancialRounding.Instance.RoundMargin(RoundingDigits, 0m);

        public int RoundingDigits
        {
            get { return _marginCalculator?.RoundingDigits ?? AccountCalculator.DefaultRounding; }
        }

        #endregion Properties

        #region Interfaces implementation

        public decimal Balance { get; set; }

        public int Leverage { get; set; }

        public string BalanceCurrency { get; set; }

        public IEnumerable<IPositionModel> Positions => _positionsDict.Values;

        public AccountType AccountingType { get; set; }

        public IEnumerable<IOrderModel> Orders => _ordersDict.Values;

        public IEnumerable<IAssetModel> Assets => _assetsDict.Values;

        public event Action<PositionEssentialsChangeArgs> PositionChanged;
        public event Action<IOrderModel> OrderAdded;
        public event Action<IEnumerable<IOrderModel>> OrdersAdded;
        public event Action<IOrderModel> OrderRemoved;
        public event Action<IAssetModel, AssetChangeTypes> AssetsChanged;
        
        #endregion Interfaces implementation

        #region Methods
        public void InitCalculator(MarketState marketState)
        {
            try
            {
                if (AccountingType == AccountType.Gross || AccountingType == AccountType.Net)
                {
                    if (_marginCalculator == null)
                    {
                        _marginCalculator = new AccountCalculator(NettingCalculationTypes.OneByOne, this, marketState, true);
                        _marginCalculator.WorstErrorCodeChanged += Calc_WorstCalculationErrorChanged;
                        CalcWorstError = _marginCalculator.WorstError;
                    }
                    else
                    {
                        _marginCalculator.Market = marketState;
                    }
                }
                else
                {
                    if (_cashCalculator == null)
                    {
                        _cashCalculator = new CashAccountCalculator(this, marketState);
                    }
                    else
                    {
                        _cashCalculator.Market = marketState;
                    }
                }
            }
            catch (Exception e)
            {
                _marginCalculator = null;
                _cashCalculator = null;
            }
        }

        private void Calc_WorstCalculationErrorChanged(object sender, EventArgs e)
        {
            CalcWorstError = _marginCalculator.WorstError;
        }

        public void InitOrders(IEnumerable<TradeRecord> orders)
        {
            if (orders != null)
            {
                var orderModels = new List<IOrderModel>(orders.Count());
                foreach (var order in orders)
                {
                    var orderModel = new OrderAccessor(order, GetSymbolInfo(order.Symbol));
                    _ordersDict[orderModel.OrderId] = orderModel;
                    orderModels.Add(orderModel);
                }
                OrdersAdded?.Invoke(orderModels);
            }
        }

        public void InitPositions(IEnumerable<Position> positions)
        {
            if (positions != null)
            {
                foreach (var position in positions)
                {
                    ProcessUpdate(position);
                }
            }
        }

        public void InitAssets(AssetInfo[] assets)
        {
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    var assetModel = new AssetModel(assets[i]);
                    _assetsDict[assetModel.Currency] = assetModel;
                    AssetsChanged?.Invoke(assetModel, AssetChangeTypes.Added);
                }
            }
        }

        public void UpdateAssets(AssetInfo[] assets)
        {
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    AssetModel asset;
                    AssetInfo inputAsset = assets[i];
                    if (!_assetsDict.TryGetValue(inputAsset.Currency, out asset))
                    {
                        if (inputAsset.Balance > 0)
                        {
                            asset = new AssetModel(inputAsset);
                            _assetsDict[asset.Currency] = asset;
                            AssetsChanged?.Invoke(asset, AssetChangeTypes.Added);
                        }
                    }
                    else if (inputAsset.Balance <= 0)
                    {
                        if (_assetsDict.Remove(asset.Currency))
                            AssetsChanged?.Invoke(asset, AssetChangeTypes.Removed);
                    }
                    else
                    {
                        if (asset.Update((decimal) inputAsset.Balance))
                            AssetsChanged?.Invoke(asset, AssetChangeTypes.Replaced);
                    }
                }
            }
        }

        public void ProcessUpdate(TradeUpdate update)
        {
            if (update != null)
            {
                switch (update.TradeRecordUpdateAction)
                {
                    case UpdateActions.Added:
                        var newOrder = new OrderAccessor(update.NewRecord, GetSymbolInfo(update.NewRecord.Symbol));
                        _ordersDict[update.NewRecord.OrderId] = newOrder;
                        OrderAdded?.Invoke(newOrder);
                        break;

                    case UpdateActions.Removed:
                        if (_ordersDict.ContainsKey(update.OldRecord.OrderId))
                        {
                            OrderAccessor oldOrder = _ordersDict[update.OldRecord.OrderId];
                            _ordersDict.Remove(update.OldRecord.OrderId);
                            OrderRemoved?.Invoke(oldOrder);
                        }
                        break;

                    case UpdateActions.Replaced:
                        OrderAccessor order;
                        if (_ordersDict.TryGetValue(update.NewRecord.OrderId, out order))
                        {
                            order.Update(update.NewRecord);
                        }
                        else
                        {
                            var newOrder2 = new OrderAccessor(update.NewRecord, GetSymbolInfo(update.NewRecord.Symbol));
                            _ordersDict[update.NewRecord.OrderId] = newOrder2;
                            OrderAdded?.Invoke(newOrder2);
                        }
                        break;
                }

                if (update.NewBalance != null)
                {
                    Balance = (decimal)update.NewBalance.Value;
                }

                if (update.UpdatedAssets != null)
                {
                    UpdateAssets(update.UpdatedAssets);
                }
            }
        }

        public void ProcessUpdate(Position newPosition)
        {
            if (newPosition != null)
            {
                PositionAccessor position;
                decimal? oldLongAmount = null, oldLongPrice = null, oldShortAmount = null, oldShortPrice = null;
                if (_positionsDict.TryGetValue(newPosition.Symbol, out position))
                {
                    oldLongAmount = position.Long.Amount;
                    oldLongPrice = position.Long.Price;
                    oldShortAmount = position.Short.Amount;
                    oldShortPrice = position.Short.Price;
                    position.Update(newPosition);
                    if (position.IsEmpty)
                        _positionsDict.Remove(position.Symbol);
                }
                else
                {
                    position = new PositionAccessor(newPosition, Leverage, GetSymbolInfo(newPosition.Symbol));
                    if (!position.IsEmpty)
                        _positionsDict[position.Symbol] = position;
                }
                PositionChanged?.Invoke(new PositionEssentialsChangeArgs(position, oldLongAmount, oldLongPrice, oldShortAmount, oldShortPrice));
            }
        }

        public void Clear()
        {
            _ordersDict.Foreach(p => OrderRemoved?.Invoke(p.Value));
            _ordersDict.Clear();
            _positionsDict.Clear();
            _assetsDict.Clear();
            _unknownSymbols.Clear();

            if (_marginCalculator != null)
            {
                _marginCalculator?.Dispose();
                _marginCalculator = null;
                CalcWorstError = null;
            }

            if (_cashCalculator != null)
            {
                _cashCalculator?.Dispose();
                _cashCalculator = null;
            }
        }

        public TradeRecord[] GetOrdersCalculated()
        {
            var records = new List<TradeRecord>(_ordersDict.Count);
            if (AccountingType != AccountType.Cash)
            {
                foreach (var orderAccessor in _ordersDict.Values)
                {
                    var tradeRecord = orderAccessor.TradeRecord;
                    if (tradeRecord.Type == OrderType.Position)
                        tradeRecord.Profit = (double)FinancialRounding.Instance.RoundProfit(RoundingDigits, orderAccessor.Profit);
                    tradeRecord.Margin = (double)FinancialRounding.Instance.RoundMargin(RoundingDigits, orderAccessor.Margin);
                    records.Add(tradeRecord);
                }
            }
            else
            {
                foreach (var orderAccessor in _ordersDict.Values)
                {
                    var tradeRecord = orderAccessor.TradeRecord;
                    tradeRecord.Margin = (double)orderAccessor.CashMargin;
                    records.Add(tradeRecord);
                }
            }

            return records.ToArray();
        }

        public Position[] GetPositionsCalculated()
        {
            var positions = new List<Position>(_ordersDict.Count);
            foreach (var positionAccessor in _positionsDict.Values)
            {
                var position = positionAccessor.Position;
                position.Profit = (double)FinancialRounding.Instance.RoundProfit(RoundingDigits, positionAccessor.Profit);
                position.Margin = (double)FinancialRounding.Instance.RoundMargin(RoundingDigits, positionAccessor.Margin);
                positions.Add(position);
            }

            return positions.ToArray();
        }

        public IDictionary<string, Asset> GetAssetsCalculated(out CalcError error)
        {
            error = null;
            if (AccountingType != AccountType.Cash)
            {
                var marginAssets = _marginCalculator?.GetAssets(out error);
                if (marginAssets != null)
                {
                    return marginAssets;
                }
            }
            else
            {
                return _assetsDict.ToDictionary(p => p.Key, p => new Asset
                {
                    Currency = p.Value.Currency,
                    Volume = (double) p.Value.Amount,
                    LockedVolume = (double) p.Value.Margin,
                    DepositCurrency = (double) p.Value.Amount,
                    Rate = 1
                });
            }

            return new Dictionary<string, Asset>();
        }

        public string[] GetUnknownSymbols()
        {
            return _unknownSymbols.ToArray();
        }

        private ISymbolInfo GetSymbolInfo(string symbolName)
        {
            var symbolInfo = _symbolProvider?.Invoke(symbolName);
            if (symbolInfo == null && !_unknownSymbols.Contains(symbolName))
                _unknownSymbols.Add(symbolName);
            return symbolInfo;
        }
        #endregion Methods
    }
}
