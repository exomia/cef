#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

namespace Exomia.CEF.Interaction
{
    /// <summary>
    ///     Interface for user interface action handling.
    /// </summary>
    public interface IUiActionHandler
    {
        /// <summary>
        ///     Occurs when a trigger is invoked from javascript code.
        /// </summary>
        event TriggerHandler? Trigger;
    }
}