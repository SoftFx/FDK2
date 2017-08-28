namespace StandardExamples
{
    using System;
    using System.IO;
    using System.Reflection;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Standard;
    using TickTrader.FDK.Extended;

    abstract class Example : IDisposable
    {
        #region Construction

        protected Example(string address, string username, string password)
        {
            // Create folders
            EnsureDirectoriesCreated();

            ConnectionStringBuilder dataTradeBuilder = new ConnectionStringBuilder
            {
                Port = 5040,
                Address = address,
                LogDirectory = LogPath,
                Username = username,
                Password = password,
                DecodeLogMessages = true,
                OperationTimeout = 30000
            };

            ConnectionStringBuilder dataFeedBuilder = new ConnectionStringBuilder
            {
                Port = 5030,
                Address = address,
                LogDirectory = LogPath,
                Username = username,
                Password = password,
                DecodeLogMessages = true,
                OperationTimeout = 30000
            };

            this.Manager = new Manager(dataTradeBuilder.ToString(), dataFeedBuilder.ToString(), "Quotes");
            this.Manager.Updated += this.OnUpdated;
            this.Manager.Error += this.OnError;
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

        protected Manager Manager { get; private set; }

        #endregion

        #region Control Methods

        public void Run()
        {
            this.Manager.Start();

            try
            {
                this.RunExample();
            }
            finally
            {
                this.Manager.Stop();
            }
        }


        protected virtual void OnUpdated(object sender, EventArgs args)
        {
        }

        protected virtual void OnError(object sender, TickTrader.FDK.Standard.ErrorEventArgs args)
        {
            try
            {
                Console.WriteLine("Error : " + args.Exception.Message);
            }
            catch
            {
            }
        }

        #endregion

        #region Abstract Methods

        protected abstract void RunExample();

        #endregion

        #region IDisposable Interface

        public void Dispose()
        {
            if (this.Manager != null)
                this.Manager.Dispose();
        }

        #endregion
    }
}
