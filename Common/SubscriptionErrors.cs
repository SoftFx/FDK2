using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.FDK.Common
{
    public class SubscriptionErrors
    {
        private readonly Dictionary<SubscriptionErrorCodes, List<string>> _innerCollection;

        public SubscriptionErrors(IEnumerable<KeyValuePair<SubscriptionErrorCodes, List<string>>> errors)
        {
            _innerCollection = errors.ToDictionary(it => it.Key, it => it.Value);
        }

        public IReadOnlyDictionary<SubscriptionErrorCodes, List<string>> Errors => _innerCollection;

        public bool IsEmpty => !_innerCollection.Any();
    }

    public enum SubscriptionErrorCodes
    {
        SymbolNotFound,
        Other
    }
}
