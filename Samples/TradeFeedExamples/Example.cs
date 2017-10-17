namespace TradeFeedExamples
{
    using System;
    using System.IO;
    using System.Reflection;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Calculator;

    abstract class Example : IDisposable
    {
        #region Construction

        protected Example(string address, string username, string password)
        {
            // Create folders
            EnsureDirectoriesCreated();

            ConnectionStringBuilder dataTradeBuilder = new ConnectionStringBuilder
            {
                Address = address,
                LogDirectory = LogPath,
                Username = username,
                Password = password,
                DecodeLogMessages = true,
                OperationTimeout = 30000
            };

            this.Trade = new DataTrade(dataTradeBuilder.ToString());
            this.Trade.Logon += this.OnDataTradeLogon;
            this.Trade.Logout += this.OnDataTradeLogout;

            ConnectionStringBuilder dataFeedBuilder = new ConnectionStringBuilder
            {
                Address = address,
                LogDirectory = LogPath,
                Username = username,
                Password = password,
                DecodeLogMessages = true,
                OperationTimeout = 30000
            };
            
            this.Feed = new DataFeed(dataFeedBuilder.ToString());
            this.Feed.Logon += this.OnDataFeedLogon;
            this.Feed.Logout += this.OnDataFeedLogout;            
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

        protected DataFeed Feed { get; private set; }               

        #endregion

        #region Control Methods

        public void Run()
        {
            this.Trade.Start();

            try
            {
                this.Feed.Start();

                try
                {
                    if (! this.Trade.WaitForLogon())
                        throw new TimeoutException("Timeout of data trade logon waiting has been reached");

                    if (! this.Feed.WaitForLogon())
                        throw new TimeoutException("Timeout of data feed logon waiting has been reached");

                    this.RunExample();
                }
                finally
                {
                    this.Feed.Stop();
                }
            }
            finally
            {
                this.Trade.Stop();
            }
        }

        protected virtual void OnDataTradeLogon(object sender, LogonEventArgs e)
        {
            Console.WriteLine("OnDataTradeLogon(): {0}", e);
        }

        protected virtual void OnDataTradeLogout(object sender, LogoutEventArgs e)
        {
            Console.WriteLine("OnDataTradeLogout(): {0}", e);
        }

        protected virtual void OnDataFeedLogon(object sender, LogonEventArgs e)
        {
            Console.WriteLine("OnDataFeedLogon(): {0}", e);
        }

        protected virtual void OnDataFeedLogout(object sender, LogoutEventArgs e)
        {
            Console.WriteLine("OnDataFeedLogout(): {0}", e);
        }

        #endregion

        #region Abstract Methods

        protected abstract void RunExample();

        #endregion

        #region IDisposable Interface

        public void Dispose()
        {
            if (this.Trade != null)
                this.Trade.Dispose();

            if (this.Feed != null)
                this.Feed.Dispose();
        }

        #endregion
    }
}
