#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using System.Threading;
using System.Windows.Forms;
using Exomia.Framework.Input;
using MouseButtons = Exomia.Framework.Input.MouseButtons;

namespace Exomia.CEF.Interaction
{
    /// <summary>
    ///     An input wrapper. This class cannot be inherited.
    /// </summary>
    public sealed class UiInputWrapper : IUiInputWrapper
    {
        /// <summary>
        ///     The mouse flag.
        /// </summary>
        private const byte MOUSE_FLAG = 0b0000_0001;

        /// <summary>
        ///     The key flag.
        /// </summary>
        private const byte KEY_FLAG = 0b0000_0010;

        /// <summary>
        ///     The state.
        /// </summary>
        private int _state = MOUSE_FLAG | KEY_FLAG;

        /// <summary>
        ///     The input handler.
        /// </summary>
        /// <value>
        ///     The input handler.
        /// </value>
        public IInputHandler? InputHandler { get; set; }

        /// <inheritdoc />
        void IInputHandler.KeyDown(int keyValue, KeyModifier modifiers)
        {
            if ((_state & KEY_FLAG) == KEY_FLAG)
            {
                InputHandler?.KeyDown(keyValue, modifiers);
            }
        }

        /// <inheritdoc />
        void IInputHandler.KeyPress(char key)
        {
            if ((_state & KEY_FLAG) == KEY_FLAG)
            {
                InputHandler?.KeyPress(key);
            }
        }

        /// <inheritdoc />
        void IInputHandler.KeyUp(int keyValue, KeyModifier modifiers)
        {
            if ((_state & KEY_FLAG) == KEY_FLAG)
            {
                InputHandler?.KeyUp(keyValue, modifiers);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.KeyEvent(ref Message message)
        {
            if ((_state & KEY_FLAG) == KEY_FLAG)
            {
                InputHandler?.KeyEvent(ref message);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseClick(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_FLAG) == MOUSE_FLAG)
            {
                InputHandler?.MouseClick(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseDown(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_FLAG) == MOUSE_FLAG)
            {
                InputHandler?.MouseDown(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseMove(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_FLAG) == MOUSE_FLAG)
            {
                InputHandler?.MouseMove(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseUp(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_FLAG) == MOUSE_FLAG)
            {
                InputHandler?.MouseUp(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseWheel(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_FLAG) == MOUSE_FLAG)
            {
                InputHandler?.MouseWheel(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <summary>
        ///     [vue-ui] call this function to set the focus on an ui element and stop input forwarding.
        /// </summary>
        /// <param name="uiFocus"> True to focus an ui element. </param>
        public void SetFocus(bool uiFocus)
        {
            if (uiFocus)
            {
                Interlocked.CompareExchange(ref _state, 0, MOUSE_FLAG | KEY_FLAG);
            }
            else
            {
                Interlocked.CompareExchange(ref _state, MOUSE_FLAG | KEY_FLAG, 0);
            }
        }
    }
}