#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using System;

namespace Exomia.CEF.UI
{
    /// <summary>
    ///     An ui action class. This class cannot be inherited.
    /// </summary>
    public sealed class UiActions : IJsUiActions, IUiActionHandler, IDisposable
    {
        /// <summary>
        ///     Occurs when User interface Action Handler. Trigger.
        /// </summary>
        /// ###
        /// <inheritdoc />
        event TriggerHandler? IUiActionHandler.Trigger
        {
            add { _trigger += value; }
            remove
            {
                if (_trigger != null)
                {
                    // ReSharper disable once DelegateSubtraction
                    _trigger -= value;
                }
            }
        }

        /// <summary>
        ///     The trigger.
        /// </summary>
        private TriggerHandler? _trigger;

        /// <inheritdoc />
        public void Trigger(int key, params object[] args)
        {
            _trigger?.Invoke(key, args);
        }

        #region IDisposable Support

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _trigger = null;
                }
                _disposed = true;
            }
        }

        /// <inheritdoc />
        ~UiActions()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged/managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}