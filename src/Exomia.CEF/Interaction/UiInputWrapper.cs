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
        ///     The key down flag.
        /// </summary>
        private const int KEY_DOWN_FLAG = 1;

        /// <summary>
        ///     The key up flag.
        /// </summary>
        private const int KEY_UP_FLAG = 1 << 1;

        /// <summary>
        ///     The key press flag.
        /// </summary>
        private const int KEY_PRESS_FLAG = 1 << 2;

        /// <summary>
        ///     The key event flag.
        /// </summary>
        private const int KEY_EVENT_FLAG = 1 << 3;

        /// <summary>
        ///     The key all flag.
        /// </summary>
        private const int KEY_ALL_FLAG = KEY_DOWN_FLAG | KEY_UP_FLAG | KEY_PRESS_FLAG | KEY_EVENT_FLAG;

        /// <summary>
        ///     The mouse down flag.
        /// </summary>
        private const int MOUSE_DOWN_FLAG = 1 << 4;

        /// <summary>
        ///     The mouse up flag.
        /// </summary>
        private const int MOUSE_UP_FLAG = 1 << 5;

        /// <summary>
        ///     The mouse click flag.
        /// </summary>
        private const int MOUSE_CLICK_FLAG = 1 << 6;

        /// <summary>
        ///     The mouse move flag.
        /// </summary>
        private const int MOUSE_MOVE_FLAG = 1 << 7;

        /// <summary>
        ///     The mouse wheel flag.
        /// </summary>
        private const int MOUSE_WHEEL_FLAG = 1 << 8;

        /// <summary>
        ///     The mouse all flag.
        /// </summary>
        private const int MOUSE_ALL_FLAG =
            MOUSE_DOWN_FLAG | MOUSE_UP_FLAG | MOUSE_CLICK_FLAG | MOUSE_MOVE_FLAG | MOUSE_WHEEL_FLAG;

        /// <summary>
        ///     The state.
        /// </summary>
        private int _state = MOUSE_ALL_FLAG | KEY_ALL_FLAG;

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
            if ((_state & KEY_DOWN_FLAG) == KEY_DOWN_FLAG)
            {
                InputHandler?.KeyDown(keyValue, modifiers);
            }
        }

        /// <inheritdoc />
        void IInputHandler.KeyPress(char key)
        {
            if ((_state & KEY_PRESS_FLAG) == KEY_PRESS_FLAG)
            {
                InputHandler?.KeyPress(key);
            }
        }

        /// <inheritdoc />
        void IInputHandler.KeyUp(int keyValue, KeyModifier modifiers)
        {
            if ((_state & KEY_UP_FLAG) == KEY_UP_FLAG)
            {
                InputHandler?.KeyUp(keyValue, modifiers);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.KeyEvent(ref Message message)
        {
            if ((_state & KEY_EVENT_FLAG) == KEY_EVENT_FLAG)
            {
                InputHandler?.KeyEvent(ref message);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseDown(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_DOWN_FLAG) == MOUSE_DOWN_FLAG)
            {
                InputHandler?.MouseDown(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseUp(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_UP_FLAG) == MOUSE_UP_FLAG)
            {
                InputHandler?.MouseUp(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseClick(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_CLICK_FLAG) == MOUSE_CLICK_FLAG)
            {
                InputHandler?.MouseClick(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseMove(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_MOVE_FLAG) == MOUSE_MOVE_FLAG)
            {
                InputHandler?.MouseMove(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseWheel(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((_state & MOUSE_WHEEL_FLAG) == MOUSE_WHEEL_FLAG)
            {
                InputHandler?.MouseWheel(x, y, buttons, clicks, wheelDelta);
            }
        }

        /// <summary>
        ///     [vue-ui] call this function to disable input forwarding, for a specific flag, to the <see cref="InputHandler" />.
        /// </summary>
        /// <param name="flag"> the flag to set </param>
        /// <remarks>
        ///     flags:<br />
        ///         KEY_DOWN = 1,<br />
        ///         KEY_UP = 2,<br />
        ///         KEY_PRESS = 4,<br />
        ///         KEY_EVENT = 8,<br />
        ///         KEY_ALL = 15,<br />
        ///         MOUSE_DOWN = 16,<br />
        ///         MOUSE_UP = 32,<br />
        ///         MOUSE_CLICK = 64,<br />
        ///         MOUSE_MOVE = 128,<br />
        ///         MOUSE_WHEEL = 256,<br />
        ///         MOUSE_ALL = 496,<br />
        ///         ALL = 511<br />
        /// </remarks>
        public void SetFlag(int flag)
        {
            int state = _state;
            Interlocked.Exchange(ref _state, state | flag);
        }

        /// <summary>
        ///     [vue-ui] call this function to enable input forwarding, for a specific flag, to the <see cref="InputHandler" />.
        /// </summary>
        /// <param name="flag"> the flag to remove</param>
        /// <remarks>
        ///     <para>flags:</para>
        ///         <para>KEY_DOWN = 1,</para>
        ///         <para>KEY_UP = 2,</para>
        ///         <para>KEY_PRESS = 4,</para>
        ///         <para>KEY_EVENT = 8,</para>
        ///         <para>KEY_ALL = 15,</para>
        ///         <para>MOUSE_DOWN = 16,</para>
        ///         <para>MOUSE_UP = 32,</para>
        ///         <para>MOUSE_CLICK = 64,</para>
        ///         <para>MOUSE_MOVE = 128,</para>
        ///         <para>MOUSE_WHEEL = 256,</para>
        ///         <para>MOUSE_ALL = 496,</para>
        ///         <para>ALL = 511</para>
        /// </remarks>
        public void RemoveFlag(int flag)
        {
            int state = _state;
            Interlocked.Exchange(ref _state, state & ~flag);
        }
    }
}