﻿#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using Exomia.Framework.Input;

namespace Exomia.CEF.Interaction
{
    /// <summary>
    ///     Interface for user interface input wrapper.
    /// </summary>
    interface IUiInputWrapper : IInputHandler
    {
        /// <summary>
        ///     Gets or sets the input handler.
        /// </summary>
        /// <value>
        ///     The input handler.
        /// </value>
        IInputHandler? InputHandler { get; set; }
    }
}