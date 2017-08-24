namespace TickTrader.FDK.Extended
{
    using System;
    using Common;

    /// <summary>
    /// The class contains common part of all SoftFX event arguments.
    /// </summary>
    public abstract class DataEventArgs : EventArgs
    {
        /// <summary>
        /// Gets UTC server date and time, when the event has been sent by server (if available).
        /// </summary>
        public DateTime? SendingTime { get; set; }

        /// <summary>
        /// Gets UTC client date and time, when the event has been received by server.
        /// </summary>
        public DateTime ReceivingTime { get; set; }

        /// <summary>
        /// Returns formated string for the instance.
        /// </summary>								
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var stSendingTime = string.Empty;
            if (this.SendingTime != null)
            {
                var sendingTime = (DateTime)this.SendingTime;
                stSendingTime = string.Format("SendingTime = {0};", sendingTime);
            }
            var result = string.Format("{0}ReceivingTime = {1}", stSendingTime, this.ReceivingTime);
            return result;
        }
    }

    /// <summary>
    /// Contains data for the logon event.
    /// </summary>
    public class LogonEventArgs : DataEventArgs
    {
        /// <summary>
        /// Get protocol version of logon event.
        /// </summary>
        public string ProtocolVersion { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = this.ProtocolVersion;
            if (string.IsNullOrEmpty(result))
            {
                result = base.ToString();
            }
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle logon event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Logon information.</param>
    public delegate void LogonHandler(object sender, LogonEventArgs e);

    /// <summary>
    /// Contains data for the logout event.
    /// </summary>
    public class LogoutEventArgs : DataEventArgs
    {
        #region Properties

        /// <summary>
        /// Get text description of logout event.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets logout reason; supported for version >= ext.1.0.
        /// </summary>
        public LogoutReason Reason { get; set; }

        /// <summary>
        /// Gets GetLastError() code, if logout reason is connection problem, otherwise 0.
        /// </summary>
        public int Code { get; set; }

        #endregion

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = this.Text;
            if (string.IsNullOrEmpty(result))
            {
                result = base.ToString();
            }
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle logout event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Logout information.</param>
    public delegate void LogoutHandler(object sender, LogoutEventArgs e);

    /// <summary>
    /// Contains data for the two factor auth event.
    /// </summary>
    public class TwoFactorAuthEventArgs : DataEventArgs
    {
        /// <summary>
        /// Contains information about feed/trade two factor auth info.
        /// </summary>
        public TwoFactorAuth TwoFactorAuth { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = TwoFactorAuth.ToString();
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle two factor auth event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Two factor auth information.</param>
    public delegate void TwoFactorAuthHandler(object sender, TwoFactorAuthEventArgs e);

    /// <summary>
    /// Contains data for the subscribed event.
    /// </summary>
    public class SubscribedEventArgs : DataEventArgs
    {
        /// <summary>
        /// Gets snapshot tick.
        /// </summary>
        public Quote Tick { get; set; }

        /// <summary>
        /// Returns formatted string for class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = this.Tick.ToString();
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle symbol subscribed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Subscribed nformation.</param>
    public delegate void SubscribedHandler(object sender, SubscribedEventArgs e);

    /// <summary>
    /// Contains data for the unsubscribed event.
    /// </summary>
    public class UnsubscribedEventArgs : DataEventArgs
    {
        /// <summary>
        /// Gets symbol name.
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Returns formatted string for class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = this.Symbol;
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle symbol unsubscribed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Unsubscribed nformation.</param>
    public delegate void UnsubscribedHandler(object sender, UnsubscribedEventArgs e);
    
    /// <summary>
    /// Contains data for the tick event.
    /// </summary>
    public class TickEventArgs : DataEventArgs
    {
        /// <summary>
        /// Gets update tick.
        /// </summary>
        public Quote Tick { get; set; }

        /// <summary>
        /// Returns formatted string for class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = this.Tick.ToString();
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle new tick event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Tick information.</param>
    public delegate void TickHandler(object sender, TickEventArgs e);

    /// <summary>
    /// This message contains current feed/trade session information. It received by the client in following circumstances:
    /// 1. After successful login;
    /// 2. After trading session status is changed on server (opened to closed, closed to opened);
    /// </summary>
    public class SessionInfoEventArgs : DataEventArgs
    {
        /// <summary>
        /// Contains information about feed/trade session info.
        /// </summary>
        public SessionInfo Information { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = Information.ToString();
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle: successful login; session status is changed on server (opened to closed, closed to opened).
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Contains session information.</param>
    public delegate void SessionInfoHandler(object sender, SessionInfoEventArgs e);

    /// <summary>
    /// Contains data for the cache event.
    /// </summary>
    public class CacheEventArgs : EventArgs
    {
    }

    /// <summary>
    /// Represents the method that will handle: cache modification.
    /// </summary>
    /// <param name="sender">The source of the event; can be data feed or data trade instance.</param>
    /// <param name="e">Contains cache modification information.</param>
    public delegate void CacheHandler(object sender, CacheEventArgs e);

    /// <summary>
    /// Contains account information.
    /// </summary>
    public class AccountInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Gets account information.
        /// </summary>
        public AccountInfo Information { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = Information.ToString();
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle: account settings (Leverage, Currency, AccountingType) are changed.
    /// </summary>
    /// <param name="sender">The source of the event; can be data trade instance.</param>
    /// <param name="e">Contains account information.</param>
    public delegate void AccountInfoHandler(object sender, AccountInfoEventArgs e);

    /// <summary>
    /// Contains data for execution report event.
    /// </summary>
    public class ExecutionReportEventArgs : EventArgs
    {
        /// <summary>
        /// Get corresponded execution report; can not be null.
        /// </summary>
        public ExecutionReport Report { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = Report.ToString();
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle: any execution report.
    /// </summary>
    /// <param name="sender">The source of the event; can be data trade instance.</param>
    /// <param name="e">Contains trade report information.</param>
    public delegate void ExecutionReportHandler(object sender, ExecutionReportEventArgs e);

    /// <summary>
    /// Contains data for currency info event.
    /// </summary>
    public class CurrencyInfoEventArgs : EventArgs
    {        
        /// <summary>
        /// Gets currencies information; can not be null.
        /// </summary>
        public CurrencyInfo[] Information { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = Information.ToString();
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle: initialization of currency information.
    /// </summary>
    /// <param name="sender">The source of the event; can be data trade instance.</param>
    /// <param name="e">Contains symbols information.</param>
    public delegate void CurrencyInfoHandler(object sender, CurrencyInfoEventArgs e);

    /// <summary>
    /// Contains data for symbol info event.
    /// </summary>
    public class SymbolInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Gets symbols information; can not be null.
        /// </summary>
        public SymbolInfo[] Information { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = Information.ToString();
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle: initialization of symbols information.
    /// </summary>
    /// <param name="sender">The source of the event; can be data trade instance.</param>
    /// <param name="e">Contains symbols information.</param>
    public delegate void SymbolInfoHandler(object sender, SymbolInfoEventArgs e);

    /// <summary>
    /// Data for TradeTransactionReport event.
    /// </summary>
    public class TradeTransactionReportEventArgs : EventArgs
    {
        /// <summary>
        /// Trade transaction report
        /// </summary>
        public TradeTransactionReport Report { get; set; }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void TradeTransactionReportHandler(object sender, TradeTransactionReportEventArgs e);

    /// <summary>
    /// Contains data for position report event.
    /// </summary>
    public class PositionReportEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a position report; can not be null.
        /// </summary>
        public Position Report { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public override string ToString()
        {
            return this.Report.ToString();
        }
    }

    /// <summary>
    /// Represents the method that will handle: any position report update.
    /// </summary>
    /// <param name="sender">The source of the event; can be data trade instance.</param>
    /// <param name="e">Contains trade position report information.</param>
    public delegate void PositionReportHandler(object sender, PositionReportEventArgs e);

    /// <summary>
    /// Notification message.
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the notification severity.
        /// </summary>
        public NotificationSeverity Severity { get; set; }

        /// <summary>
        /// Gets the notification type.
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// Gets the notification text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = string.Format("Severity = {0}; Type = {1}; Text = {2}", this.Severity, this.Type, this.Text);
            return result;
        }
    }

    /// <summary>
    /// Represents the method that will handle: margin call, margin call revocation, stop out.
    /// </summary>
    /// <param name="sender">The source of event; can be DataTrade instance.</param>
    /// <param name="e">Contains notification information.</param>
    public delegate void NotifyHandler(object sender, NotificationEventArgs e);

    /// <summary>
    /// Notification message with argument.
    /// </summary>
    /// <typeparam name="T">any type.</typeparam>
    public class NotificationEventArgs<T> : NotificationEventArgs
    {
        /// <summary>
        /// Gets notification argument.
        /// </summary>
        public T Data { get; set; }
    }

    /// <summary>
    /// Represents the method that will handle: margin call, margin call revocation, stop out.
    /// </summary>
    /// <param name="sender">The source of event; can be DataTrade instance.</param>
    /// <param name="e">Contains notification information.</param>
    public delegate void NotifyHandler<T>(object sender, NotificationEventArgs<T> e);
}