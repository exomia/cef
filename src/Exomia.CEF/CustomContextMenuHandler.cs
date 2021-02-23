#region License

// Copyright (c) 2018-2021, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using CefSharp;

namespace Exomia.CEF
{
    class CustomContextMenuHandler : IContextMenuHandler
    {
        /// <inheritdoc />
        public void OnBeforeContextMenu(IWebBrowser        chromiumWebBrowser,
                                        IBrowser           browser,
                                        IFrame             frame,
                                        IContextMenuParams parameters,
                                        IMenuModel         model)
        {
            model.Clear();
        }

        /// <inheritdoc />
        public bool OnContextMenuCommand(IWebBrowser        chromiumWebBrowser,
                                         IBrowser           browser,
                                         IFrame             frame,
                                         IContextMenuParams parameters,
                                         CefMenuCommand     commandId,
                                         CefEventFlags      eventFlags)
        {
            return false;
        }

        /// <inheritdoc />
        public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame) { }

        /// <inheritdoc />
        public bool RunContextMenu(IWebBrowser             chromiumWebBrowser,
                                   IBrowser                browser,
                                   IFrame                  frame,
                                   IContextMenuParams      parameters,
                                   IMenuModel              model,
                                   IRunContextMenuCallback callback)
        {
            return false;
        }
    }
}