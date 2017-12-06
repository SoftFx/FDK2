using System;
using System.Collections.Generic;
using System.Linq;
using TickTrader.FDK.Common;
using TickTrader.FDK.QuoteFeed;


namespace FDK2toR
{
    class QuoteFeed
    {

        private const int Timeout = 30000;
        private static Client _client;
        private static List<SymbolInfo> _symbols;
        private static List<CurrencyInfo> _currencies; 

        #region Connection
        public static int Connect(string address, string login, string password)
        {
            try
            {
                _client = new Client("name", 5030, false, "Logs", true);
                _client.Connect(address, Timeout);
                _client.Login(login, password, "", "", "", Timeout);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        public static int Disconnect()
        {
            try
            {
                _client.Logout("Client logout", Timeout);
                _client.Disconnect("Clent disconnect");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        #endregion

        #region Get symbol list

        public static void GetSymbolList()
        {
            try
            {
                _symbols?.Clear();
                _symbols = _client.GetSymbolList(Timeout).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static string[] GetSymbolName()
        {
            return _symbols.Select(it => it.Name).ToArray();
        }
        public static string[] GetSymbolCurrency()
        {
            return _symbols.Select(it => it.Currency).ToArray();
        }
        public static string[] GetSymbolSettlementCurrency()
        {
            return _symbols.Select(it => it.SettlementCurrency).ToArray();
        }
        public static string[] GetSymbolDescription()
        {
            return _symbols.Select(it => it.Description).ToArray();
        }
        public static double[] GetSymbolPrecision()
        {
            return _symbols.Select(it => (double)it.Precision).ToArray();
        }
        public static double[] GetSymbolRoundLot()
        {
            return _symbols.Select(it => it.RoundLot).ToArray();
        }
        public static double[] GetSymbolMinTradeVolume()
        {
            return _symbols.Select(it => it.MinTradeVolume).ToArray();
        }
        public static double[] GetSymbolMaxTradeVolume()
        {
            return _symbols.Select(it => it.MaxTradeVolume).ToArray();
        }
        public static double[] GetSymbolTradeVolumeStep()
        {
            return _symbols.Select(it => it.TradeVolumeStep).ToArray();
        }
        public static string[] GetSymbolProfitCalcMode()
        {
            return _symbols.Select(it => it.ProfitCalcMode.ToString()).ToArray();
        }
        public static string[] GetSymbolMarginCalcMode()
        {
            return _symbols.Select(it => it.MarginCalcMode.ToString()).ToArray();
        }
        public static double[] GetSymbolMarginHedge()
        {
            return _symbols.Select(it => it.MarginHedge).ToArray();
        }
        public static double[] GetSymbolMarginFactor()
        {
            return _symbols.Select(it => (double)it.MarginFactor).ToArray();
        }
        public static double[] GetSymbolMarginFactorFractional()
        {
            return _symbols.Select(it => it.MarginFactorFractional ?? Double.NaN).ToArray();
        }
        public static double[] GetSymbolContractMultiplier()
        {
            return _symbols.Select(it => it.ContractMultiplier).ToArray();
        }
        public static double[] GetSymbolColor()
        {
            return _symbols.Select(it => (double)it.Color).ToArray();
        }
        public static string[] GetSymbolCommissionType()
        {
            return _symbols.Select(it => it.CommissionType.ToString()).ToArray();
        }
        public static string[] GetSymbolCommissionChargeType()
        {
            return _symbols.Select(it => it.CommissionChargeType.ToString()).ToArray();
        }
        public static string[] GetSymbolCommissionChargeMethod()
        {
            return _symbols.Select(it => it.CommissionChargeMethod.ToString()).ToArray();
        }
        public static double[] GetSymbolLimitsCommission()
        {
            return _symbols.Select(it => it.LimitsCommission).ToArray();
        }
        public static double[] GetSymbolCommission()
        {
            return _symbols.Select(it => it.Commission).ToArray();
        }
        public static double[] GetSymbolMinCommission()
        {
            return _symbols.Select(it => it.MinCommission).ToArray();
        }
        public static string[] GetSymbolMinCommissionCurrency()
        {
            return _symbols.Select(it => it.MinCommissionCurrency).ToArray();
        }
        public static string[] GetSymbolSwapType()
        {
            return _symbols.Select(it => it.SwapType.ToString()).ToArray();
        }
        public static double[] GetSymbolTripleSwapDay()
        {
            return _symbols.Select(it => (double)it.TripleSwapDay).ToArray();
        }
        public static double[] GetSymbolSwapSizeShort()
        {
            return _symbols.Select(it => it.SwapSizeShort ?? double.NaN).ToArray();
        }
        public static double[] GetSymbolSwapSizeLong()
        {
            return _symbols.Select(it => it.SwapSizeLong ?? double.NaN).ToArray();
        }
        public static double[] GetSymbolDefaultSlippage()
        {
            return _symbols.Select(it => it.DefaultSlippage ?? double.NaN).ToArray();
        }
        public static bool[] GetSymbolIsTradeEnabled()
        {
            return _symbols.Select(it => it.IsTradeEnabled).ToArray();
        }
        public static double[] GetSymbolGroupSortOrder()
        {
            return _symbols.Select(it => (double)it.GroupSortOrder).ToArray();
        }
        public static double[] GetSymbolSortOrder()
        {
            return _symbols.Select(it => (double)it.SortOrder).ToArray();
        }
        public static double[] GetSymbolCurrencySortOrder()
        {
            return _symbols.Select(it => (double)it.CurrencySortOrder).ToArray();
        }
        public static double[] GetSymbolSettlementCurrencySortOrder()
        {
            return _symbols.Select(it => (double)it.SettlementCurrencySortOrder).ToArray();
        }
        public static double[] GetSymbolCurrencyPrecision()
        {
            return _symbols.Select(it => (double)it.CurrencyPrecision).ToArray();
        }
        public static double[] GetSymbolSettlementCurrencyPrecision()
        {
            return _symbols.Select(it => (double)it.SettlementCurrencyPrecision).ToArray();
        }
        public static string[] GetSymbolStatusGroupId()
        {
            return _symbols.Select(it => it.StatusGroupId).ToArray();
        }
        public static string[] GetSymbolSecurityName()
        {
            return _symbols.Select(it => it.SecurityName).ToArray();
        }
        public static string[] GetSymbolSecurityDescription()
        {
            return _symbols.Select(it => it.SecurityDescription).ToArray();
        }
        public static double[] GetSymbolStopOrderMarginReduction()
        {
            return _symbols.Select(it => it.StopOrderMarginReduction ?? double.NaN).ToArray();
        }
        public static double[] GetSymbolHiddenLimitOrderMarginReduction()
        {
            return _symbols.Select(it => it.HiddenLimitOrderMarginReduction ?? double.NaN).ToArray();
        }


        #endregion

        #region Get currency list

        public static void GetCurrencyList()
        {
            try
            {
                _currencies?.Clear();
                _currencies = _client.GetCurrencyList(Timeout).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public static string[] GetCurrencyName()
        {
            return _currencies.Select(it => it.Name).ToArray();
        }
        public static string[] GetCurrencyDescription()
        {
            return _currencies.Select(it => it.Description).ToArray();
        }
        public static double[] GetCurrencySortOrder()
        {
            return _currencies.Select(it => (double)it.SortOrder).ToArray();
        }
        public static double[] GetCurrencyPrecision()
        {
            return _currencies.Select(it => (double)it.Precision).ToArray();
        }

        #endregion
    }
}
