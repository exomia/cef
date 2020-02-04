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
    ///     Handler, called when it is invoked from javascript code.
    /// </summary>
    /// <param name="key">  The key. </param>
    /// <param name="args"> The arguments. </param>
    public delegate void TriggerHandler(int key, object[] args);
}