﻿namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class ModifyTradeRecordExample : Example
    {
        public ModifyTradeRecordExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var record1 = this.Trade.Server.SendOrder("EURUSD", OrderType.Limit, OrderSide.Buy, 10000, null, 1.0, null, null, null, OrderTimeInForce.GoodTillCancel, null, null, null, null, false, null, false, false, null);
            Console.WriteLine(record1);
            var record2 = record1.Modify(1.1, null, null, null, OrderTimeInForce.GoodTillCancel, null, null, null, null, false, null);
            Console.WriteLine(record2);
        }
    }
}
