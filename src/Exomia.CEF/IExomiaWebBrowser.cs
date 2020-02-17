#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using CefSharp;
using Exomia.CEF.Interaction;
using Exomia.Framework.Graphics;
using Exomia.Framework.Input;

namespace Exomia.CEF
{
    /// <summary>
    ///     Interface for exomia web browser.
    /// </summary>
    public interface IExomiaWebBrowser : IWebBrowser
    {
        /// <summary>
        ///     Gets the texture.
        /// </summary>
        /// <value>
        ///     The texture.
        /// </value>
        Texture? Texture { get; }

        /// <summary>
        ///     Gets the input handler.
        /// </summary>
        /// <value>
        ///     The input handler.
        /// </value>
        IInputHandler InputHandler { get; }

        /// <summary>
        ///     Creates js user interface actions.
        /// </summary>
        /// <returns>
        ///     The new js user interface actions.
        /// </returns>
        IUiActionHandler CreateJsUiActions();

        /// <summary>
        ///     Set the user interface input handler.
        /// </summary>
        /// <param name="inputHandler"> The input handler. </param>
        void SetUiInputHandler(IInputHandler? inputHandler);

        /// <summary>
        ///     Sets the js user interface store.
        /// </summary>
        /// <param name="jsUiStore"> The js user interface store. </param>
        void SetJsUiStore(IJsUiStore? jsUiStore);

        /// <summary>
        ///     Adds a user interface callback item.
        /// </summary>
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="name"> The name. </param>
        /// <param name="item"> The item. </param>
        /// <returns>
        ///     A T.
        /// </returns>
        T AddUiCallback<T>(string name, T item);
    }
}