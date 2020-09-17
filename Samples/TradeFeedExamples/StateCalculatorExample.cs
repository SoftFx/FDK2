namespace TradeFeedExamples
{
    using System;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Calculator;

    class StateCalculatorExample : Example
    {
        public StateCalculatorExample(string address, string username, string password)
            : base(address, username, password)
        {
            stateCalculator = new StateCalculator(this.Trade, this.Feed);
        }

        protected override void RunExample()
        {
            try
            {
                stateCalculator.StateInfoChanged += OnStateInfoChanged;
                stateCalculator.CalculatorException += OnCalculatorException;

                // DataFeed subscribes to quotes updates with depth = 1 for all symbols

                /*SymbolInfo[] symbolInfos = this.Feed.Cache.Symbols;
                int count = symbolInfos.Length;
                string[] symbols = new string[count];

                for (var index = 0; index < count; ++index)
                    symbols[index] = symbolInfos[index].Name;

                this.Feed.Server.SubscribeToQuotes(symbols, 1);*/

                Console.WriteLine("Press any key to stop");
                Console.ReadKey();                    

                //this.Feed.Server.UnsubscribeQuotes(symbols);
            }
            finally
            {
                stateCalculator.CalculatorException -= OnCalculatorException;
                stateCalculator.StateInfoChanged -= OnStateInfoChanged;
                stateCalculator.Dispose();
            }
        }

        void OnStateInfoChanged(object sender, StateInfoEventArgs e)
        {
            Console.WriteLine
            (
                "Generation = {0}; Balance = {1}; Equity = {2}; Margin = {3}; Free Margin = {4}; Margin Level = {5}%; Trades = {6}",
                e.Information.Generation,
                e.Information.Balance,
                e.Information.Equity,
                e.Information.Margin,
                e.Information.FreeMargin,
                e.Information.MarginLevel * 100,
                e.Information.TradeRecords.Length
            );
        }

        void OnCalculatorException(object sender, ExceptionEventArgs e)
        {
            Console.WriteLine("Calculation error : {0}", e.Exception.Message);
        }

        StateCalculator stateCalculator;
    }
}
