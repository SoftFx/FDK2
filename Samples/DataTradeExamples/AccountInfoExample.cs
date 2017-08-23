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
            var info = Trade.Server.GetAccountInfo();
            Console.WriteLine(info);
        }
    }
}
