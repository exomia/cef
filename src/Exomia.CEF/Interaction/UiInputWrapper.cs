#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using System.Threading;
using System.Windows.Forms;
using CefSharp;
using Exomia.Framework.Input;
using MouseButtons = Exomia.Framework.Input.MouseButtons;

namespace Exomia.CEF.Interaction
{
    /// <summary>
    ///     An input wrapper. This class cannot be inherited.
    /// </summary>
    public sealed class UiInputWrapper : IUiInputWrapper
    {
        private const int KEY_DOWN_FLAG  = 1;
        private const int KEY_UP_FLAG    = 1 << 1;
        private const int KEY_PRESS_FLAG = 1 << 2;
        private const int KEY_EVENT_FLAG = 1 << 3;
        private const int KEY_ALL_FLAG   = KEY_DOWN_FLAG | KEY_UP_FLAG | KEY_PRESS_FLAG | KEY_EVENT_FLAG;

        private const int MOUSE_DOWN_FLAG  = 1 << 4;
        private const int MOUSE_UP_FLAG    = 1 << 5;
        private const int MOUSE_CLICK_FLAG = 1 << 6;
        private const int MOUSE_MOVE_FLAG  = 1 << 7;
        private const int MOUSE_WHEEL_FLAG = 1 << 8;

        private const int MOUSE_ALL_FLAG =
            MOUSE_DOWN_FLAG | MOUSE_UP_FLAG | MOUSE_CLICK_FLAG | MOUSE_MOVE_FLAG | MOUSE_WHEEL_FLAG;

        private readonly IBrowserHost _host;

        private int _state = MOUSE_ALL_FLAG | KEY_ALL_FLAG;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UiInputWrapper" /> class.
        /// </summary>
        /// <param name="host"> The host. </param>
        public UiInputWrapper(IBrowserHost host)
        {
            _host = host;
        }

        /// <summary>
        ///     Registers the input described by device.
        /// </summary>
        /// <param name="device"> The device. </param>
        public void RegisterInput(IInputDevice device)
        {
            device.RegisterKeyDown(KeyDown);
            device.RegisterKeyUp(KeyUp);
            device.RegisterKeyPress(KeyPress);
            device.RegisterRawKeyEvent(RawKeyEvent);
            device.RegisterMouseDown(MouseDown);
            device.RegisterMouseUp(MouseUp);
            device.RegisterMouseClick(MouseClick);
            device.RegisterMouseMove(MouseMove);
            device.RegisterMouseWheel(MouseWheel);
        }

        /// <summary>
        ///     Unregister the input described by device.
        /// </summary>
        /// <param name="device"> The device. </param>
        public void UnregisterInput(IInputDevice device)
        {
            device.UnregisterKeyDown(KeyDown);
            device.UnregisterKeyUp(KeyUp);
            device.UnregisterKeyPress(KeyPress);
            device.UnregisterRawKeyEvent(RawKeyEvent);
            device.UnregisterMouseDown(MouseDown);
            device.UnregisterMouseUp(MouseUp);
            device.UnregisterMouseClick(MouseClick);
            device.UnregisterMouseMove(MouseMove);
            device.UnregisterMouseWheel(MouseWheel);
        }

        /// <summary>
        ///     [vue-ui] call this function to disable input forwarding, for a specific flag.
        /// </summary>
        /// <param name="flag"> the flag to set. </param>
        /// <remarks>
        ///     \tflags:
        ///     <para />
        ///     \t\tKEY_DOWN = 1,
        ///     <para />
        ///     \t\tKEY_UP = 2,
        ///     <para />
        ///     \t\tKEY_PRESS = 4,
        ///     <para />
        ///     \t\tKEY_EVENT = 8,
        ///     <para />
        ///     \t\tKEY_ALL = 15,
        ///     <para />
        ///     \t\tMOUSE_DOWN = 16,
        ///     <para />
        ///     \t\tMOUSE_UP = 32,
        ///     <para />
        ///     \t\tMOUSE_CLICK = 64,
        ///     <para />
        ///     \t\tMOUSE_MOVE = 128,
        ///     <para />
        ///     \t\tMOUSE_WHEEL = 256,
        ///     <para />
        ///     \t\tMOUSE_ALL = 496,
        ///     <para />
        ///     \t\tALL = 511.
        /// </remarks>
        public void SetFlag(int flag)
        {
            int state = _state;
            Interlocked.Exchange(ref _state, state | flag);
        }

        /// <summary>
        ///     [vue-ui] call this function to enable input forwarding, for a specific flag.
        /// </summary>
        /// <param name="flag"> the flag to remove. </param>
        /// <remarks>
        ///     \tflags:
        ///     <para />
        ///     \t\tKEY_DOWN = 1,
        ///     <para />
        ///     \t\tKEY_UP = 2,
        ///     <para />
        ///     \t\tKEY_PRESS = 4,
        ///     <para />
        ///     \t\tKEY_EVENT = 8,
        ///     <para />
        ///     \t\tKEY_ALL = 15,
        ///     <para />
        ///     \t\tMOUSE_DOWN = 16,
        ///     <para />
        ///     \t\tMOUSE_UP = 32,
        ///     <para />
        ///     \t\tMOUSE_CLICK = 64,
        ///     <para />
        ///     \t\tMOUSE_MOVE = 128,
        ///     <para />
        ///     \t\tMOUSE_WHEEL = 256,
        ///     <para />
        ///     \t\tMOUSE_ALL = 496,
        ///     <para />
        ///     \t\tALL = 511.
        /// </remarks>
        public void RemoveFlag(int flag)
        {
            int state = _state;
            Interlocked.Exchange(ref _state, state & ~flag);
        }

        private bool KeyDown(int keyValue, KeyModifier modifiers)
        {
            return (_state & KEY_DOWN_FLAG) == KEY_DOWN_FLAG;
        }

        private bool KeyPress(char key)
        {
            return (_state & KEY_PRESS_FLAG) == KEY_PRESS_FLAG;
        }

        private bool KeyUp(int keyValue, KeyModifier modifiers)
        {
            return (_state & KEY_UP_FLAG) == KEY_UP_FLAG;
        }

        private bool RawKeyEvent(in Message message)
        {
            _host.SendKeyEvent(message.Msg, (int)message.WParam.ToInt64(), (int)message.LParam.ToInt64());
            return (_state & KEY_EVENT_FLAG) == KEY_EVENT_FLAG;
        }

        private bool MouseDown(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                _host.SendMouseClickEvent(x, y, MouseButtonType.Left, false, clicks, CefEventFlags.LeftMouseButton);
            }
            if ((buttons & MouseButtons.Middle) == MouseButtons.Middle)
            {
                _host.SendMouseClickEvent(x, y, MouseButtonType.Middle, false, clicks, CefEventFlags.MiddleMouseButton);
            }
            if ((buttons & MouseButtons.Right) == MouseButtons.Right)
            {
                _host.SendMouseClickEvent(x, y, MouseButtonType.Right, false, clicks, CefEventFlags.RightMouseButton);
            }
            return (_state & MOUSE_DOWN_FLAG) == MOUSE_DOWN_FLAG;
        }

        private bool MouseUp(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            if ((buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                _host.SendMouseClickEvent(x, y, MouseButtonType.Left, true, clicks, CefEventFlags.LeftMouseButton);
            }
            if ((buttons & MouseButtons.Middle) == MouseButtons.Middle)
            {
                _host.SendMouseClickEvent(x, y, MouseButtonType.Middle, true, clicks, CefEventFlags.MiddleMouseButton);
            }
            if ((buttons & MouseButtons.Right) == MouseButtons.Right)
            {
                _host.SendMouseClickEvent(x, y, MouseButtonType.Right, true, clicks, CefEventFlags.RightMouseButton);
            }
            return (_state & MOUSE_UP_FLAG) == MOUSE_UP_FLAG;
        }

        private bool MouseClick(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            return (_state & MOUSE_CLICK_FLAG) == MOUSE_CLICK_FLAG;
        }

        private bool MouseMove(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            CefEventFlags cefEventFlags = CefEventFlags.None;
            if ((buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                cefEventFlags |= CefEventFlags.LeftMouseButton;
            }
            if ((buttons & MouseButtons.Middle) == MouseButtons.Middle)
            {
                cefEventFlags |= CefEventFlags.MiddleMouseButton;
            }
            if ((buttons & MouseButtons.Right) == MouseButtons.Right)
            {
                cefEventFlags |= CefEventFlags.RightMouseButton;
            }
            _host.SendMouseMoveEvent(x, y, false, cefEventFlags);
            return (_state & MOUSE_MOVE_FLAG) == MOUSE_MOVE_FLAG;
        }

        private bool MouseWheel(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            _host.SendMouseWheelEvent(x, y, 0, wheelDelta, CefEventFlags.None);
            return (_state & MOUSE_WHEEL_FLAG) == MOUSE_WHEEL_FLAG;
        }
    }
}