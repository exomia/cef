#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CefSharp;
using CefSharp.Internals;
using CefSharp.OffScreen;

namespace Exomia.CEF
{
    /// <summary>
    ///     A cef wrapper for clean initialization and shutdown.
    /// </summary>
    public class CefWrapper : IDisposable
    {
        /// <summary>
        ///     Initializes static members of the <see cref="CefWrapper"/> class.
        /// </summary>
        static CefWrapper()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        /// <summary>
        ///     Current domain on assembly resolve.
        /// </summary>
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Resolve event information. </param>
        /// <returns>
        ///     An Assembly?
        /// </returns>
        private static Assembly? CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("CefSharp"))
            {
                string archSpecificPath = Path.Combine(
                    AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    Environment.Is64BitProcess ? "x64" : "x86", args.Name.Split(new[] { ',' }, 2)[0] + ".dll");
                return File.Exists(archSpecificPath)
                    ? Assembly.LoadFile(archSpecificPath)
                    : null;
            }

            return null;
        }

        /// <summary>
        ///     Prevents a default instance of the <see cref="CefWrapper" /> class from being created.
        /// </summary>
        /// <param name="commandLineSettings"> Command line settings. </param>
        private CefWrapper(Action<Dictionary<string, string>>? commandLineSettings)
        {
            InitializeCef(commandLineSettings);
        }

        /// <summary>
        ///     Creates a new <see cref="CefWrapper" />.
        /// </summary>
        /// <param name="commandLineSettings"> (Optional) Command line settings. </param>
        /// <returns>
        ///     An <see cref="IDisposable" />.
        /// </returns>
        public static IDisposable Create(Action<Dictionary<string,string>>? commandLineSettings = null)
        {
            return new CefWrapper(commandLineSettings);
        }

        /// <summary>
        ///     Initializes the cef.
        /// </summary>
        /// <param name="commandLineSettings"> Command line settings. </param>
        private void InitializeCef(Action<Dictionary<string, string>>? commandLineSettings)
        {
            if (!Cef.IsInitialized)
            {
                CefSharpSettings.LegacyJavascriptBindingEnabled      = false;
                CefSharpSettings.ConcurrentTaskExecution             = true;
                CefSharpSettings.ShutdownOnExit                      = true;
                CefSharpSettings.SubprocessExitIfParentProcessClosed = true;

                Cef.EnableHighDPISupport();

                CefSettings settings = new CefSettings
                {
                    BrowserSubprocessPath = Path.Combine(
                        AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                        Environment.Is64BitProcess ? "x64" : "x86",
                        "CefSharp.BrowserSubprocess.exe"),
                    MultiThreadedMessageLoop   = true,
                    LogSeverity                = LogSeverity.Error,
                    WindowlessRenderingEnabled = true
                };
                commandLineSettings?.Invoke(settings.CefCommandLineArgs);
                Cef.Initialize(settings, true, browserProcessHandler: null);
            }
        }

        #region IDisposable Support

        /// <inheritdoc/>
        ~CefWrapper()
        {
            Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Cef.IsInitialized)
            {
                Cef.Shutdown();
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}