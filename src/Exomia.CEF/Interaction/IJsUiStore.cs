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
        ///     [vue-ui] call this function to retrieve a state of the given module by name.
        /// </summary>
        /// <param name="moduleName"> The module name to get the state from </param>
        /// <returns>
        ///     An <see cref="IDictionary{TKey,TValue}" />
        /// </returns>
        IDictionary<string, object> GetState(string moduleName);
    }
}