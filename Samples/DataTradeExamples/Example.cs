namespace DataTradeExamples
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    abstract class Example : IDisposable
    {
        #region Construction

        protected Example(string address, string username, string password)
        {
            // Create folders
            EnsureDirectoriesCreated();

            this.builder = new ConnectionStringBuilder
            {
                Address = address,
                LogDirectory = LogPath,
                Username = username,
                Password = password,
                LogMessages = true,
                OperationTimeout = 30000
            };

            this.Trade = new DataTrade();
        }

        static void EnsureDirectoriesCreated()
        {
            if (!Directory.Exists(LogPath))
                Directory.CreateDirectory(LogPath);
        }

        #endregion

        #region Properties

        static string CommonPath
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                return assembly != null ? Path.GetDirectoryName(assembly.Location) : string.Empty;
            }
        }

        static string LogPath
        {
            get
            {
                return Path.Combine(CommonPath, "Logs");
            }
        }

        protected DataTrade Trade { get; private set; }

        #endregion

        #region Control Methods

        public void Run()
        {
            this.Trade.Initialize(this.builder.ToString());
            this.Trade.Logon += this.OnLogon;
            this.Trade.Logout += this.OnLogout;
            this.Trade.TwoFactorAuth += this.OnTwoFactorAuth;
            this.Trade.SessionInfo += this.OnSessionInfo;
            this.Trade.AccountInfo += this.OnAccountInfo;
            this.Trade.ExecutionReport += this.OnExecutionReport;
            this.Trade.PositionReport += this.OnPositionReport;            
            this.Trade.BalanceOperation += this.OnBalanceOperaiton;
            this.Trade.TradeTransactionReport += this.OnTradeTransactionReport;
            this.Trade.Notify += this.OnNofity;
            
            this.Trade.Start();

            try
            {
                if (! this.Trade.WaitForLogon())
                    throw new TickTrader.FDK.Common.TimeoutException("Timeout of logon waiting has been reached");

                this.RunExample();
            }
            finally
            {
                this.Trade.Stop();
            }
        }

        #endregion

        #region Event Handlers

        protected virtual void OnLogon(object sender, LogonEventArgs e)
        {
            Console.WriteLine("OnLogon(): {0}", e);
        }

        protected virtual void OnLogout(object sender, LogoutEventArgs e)
        {
            Console.WriteLine("OnLogout(): {0}", e);
        }

        protected virtual void OnTwoFactorAuth(object sender, TwoFactorAuthEventArgs e)
        {
            if (e.TwoFactorAuth.Reason == TwoFactorReason.ServerRequest)
            {
                Console.WriteLine("Please enter one time password: ");
                string otp = Console.ReadLine();
                Trade.Server.SendTwoFactorLoginResponse(otp);
            }
            else if (e.TwoFactorAuth.Reason == TwoFactorReason.ServerSuccess)
            {
                Console.WriteLine("Two factor success: {0}", e.TwoFactorAuth.Expire);
            }
            else if (e.TwoFactorAuth.Reason == TwoFactorReason.ServerError)
            {
                Console.WriteLine("Two factor failed: {0}", e.TwoFactorAuth.Text);
            }
            else if (e.TwoFactorAuth.Reason == TwoFactorReason.ServerResume)
            {
                Console.WriteLine("Two factor resume: {0}", e.TwoFactorAuth.Expire);
            }
            else
                Console.WriteLine("Invalid two factor server response: {0} - {1}", e.TwoFactorAuth.Reason, e.TwoFactorAuth.Text);
        }

        protected virtual void OnAccountInfo(object sender, AccountInfoEventArgs e)
        {
            Console.WriteLine("OnAccountInfo(): {0}", e);
        }

        protected virtual void OnSessionInfo(object sender, SessionInfoEventArgs e)
        {
            Console.WriteLine("OnSessionInfo(): {0}", e);
        }

        protected virtual void OnExecutionReport(object sender, ExecutionReportEventArgs e)
        {
            Console.WriteLine("OnExecutionReport(): {0}", e);
        }

        protected virtual void OnPositionReport(object sender, PositionReportEventArgs e)
        {
            Console.WriteLine("OnPositionReport(): {0}", e);
        }

        protected virtual void OnBalanceOperaiton(object sender, NotificationEventArgs<BalanceOperation> e)
        {
            Console.WriteLine("OnBalanceOperaiton(): {0}", e);
        }

        protected virtual void OnTradeTransactionReport(object sender, TradeTransactionReportEventArgs e)
        {
            Console.WriteLine("OnTradeTransactionReport(): {0}", e);
        }

        protected virtual void OnNofity(object sender, NotificationEventArgs e)
        {
            Console.WriteLine("OnNotify(): {0}", e);
        }

        #endregion

        #region Abstract Methods

        protected abstract void RunExample();

        #endregion

        #region IDisposable interface

        public void Dispose()
        {
            if (this.Trade != null)
                this.Trade.Dispose();
        }

        #endregion

        #region Members

        ConnectionStringBuilder builder;

        #endregion
    }
}
