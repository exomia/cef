#region License

// Copyright (c) 2018-2021, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using System;
using CefSharp.OffScreen;

namespace Exomia.CEF
{
    /// <summary>
    ///     A cef settings wrapper. This class cannot be inherited.
    /// </summary>
    public sealed class CefSettingsWrapper : IDisposable
    {
        /// <summary>
        ///     The cef settings.
        /// </summary>
        public CefSettings Settings { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CefSettingsWrapper" /> class.
        /// </summary>
        /// <param name="cefSettings"> The cef settings. </param>
        internal CefSettingsWrapper(CefSettings cefSettings)
        {
            Settings = cefSettings;
        }

        /// <summary>
        ///     Implicit cast that converts the given CefSettingsWrapper to the CefSettings.
        /// </summary>
        /// <param name="wrapper"> The wrapper. </param>
        /// <returns>
        ///     The result <see cref="CefSharp.OffScreen.CefSettings" />
        /// </returns>
        public static implicit operator CefSettings(CefSettingsWrapper wrapper)
        {
            return wrapper.Settings;
        }

        #region IDisposable Support

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Settings.Dispose();
                }

                _disposed = true;
            }
        }

        /// <inheritdoc />
        ~CefSettingsWrapper()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged/managed resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}