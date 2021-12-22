﻿namespace Swan.Platform
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Provides utility methods to retrieve information about the current application.
    /// </summary>
    public static class SwanRuntime
    {
        private static readonly Lazy<Assembly> EntryAssemblyLazy = new(() => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());

        private static readonly Lazy<string> CompanyNameLazy = new(() =>
        {
            var attribute =
                EntryAssembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
            return attribute?.Company ?? string.Empty;
        });

        private static readonly Lazy<string> ProductNameLazy = new(() =>
        {
            var attribute =
                EntryAssembly.GetCustomAttribute(typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
            return attribute?.Product ?? string.Empty;
        });

        private static readonly Lazy<string> ProductTrademarkLazy = new(() =>
        {
            var attribute =
                EntryAssembly.GetCustomAttribute(typeof(AssemblyTrademarkAttribute)) as AssemblyTrademarkAttribute;
            return attribute?.Trademark ?? string.Empty;
        });

        private static readonly string ApplicationMutexName = "Global\\{{" + EntryAssembly.FullName + "}}";

        private static readonly Lazy<Encoding> CurrentAnsiEncodingLazy = new(Encoding.GetEncoding(default(int)));

        private static readonly Lazy<Encoding> Windows1252EncodingLazy = new(
            Encoding.GetEncodings().FirstOrDefault(c => c.CodePage == 1252)?.GetEncoding() ?? CurrentAnsiEncodingLazy.Value);

        private static readonly object SyncLock = new();

        #region Properties

        /// <summary>
        /// Gets the current ANSI encoding
        /// </summary>
        public static Encoding CurrentAnsiEncoding => CurrentAnsiEncodingLazy.Value;

        /// <summary>
        /// Gets the Windows 1253 Encoding (if available). Otherwise returns the default ANSI encoding.
        /// </summary>
        public static Encoding Windows1252Encoding => Windows1252EncodingLazy.Value;

        /// <summary>
        /// Checks if this application (including version number) is the only instance currently running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is the only instance; otherwise, <c>false</c>.
        /// </value>
        public static bool IsTheOnlyInstance
        {
            get
            {
                lock (SyncLock)
                {
                    try
                    {
                        // Try to open existing mutex.
                        using var _ = Mutex.OpenExisting(ApplicationMutexName);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        try
                        {
                            // If exception occurred, there is no such mutex.
                            using var appMutex = new Mutex(true, ApplicationMutexName);

                            // Only one instance.
                            return true;
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch
#pragma warning restore CA1031 // Do not catch general exception types
                        {
                            // Sometimes the user can't create the Global Mutex
                        }
                    }

                    // More than one instance.
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this application instance is using the MONO runtime.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is using MONO runtime; otherwise, <c>false</c>.
        /// </value>
        public static bool IsUsingMonoRuntime => Type.GetType("Mono.Runtime") is not null;

        /// <summary>
        /// Gets the assembly that started the application.
        /// </summary>
        /// <value>
        /// The entry assembly.
        /// </value>
        public static Assembly EntryAssembly => EntryAssemblyLazy.Value;

        /// <summary>
        /// Gets the name of the entry assembly.
        /// </summary>
        /// <value>
        /// The name of the entry assembly.
        /// </value>
        public static AssemblyName EntryAssemblyName => EntryAssemblyLazy.Value.GetName();

        /// <summary>
        /// Gets the entry assembly version.
        /// </summary>
        public static Version EntryAssemblyVersion => EntryAssemblyName.Version ?? new();

        /// <summary>
        /// Gets the full path to the folder containing the assembly that started the application.
        /// </summary>
        /// <value>
        /// The entry assembly directory.
        /// </value>
        public static string EntryAssemblyDirectory
        {
            get
            {
                var uri = new UriBuilder(EntryAssembly.Location);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path) ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the name of the company.
        /// </summary>
        /// <value>
        /// The name of the company.
        /// </value>
        public static string CompanyName => CompanyNameLazy.Value;

        /// <summary>
        /// Gets the name of the product.
        /// </summary>
        /// <value>
        /// The name of the product.
        /// </value>
        public static string ProductName => ProductNameLazy.Value;

        /// <summary>
        /// Gets the trademark.
        /// </summary>
        /// <value>
        /// The product trademark.
        /// </value>
        public static string ProductTrademark => ProductTrademarkLazy.Value;

        /// <summary>
        /// Gets a local storage path with a version.
        /// </summary>
        /// <value>
        /// The local storage path.
        /// </value>
        public static string LocalStoragePath
        {
            get
            {
                var localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        EntryAssemblyName.Name);

                var returnPath = Path.Combine(localAppDataPath, EntryAssemblyVersion.ToString());

                if (!Directory.Exists(returnPath))
                {
                    Directory.CreateDirectory(returnPath);
                }

                return returnPath;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Build a full path pointing to the current user's desktop with the given filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>
        /// The fully qualified location of path, such as "C:\MyFile.txt".
        /// </returns>
        /// <exception cref="ArgumentNullException">filename.</exception>
        public static string GetDesktopFilePath(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException(nameof(filename));

            var pathWithFilename = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                filename);

            return Path.GetFullPath(pathWithFilename);
        }

        #endregion
    }
}