#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using System.Collections.Generic;

namespace Exomia.CEF.Interaction
{
    /// <summary>
    ///     Interface for js user interface store.
    /// </summary>
    public interface IJsUiStore
    {
        /// <summary>
        ///     Initial state.
        /// </summary>
        /// <returns>
        ///     An <see cref="IDictionary{TKey,TValue}" />
        /// </returns>
        IDictionary<string, object> GetState();
    }
}