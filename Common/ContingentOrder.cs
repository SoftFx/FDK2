using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.FDK.Common
{
    public class ContingentOrderTriggerReport
    {
        public string Id { get; set; }
        public long ContingentOrderId { get; set; }
        public DateTime TransactionTime { get; set; }
        public ContingentOrderTriggerType TriggerType { get; set; }
        public TriggerResultState TriggerState { get; set; }
        public DateTime? TriggerTime { get; set; }
        public long? OrderIdTriggeredBy { get; set; }
        public string Symbol { get; set; }
        public OrderType Type { get; set; }
        public OrderSide Side { get; set; }
        public double? Price { get; set; }
        public double? StopPrice { get; set; }
        public double Amount { get; set; }
        public long? RelatedOrderId { get; set; }

        public override string ToString()
        {
            return string.Format
            (
                "Id={0}; ContingentOrderId={1}; TransactionTime={2}; TriggerType={3}; TriggerState={4}; TriggerTime={5}; Symbol={6}; Type={7}; Side={8}; Price={9}; StopPrice={10}; Amount={11}; RelatedOrderId={12}",
                Id,
                ContingentOrderId,
                TransactionTime,
                TriggerType,
                TriggerState,
                TriggerTime,
                Symbol,
                Type,
                Side,
                Price,
                StopPrice,
                Amount,
                RelatedOrderId
            );
        }
    }

    public enum TriggerResultState
    {
        Failed,
        Successful
    }

    public enum ContingentOrderTriggerType
    {
        OnPendingOrderExpired,
        OnPendingOrderPartiallyFilled,
        OnTime
	}
}
