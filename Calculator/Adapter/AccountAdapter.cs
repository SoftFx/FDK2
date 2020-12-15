using System;
using System.Collections.Generic;
using System.Linq;
using TickTrader.FDK.Calculator.Netting;
using TickTrader.FDK.Calculator.Rounding;
using TickTrader.FDK.Calculator.Validation;
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

        public decimal MaxOverdraftAmount { get; set; }

        public string OverdraftCurrency { get; set; }

        public int OverdraftCurrencyPrecision { get; set; }

        public string TokenCommissionCurrency { get; set; }

        public double? TokenCommissionCurrencyDiscount { get; set; }

        public bool IsTokenCommissionEnabled { get; set; }

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
                        if (!inputAsset.IsEmpty())
                        {
                            asset = new AssetModel(inputAsset);
                            _assetsDict[asset.Currency] = asset;
                            AssetsChanged?.Invoke(asset, AssetChangeTypes.Added);
                        }
                    }
                    else if (inputAsset.IsEmpty())
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

        public decimal? TryCalculateOverdraft(out string error)
        {
            error = null;
            var overdraft = _cashCalculator?.TryCalculateOverdraft(out error);
            if (overdraft != null)
            {
                overdraft = FinancialRounding.Instance.RoundMargin(OverdraftCurrencyPrecision, overdraft.Value);
            }
            return overdraft;
        }

        #region Validate orders

        public void ValidateNewOrderMargin(IOrderCalcInfo newOrder)
        {
            var orderInstance = new OrderLightClone(newOrder);
            if (AccountingType == AccountType.Gross || AccountingType == AccountType.Net)
            {
                var calculator = _marginCalculator;
                if (calculator != null)
                {
                    OrderCalculator fCalc = calculator.Market.GetCalculator(orderInstance.Symbol, BalanceCurrency);
                    orderInstance.Margin = fCalc.CalculateMargin(orderInstance, Leverage, out var error);
                    BusinessLogicException.ThrowIfError(error);
                    var sufficient = calculator.HasSufficientMarginToOpenOrder(orderInstance, out var newMargin, out error);
                    BusinessLogicException.ThrowIfError(error);
                    if (!sufficient)
                        throw new NotEnoughMoneyException($"Not Enough Money. {ToMarginAccountInfoString()}, NewMargin={newMargin}");
                }
            }
            else
            {
                var calculator = _cashCalculator;
                if (calculator != null && !ShouldSkipNewOrderRequestValidation(orderInstance))
                {
                    var symbolInfo = _symbolProvider?.Invoke(orderInstance.Symbol);
                    if (symbolInfo == null)
                        throw new MarketConfigurationException("Symbol not found: " + orderInstance.Symbol);
                    var marginMovement = CashAccountCalculator.CalculateMargin(orderInstance, symbolInfo);
                    var marginAssetCurrency = calculator.GetMarginAssetCurrency(symbolInfo, orderInstance.Side);
                    var marginAsset = _assetsDict.GetOrDefault(marginAssetCurrency) ?? new AssetModel(marginAssetCurrency);
                    var marginCurrency = calculator.Market.GetCurrencyOrThrow(marginAsset.Currency);

                    if (orderInstance.Side == OrderSide.Buy)
                    {
                        marginMovement = FinancialRounding.Instance.RoundMargin(marginCurrency.Precision, marginMovement);
                    }

                    decimal commissMovement = CalculateCommissionMovement(orderInstance, symbolInfo, calculator, out var commissAsset);
                    var sufficient = calculator.HasSufficientAssetsToOpenOrder(orderInstance, marginMovement, commissMovement, marginAsset, commissAsset);
                    if (!sufficient)
                        throw new NotEnoughMoneyException($"Not Enough Money");
                }
            }
        }

        public void ValidateModifyOrderMargin(ModifyOrderRequest request)
        {
            var orderToModify = _ordersDict.GetOrDefault(request.OrderId);
            if (orderToModify == null)
                throw new ArgumentException($"Order with Id = {request.OrderId} wasn't found");

            var remainingAmount = orderToModify.RemainingAmount;
            var maxVisibleAmount = (decimal?)orderToModify.TradeRecord.MaxVisibleVolume;

            if (request.AmountChange.HasValue && orderToModify.TradeRecord.IsPendingOrder)
            {
                remainingAmount = orderToModify.RemainingAmount + request.AmountChange.Value;
            }

            if (remainingAmount <= 0)
                return;

            if ((orderToModify.Type == OrderType.Limit || orderToModify.Type == OrderType.StopLimit) && request.MaxVisibleAmount.HasValue)
            {
                maxVisibleAmount = request.MaxVisibleAmount.Value < 0 ? default(decimal?) : request.MaxVisibleAmount.Value;
            }

            if (AccountingType == AccountType.Gross || AccountingType == AccountType.Net)
            {
                var calculator = _marginCalculator;
                if (calculator != null)
                {
                    OrderCalculator fCalc = calculator.Market.GetCalculator(orderToModify.Symbol, BalanceCurrency);
                    decimal oldMargin = orderToModify.Margin;
                    decimal newMargin = fCalc.CalculateMargin(remainingAmount, Leverage, orderToModify.Type, orderToModify.Side, Extensions.IsHiddenOrder(maxVisibleAmount), out var error);
                    BusinessLogicException.ThrowIfError(error);
                    if (!calculator.CanIncreaseMarginBy(newMargin - oldMargin, orderToModify.Symbol, orderToModify.Side, out var newAccMargin))
                        throw new NotEnoughMoneyException($"Not Enough Money. {ToMarginAccountInfoString()}, NewMargin={newAccMargin}");
                }
            }
            else
            {
                var calculator = _cashCalculator;
                if (calculator != null)
                {
                    var price = request.Price ?? orderToModify.Price;
                    var stopPrice = request.StopPrice ?? orderToModify.StopPrice;

                    decimal oldMargin = orderToModify.CashMargin;
                    decimal newMargin = CashAccountCalculator.CalculateMargin(orderToModify.Type, remainingAmount, price, stopPrice, orderToModify.Side, orderToModify.SymbolInfo, Extensions.IsHiddenOrder(maxVisibleAmount));
                    decimal marginMovement = newMargin - oldMargin;
                    var sufficient = calculator.HasSufficientMarginToOpenOrder(orderToModify, marginMovement, out _);
                    if (!sufficient)
                        throw new NotEnoughMoneyException($"Not Enough Money");
                }
            }

        }

        private bool ShouldSkipNewOrderRequestValidation(IOrderCalcInfo order)
        {
            return AccountingType == AccountType.Cash &&
                MaxOverdraftAmount > 0 &&
                (order.Type == OrderType.Market || (order.Type == OrderType.Limit && order.ImmediateOrCancel));
        }

        #region Comission
        // It calculates commission and commission asset if it is not equal to destination currency
        // otherwise return 0 and null
        private decimal CalculateCommissionMovement(IOrderCalcInfo order, ISymbolInfo symbolInfo, CashAccountCalculator calculator, out AssetModel commissAsset)
        {
            if (order.InitialType != OrderType.Market && (order.InitialType != OrderType.Limit || !order.ImmediateOrCancel))
            {
                commissAsset = null;
                return 0;
            }

            var currency = calculator.Market.GetCurrencyOrThrow(order.Side == OrderSide.Buy ? symbolInfo.MarginCurrency : symbolInfo.ProfitCurrency);
            string commissCurrency = currency.Name;

            decimal amount = (order.Side == OrderSide.Buy) ? order.RemainingAmount : order.RemainingAmount * order.Price.Value;
            decimal commiss = CalculateCashCommission(symbolInfo, calculator, amount, false, ref commissCurrency);
            commiss = FinancialRounding.Instance.RoundProfit(currency.Precision, commiss);

            // Does not need to check commission movement when
            // commission and destination currencies are equal and commission is less than amount
            if (commissCurrency == currency.Name && Math.Abs(commiss) < amount)
            {
                commissAsset = null;
                return 0;
            }

            commissAsset = _assetsDict.GetOrDefault(commissCurrency) ?? new AssetModel(commissCurrency);

            return commiss;
        }

        private decimal CalculateCashCommission(ISymbolInfo symbolInfo, CashAccountCalculator calculator, decimal amount, bool isReduced, ref string commissCurrency)
        {
            decimal commiss = 0;
            decimal cmsValue = isReduced
                ? (decimal)symbolInfo.LimitsCommission
                : (decimal)symbolInfo.Commission;
            commiss = CalculateCashCommission(amount, cmsValue, symbolInfo.CommissionType);

            commiss = ApplyTokenCommission(calculator, commiss, ref commissCurrency);
            commiss = ApplyMinimalCommission(symbolInfo, calculator, commiss, commissCurrency);
            return commiss;
        }

        private decimal CalculateCashCommission(decimal amount, decimal cmsValue, CommissionType cmsType)
        {
            if (cmsType == CommissionType.Percent ||
                cmsType == CommissionType.PercentageWaivedCash ||
                cmsType == CommissionType.PercentageWaivedEnhanced)
                return -(amount * cmsValue / 100M);

            return 0;
        }

        private decimal ApplyTokenCommission(CashAccountCalculator calculator, decimal commiss, ref string commissCurrency)
        {
            var tokenAsset = _assetsDict.GetOrDefault(TokenCommissionCurrency);

            if (commiss == 0 || !IsTokenCommissionEnabled || string.IsNullOrEmpty(TokenCommissionCurrency) || !TokenCommissionCurrencyDiscount.HasValue || tokenAsset == null)
                return commiss;

            decimal tokenCommissDiscount = (decimal)TokenCommissionCurrencyDiscount.Value;
            var conversion = calculator.Market.ConversionMap.GetNegativeAssetConversion(commissCurrency, TokenCommissionCurrency);
            if (conversion.Error != null)
            {
                return commiss;
            }
            decimal tokenCommissConvRate = conversion.Value;
            decimal tokenCommiss = (1M - tokenCommissDiscount) * commiss * tokenCommissConvRate;

            // UL: ???
            if (Math.Abs(tokenCommiss) > tokenAsset.FreeAmount)
                return commiss;

            commiss = tokenCommiss;
            commissCurrency = TokenCommissionCurrency;

            return commiss;
        }

        private decimal ApplyMinimalCommission(ISymbolInfo symbolInfo, CashAccountCalculator calculator, decimal commiss, string commissCurrency)
        {
            if (symbolInfo.MinCommission <= 0)
            {
                return commiss;
            }

            var convertion = calculator.Market.ConversionMap.GetNegativeAssetConversion(symbolInfo.MinCommissionCurrency, commissCurrency);
            if (convertion.Error != null)
            {
                return commiss;
            }
            decimal minCommissConvRate = convertion.Value;
            decimal minCommiss = -(decimal)symbolInfo.MinCommission * minCommissConvRate;
            if (minCommiss < commiss)
            {
                commiss = minCommiss;
            }

            return commiss;
        }
        #endregion Comission

        #endregion Validate orders

        private string ToMarginAccountInfoString()
        {
            return $"Balance={BalanceRounded} {BalanceCurrency}, Equity={EquityRounded}, Margin={MarginRounded}, MarginLevel={Math.Round(100*MarginLevelRounded, 2)}%";
        }

        #endregion Methods
    }
}
