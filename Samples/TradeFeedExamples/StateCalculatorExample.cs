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
            stateCalculator.StateInfoChanged += OnStateInfoChanged;
            stateCalculator.CalculatorException += OnCalculatorException;

            try
            {
                SymbolInfo[] symbolInfos = this.Feed.Cache.Symbols;
                int count = symbolInfos.Length;
                string[] symbols = new string[count];

                for (var index = 0; index < count; ++index)
                    symbols[index] = symbolInfos[index].Name;

                this.Feed.Server.SubscribeToQuotes(symbols, 1);

                Console.WriteLine("Press any key to stop");
                Console.ReadKey();                    

                this.Feed.Server.UnsubscribeQuotes(symbols);
            }
            finally
            {
                stateCalculator.CalculatorException -= OnCalculatorException;
                stateCalculator.StateInfoChanged -= OnStateInfoChanged;
            }
        }

        void OnStateInfoChanged(object sender, StateInfoEventArgs e)
        {
            Console.WriteLine("Generation = {0}; Margin = {1}; Trades = {2}", e.Information.Generation, e.Information.Margin, e.Information.TradeRecords.Length);
        }

        void OnCalculatorException(object sender, ExceptionEventArgs e)
        {
            Console.WriteLine("Calculation error : {0}", e.Exception.Message);
        }

        StateCalculator stateCalculator;
    }
}
