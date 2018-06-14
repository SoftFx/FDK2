namespace TickTrader.FDK.Common
{
    public class Notification
    {
        public Notification()
        {
        }

        public string Id { get; set; }

        public NotificationType Type { get; set; }

        public NotificationSeverity Severity { get; set; }

        public string Message { get; set; }

        public override string ToString()
        {
            return string.Format("Id = {0}; Type = {1}; Severity = {2}; Messafe = {3}", this.Id, this.Type, this.Severity, this.Message);
        }
    }
}
