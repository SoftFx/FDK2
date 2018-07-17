namespace DataFeedExamples
{
    using System;
    using System.IO;
    using System.Reflection;
    using TickTrader.FDK.Extended;

    abstract class Example : IDisposable
    {
        #region Construction

        protected Example(string address, string username, string password)
        {
            // Create folders
            EnsureDirectoriesCreated();

            // Create builder
            this.builder = new ConnectionStringBuilder
            {
                Address = address,
                LogDirectory = LogPath,
                Username = username,
                Password = password,
                LogMessages = true,
                OperationTimeout = 30000
            };

            this.Feed = new DataFeed();
#if TODO
            this.Storage = new DataFeedStorage(StoragePath, StorageProvider.Ntfs, this.Feed, true);
#endif
        }

        static void EnsureDirectoriesCreated()
        {
            if (!Directory.Exists(LogPath))
                Directory.CreateDirectory(LogPath);
#if TODO
            if (!Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);
#endif
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

        static string StoragePath
        {
            get
            {
                return Path.Combine(CommonPath, "Storage");
            }
        }

        protected DataFeed Feed { get; private set; }
#if TODO
        protected DataFeedStorage Storage { get; private set; }
#endif

#endregion

#region Control Methods

        public void Run()
        {
            this.Feed.Initialize(this.builder.ToString());
            this.Feed.Logon += this.OnLogon;
            this.Feed.Logout += this.OnLogout;            
            this.Feed.Subscribed += this.OnSubscribed;
            this.Feed.Unsubscribed += this.OnUnsubscribed;
            this.Feed.SessionInfo += this.OnSessionInfo;
            this.Feed.Tick += this.OnTick;            
            this.Feed.Notify += this.OnNotify;

            this.Feed.Start();

            try
            {
                if (! this.Feed.WaitForLogon())
                    throw new TimeoutException("Timeout of logon waiting has been reached");

                this.RunExample();
            }
            finally
            {
                this.Feed.Stop();
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

        protected virtual void OnSubscribed(object sender, SubscribedEventArgs e)
        {
            Console.WriteLine("OnSubscribed(): {0}", e);
        }

        protected virtual void OnUnsubscribed(object sender, UnsubscribedEventArgs e)
        {
            Console.WriteLine("OnUnsubscribed(): {0}", e.Symbol);
        }

        protected virtual void OnSessionInfo(object sender, SessionInfoEventArgs e)
        {
            Console.WriteLine("OnSessionInfo(): {0}", e);
        }

        protected virtual void OnTick(object sender, TickEventArgs e)
        {
            Console.WriteLine("OnTick(): {0}", e);
        }

        protected virtual void OnNotify(object sender, NotificationEventArgs e)
        {
            Console.WriteLine("OnNotify(): {0}", e);
        }

#endregion

#region Abstract Methods

        protected abstract void RunExample();

#endregion

#region IDisposable Interface

        public void Dispose()
        {
            if (this.Feed != null)
                this.Feed.Dispose();
#if TODO
            if (this.Storage != null)
                this.Storage.Dispose();
#endif
        }

#endregion

#region Members

        ConnectionStringBuilder builder;

#endregion
    }
}
