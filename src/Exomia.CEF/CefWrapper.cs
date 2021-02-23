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
    public sealed class CefWrapper : IDisposable
    {
        /// <summary>
        ///     The cef settings.
        /// </summary>
        private readonly CefSettingsWrapper? _wrapper;

        /// <summary>
        ///     Initializes static members of the <see cref="CefWrapper" /> class.
        /// </summary>
        static CefWrapper()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, args) =>
            {
                string archSpecificPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    Environment.Is64BitProcess ? "x64" : "x86", args.Name.Split(new[] { ',' }, 2)[0] + ".dll");
                return File.Exists(archSpecificPath)
                    ? Assembly.LoadFile(archSpecificPath)
                    : null;
            };
        }

        /// <summary>
        ///     Prevents a default instance of the <see cref="CefWrapper" /> class from being created.
        /// </summary>
        /// <param name="overrideCefSettings"> Override cef settings. </param>
        private CefWrapper(Action<CefSettingsWrapper>? overrideCefSettings)
        {
            if (!Cef.IsInitialized)
            {
                CefSharpSettings.ConcurrentTaskExecution             = true;
                CefSharpSettings.ShutdownOnExit                      = true;
                CefSharpSettings.SubprocessExitIfParentProcessClosed = true;

                Cef.EnableHighDPISupport();

                _wrapper = new CefSettingsWrapper(
                    new CefSettings
                    {
                        BrowserSubprocessPath = Path.Combine(
                            AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                            Environment.Is64BitProcess ? "x64" : "x86",
                            "CefSharp.BrowserSubprocess.exe"),
                        MultiThreadedMessageLoop   = true,
                        LogSeverity                = LogSeverity.Error,
                        WindowlessRenderingEnabled = true
                    });
                ;
                overrideCefSettings?.Invoke(_wrapper);
                Cef.Initialize(_wrapper, true, browserProcessHandler: null);
            }
        }

        /// <summary>
        ///     Creates a new <see cref="CefWrapper" />.
        /// </summary>
        /// <param name="overrideCefSettings"> (Optional) Override cef settings. </param>
        /// <returns>
        ///     An <see cref="IDisposable" />.
        /// </returns>
        public static IDisposable Create(Action<CefSettingsWrapper>? overrideCefSettings = null)
        {
            return new CefWrapper(overrideCefSettings);
        }

        #region IDisposable Support

        /// <inheritdoc />
        ~CefWrapper()
        {
            Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Cef.IsInitialized)
            {
                if (_wrapper is IDisposable d)
                {
                    d.Dispose();
                }
                Cef.Shutdown();
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}