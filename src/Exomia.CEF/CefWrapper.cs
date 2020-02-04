#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using System;
using System.IO;
using System.Reflection;
using CefSharp;
using CefSharp.OffScreen;

namespace Exomia.CEF
{
    /// <summary>
    ///     A cef wrapper for clean initialization and shutdown.
    /// </summary>
    public class CefWrapper : IDisposable
    {
        static CefWrapper()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
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
            };
            InitializeCef();
        }

        /// <summary>
        ///     Prevents a default instance of the <see cref="CefWrapper" /> class from being created.
        /// </summary>
        private CefWrapper() { }

        /// <summary>
        ///     Creates a new <see cref="CefWrapper" />.
        /// </summary>
        /// <returns>
        ///     An <see cref="IDisposable" />.
        /// </returns>
        public static IDisposable Create()
        {
            return new CefWrapper();
        }

        /// <summary>
        ///     Initializes the cef.
        /// </summary>
        private static void InitializeCef()
        {
            if (!Cef.IsInitialized)
            {
                CefSharpSettings.LegacyJavascriptBindingEnabled      = true;
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
                Cef.Initialize(settings, true, browserProcessHandler: null);
            }
        }

        #region IDisposable Support

        /// <inheritdoc />
        ~CefWrapper()
        {
            Dispose();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged/managed resources.
        /// </summary>
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