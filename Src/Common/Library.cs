namespace TickTrader.FDK.Common
{
    using System;
    using System.Reflection;

    /// <summary>
    /// This class provides common setting of FDK.
    /// </summary>
    public static class Library
    {
        /// <summary>
        /// Gets or sets absolute or relative path to directory, which contains native FDK libraries.
        /// For example: Libary.Path = @"C:\libs\";
        /// You can also use environment variable, for example, Library.Path = "&lt;FRE&gt;"
        /// If specified folder does not contain some dlls, then they will be extracted in runtime.
        /// </summary>
        public static string Path
        {
            get
            {
                return LibPath;
            }

            set
            {
            }
        }

        /// <summary>
        /// Gets FDK version.
        /// </summary>
        public static string Version { get; private set; }

        /// <summary>
        /// Gets unique identifier of the FDK.
        /// </summary>
        public static string Id { get; private set; }

        /// <summary>
        /// Gets current platroform; can be x86 or x64.
        /// </summary>
        public static string Platform { get; private set; }

        /// <summary>
        /// For internal usage: enables or disables resolving of .Net assemblies from resources.
        /// </summary>
        public static bool ResolveDotNetAssemblies { get; set; }

        static Library()
        {
            Version = TickTrader.FDK.Common.Version.Major + "." + TickTrader.FDK.Common.Version.Minor;
            if (TickTrader.FDK.Common.Version.Stage != "")
                Version = Version + " " + TickTrader.FDK.Common.Version.Stage;
            Id = string.Empty;
            ResolveDotNetAssemblies = true;
            Platform = "MSIL";
            LibPath = Assembly.GetCallingAssembly().Location;
        }

        /// <summary>
        /// The method forces FDK initialization. Try to use it, if you have problems.
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Check for VS redistributable packages
        /// </summary>
        public static void CheckRedistPackages()
        {
        }

        /// <summary>
        /// Extract all underlying libraries to a specified directory.
        /// </summary>
        /// <param name="location">A relative or absolute path to directory where libraries and tools should be extracted</param>
        public static void ExtractUnderlyingFiles(string location)
        {
        }

        /// <summary>
        /// The method delete all extracted dll/exe files from cache. The files cache location is specified by Library.Path.
        /// </summary>
        public static void DeleteFilesCache()
        {
        }

        /// <summary>
        /// The method specifies a path, which should be used for normal dump writing on exception/fatal error.
        /// </summary>
        /// <param name="path">a path to normal dump file</param>
        public static void WriteNormalDumpOnError(string path)
        {
        }

        /// <summary>
        /// The method specifies a path, which should be used for full dump writing on exception/fatal error.
        /// </summary>
        /// <param name="path">a path to full dump file</param>
        public static void WriteFullDumpOnError(string path)
        {
        }

        /// <summary>
        /// The method write a normal dump by specified location.
        /// </summary>
        /// <param name="path">a path to normal dump file</param>
        public static void WriteNormalDump(string path)
        {
        }

        /// <summary>
        /// The method write a full dump by specified location.
        /// </summary>
        /// <param name="path">a path to full dump file</param>
        public static void WriteFullDump(string path)
        {
        }

        #region Members

        static string LibPath = string.Empty;

        #endregion
    }
}
