namespace TickTrader.FDK.Calculator
{
    using System;

    public class AccountCalculatorStats
    {
        public AccountCalculatorStats()
        {
            this.LastUpdated = DateTime.MinValue;
            this.UpdateKind = UpdateKind.Unknown;
        }

        public void Update(UpdateKind updateKind)
        {
            this.LastUpdated = DateTime.Now;
            this.Generation++;
            this.UpdateKind = updateKind;

            this.OnUpdated();
        }

        protected virtual void OnUpdated()
        {
        }

        public DateTime LastUpdated { get; private set; }
        public uint Generation { get; private set; }
        public UpdateKind UpdateKind { get; private set; }
    }
}
