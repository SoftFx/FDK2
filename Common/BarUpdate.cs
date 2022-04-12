using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.FDK.Common
{
    public class AggregatedBarUpdate
    {
        public string Symbol { get; set; }
        public double? AskClose { get; set; }
        public double? BidClose { get; set; }
        public Dictionary<BarParameters, BarUpdate> Updates { get; set; } = new Dictionary<BarParameters, BarUpdate>();

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append($"Symbol: {Symbol}; ");
            if (AskClose.HasValue)
            {
                builder.Append($"AskClosePrice: {AskClose.Value.ToString("G29")}; ");
            }
            if (BidClose.HasValue)
            {
                builder.Append($"BidClosePrice: {BidClose.Value.ToString("G29")}; ");
            }
            if (Updates.Any())
            {
                builder.AppendLine();
                foreach (var update in Updates)
                {
                    builder.AppendLine($"{update.Key}; {update.Value}");
                }
            }
            return builder.ToString();
        }
    }

    public struct BarUpdate
    {
        public double? Open { get; set; }
        public double? High { get; set; }
        public double? Low { get; set; }
        public DateTime? From { get; set; }

        public override string ToString()
        {
            var result = new StringBuilder();
            if (From.HasValue)
            {
                result.Append($"From: {From}; ");
            }
            if (Open.HasValue)
            {
                result.Append($"Open: {Open.Value.ToString("G29")}; ");
            }
            if (High.HasValue)
            {
                result.Append($"High: {High.Value.ToString("G29")}; ");
            }
            if (Low.HasValue)
            {
                result.Append($"Low: {Low.Value.ToString("G29")}; ");
            }
            return result.ToString();
        }
    }

    public struct BarParameters
    {
        public Periodicity Periodicity { get; }
        public PriceType PriceType { get; }

        public BarParameters(Periodicity periodicity, PriceType priceType)
        {
            Periodicity = periodicity;
            PriceType = priceType;
        }

        public override string ToString()
        {
            return $"Periodicity: {Periodicity}; PriceType: {PriceType}";
        }
    }

    public struct BarSubscriptionSymbolEntry
    { 
        public string Symbol { get; set; }
        public BarParameters[] Params { get; set; }
    }
}
