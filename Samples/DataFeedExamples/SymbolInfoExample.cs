namespace DataFeedExamples
{
    using System;

    class SymbolInfoExample : Example
    {
        public SymbolInfoExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var symbols = this.Feed.Cache.Symbols;

            Console.WriteLine("Server supports the following symbols ({0})", symbols.Length);
            foreach (var element in symbols)
                Console.WriteLine(element);
        }
    }
}
