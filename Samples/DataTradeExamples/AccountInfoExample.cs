namespace DataTradeExamples
{
    using System;

    class AccountInfoExample : Example
    {
        public AccountInfoExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var info = Trade.Cache.AccountInfo;
            Console.WriteLine(info);
        }
    }
}
