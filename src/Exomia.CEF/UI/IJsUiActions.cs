#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

namespace Exomia.CEF.UI
{
    /// <summary>
    ///     Interface to communicate with the c# backend.
    /// </summary>
    public interface IJsUiActions
    {
        /// <summary>
        ///     Triggers.
        /// </summary>
        /// <param name="key">  The key. </param>
        /// <param name="args"> A variable-length parameters list containing arguments. </param>
        void Trigger(int key, params object[] args);
    }
}