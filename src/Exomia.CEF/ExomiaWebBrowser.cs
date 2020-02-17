#region License

// Copyright (c) 2018-2020, exomia
// All rights reserved.
// 
// This source code is licensed under the BSD-style license found in the
// LICENSE file in the root directory of this source tree.

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using CefSharp;
using CefSharp.OffScreen;
using Exomia.CEF.Interaction;
using Exomia.Framework;
using Exomia.Framework.Graphics;
using Exomia.Framework.Input;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Message = System.Windows.Forms.Message;
using MouseButtons = Exomia.Framework.Input.MouseButtons;

namespace Exomia.CEF
{
    /// <summary>
    ///     An exomia web browser. This class cannot be inherited.
    /// </summary>
    public sealed class ExomiaWebBrowser : ChromiumWebBrowser, IExomiaWebBrowser,
                                           IComponent, IInitializable, IInputHandler
    {
        /// <summary>
        ///     The exUi.
        /// </summary>
        private const string EX_UI = "exUi";

        /// <summary>
        ///     The exUiInput.
        /// </summary>
        private const string EX_UI_INPUT = "exUiInput";

        /// <summary>
        ///     The exUiStore.
        /// </summary>
        private const string EX_UI_STORE = "exUiStore";

        /// <summary>
        ///     The name.
        /// </summary>
        private readonly string _name;

        /// <summary>
        ///     The services.
        /// </summary>
        private readonly Dictionary<Type, object> _services;

        /// <summary>
        ///     The services.
        /// </summary>
        private readonly Dictionary<string, object> _namedServices;

        /// <summary>
        ///     The input wrapper.
        /// </summary>
        private readonly IUiInputWrapper _uiInputWrapper;

        /// <summary>
        ///     The texture.
        /// </summary>
        private Texture? _texture;

        /// <summary>
        ///     The graphics device.
        /// </summary>
        private IGraphicsDevice _graphicsDevice;

        /// <inheritdoc />
        string IComponent.Name
        {
            get { return _name; }
        }

        /// <inheritdoc />
        Texture? IExomiaWebBrowser.Texture
        {
            get { return _texture; }
        }

        /// <inheritdoc />
        IInputHandler IExomiaWebBrowser.InputHandler
        {
            get { return this; }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExomiaWebBrowser" /> class.
        /// </summary>
        /// <param name="name">    The name. </param>
        /// <param name="baseUrl"> (Optional) URL of the base. </param>
        /// <param name="debug">   (Optional) True to debug. </param>
        private ExomiaWebBrowser(string name, string baseUrl = "about:blank", bool debug = false)
            : base(
                baseUrl,
                new BrowserSettings
                {
                    WindowlessFrameRate       = 60,
                    Javascript                = CefState.Enabled,
                    JavascriptCloseWindows    = CefState.Disabled,
                    JavascriptAccessClipboard = CefState.Disabled,
                    JavascriptDomPaste        = CefState.Enabled,
                    ImageLoading              = CefState.Enabled,
                    WebSecurity               = CefState.Disabled,
                    LocalStorage              = CefState.Disabled,
                    RemoteFonts               = CefState.Disabled,
                    WebGl                     = CefState.Disabled,
                    Plugins                   = CefState.Disabled
                })
        {
            if (!Cef.IsInitialized)
            {
                throw new Exception(
                    $"{nameof(CefWrapper)} must be created before the {nameof(ExomiaWebBrowser)} is available!");
            }
            _name           = name;
            _graphicsDevice = null!;

            _services      = new Dictionary<Type, object>();
            _namedServices = new Dictionary<string, object>();

            MenuHandler = new CustomContextMenuHandler();

            if (debug)
            {
                ConsoleMessage += (sender, args) =>
                {
                    Console.WriteLine("[{0}:{1}] [{2}] {3}", args.Level, args.Line, args.Source, args.Message);
                };
                JavascriptObjectRepository.ObjectBoundInJavascript += (sender, e) =>
                {
                    Console.WriteLine(
                        $"Object '{e.ObjectName}' was bound successfully. (cached: {e.IsCached}; alreadyBound: {e.AlreadyBound})");
                };
            }

            _uiInputWrapper = new UiInputWrapper();
            JavascriptObjectRepository.ResolveObject += (sender, e) =>
            {
                if (e.ObjectName == null) { return; }
                switch (e.ObjectName)
                {
                    case EX_UI:
                        {
                            lock (_services)
                            {
                                e.ObjectRepository.Register(
                                    e.ObjectName,
                                    _services.TryGetValue(typeof(IJsUiActions), out object service)
                                        ? service
                                        : throw new KeyNotFoundException(
                                            $"No '{nameof(IJsUiActions)}' created! Use the method '{nameof(IExomiaWebBrowser.CreateJsUiActions)}' first!"),
                                    true,
                                    BindingOptions.DefaultBinder);
                            }
                        }
                        break;
                    case EX_UI_INPUT:
                        {
                            JavascriptObjectRepository.Register(
                                EX_UI_INPUT, _uiInputWrapper, true, BindingOptions.DefaultBinder);
                        }
                        break;
                    case EX_UI_STORE:
                        {
                            lock (_services)
                            {
                                e.ObjectRepository.Register(
                                    e.ObjectName,
                                    _services.TryGetValue(typeof(IJsUiStore), out object service)
                                        ? service
                                        : throw new KeyNotFoundException(
                                            $"No '{nameof(IJsUiStore)}' available! Use the method '{nameof(IExomiaWebBrowser.SetJsUiStore)}' first!"),
                                    true,
                                    BindingOptions.DefaultBinder);
                            }
                        }
                        break;
                    default:
                        {
                            lock (_namedServices)
                            {
                                e.ObjectRepository.Register(
                                    e.ObjectName,
                                    _namedServices.TryGetValue(e.ObjectName, out object service)
                                        ? service
                                        : throw new KeyNotFoundException(
                                            $"No ui callback item found! Use the method '{nameof(IExomiaWebBrowser.AddUiCallback)}' first!"),
                                    true,
                                    BindingOptions.DefaultBinder);
                            }
                        }
                        break;
                }
            };
        }

        /// <inheritdoc />
        IUiActionHandler IExomiaWebBrowser.CreateJsUiActions()
        {
            lock (_services)
            {
                UiActions uiActions = new UiActions();
                if (_services.TryGetValue(typeof(IUiActionHandler), out object service))
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    JavascriptObjectRepository.UnRegister(EX_UI);
                    _services[typeof(IUiActionHandler)] = uiActions;
                    _services[typeof(IJsUiActions)]     = uiActions;
                }
                else
                {
                    _services.Add(typeof(IUiActionHandler), uiActions);
                    _services.Add(typeof(IJsUiActions), uiActions);
                }

                return uiActions;
            }
        }

        /// <inheritdoc />
        void IExomiaWebBrowser.SetUiInputHandler(IInputHandler? inputHandler)
        {
            _uiInputWrapper.InputHandler = inputHandler;
        }

        /// <inheritdoc />
        void IExomiaWebBrowser.SetJsUiStore(IJsUiStore? jsUiStore)
        {
            lock (_services)
            {
                if (_services.TryGetValue(typeof(IJsUiStore), out object service))
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    JavascriptObjectRepository.UnRegister(EX_UI_STORE);
                    if (jsUiStore != null)
                    {
                        _services[typeof(IJsUiStore)] = jsUiStore;
                    }
                }
                else
                {
                    if (jsUiStore != null)
                    {
                        _services.Add(typeof(IJsUiStore), jsUiStore);
                    }
                }
            }
        }

        /// <inheritdoc />
        T IExomiaWebBrowser.AddUiCallback<T>(string name, T item)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }
            lock (_namedServices)
            {
                if (_namedServices.TryGetValue(name, out object service))
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    JavascriptObjectRepository.UnRegister(name);
                    _namedServices[name] = item;
                }
                else
                {
                    _namedServices.Add(name, item);
                }
                return item;
            }
        }

        /// <inheritdoc />
        void IInitializable.Initialize(IServiceRegistry registry)
        {
            _graphicsDevice = registry.GetService<IGraphicsDevice>();
            _graphicsDevice.ResizeFinished += v =>
            {
                GetBrowser().GetHost().NotifyMoveOrResizeStarted();
                Size = new Size((int)v.Width, (int)v.Height);
                GetBrowser().GetHost().WasResized();
            };
            Size = new Size((int)_graphicsDevice.Viewport.Width, (int)_graphicsDevice.Viewport.Height);

            while (!IsBrowserInitialized)
            {
                ManualResetEventSlim mre = new ManualResetEventSlim(IsBrowserInitialized);

                void OnBrowserInitialized(object sender, EventArgs e)
                {
                    mre.Set();
                }

                BrowserInitialized += OnBrowserInitialized;

                mre.Wait(2000);

                BrowserInitialized -= OnBrowserInitialized;
            }

            Paint += OnPaint;
        }

        /// <summary>
        ///     Creates an new instance of <see cref="ExomiaWebBrowser" />.
        /// </summary>
        /// <param name="name"> The name. </param>
        /// <param name="baseUrl"> (Optional) URL of the base. </param>
        /// <param name="debug">   (Optional) True to debug. </param>
        /// <returns>
        ///     An <see cref="IExomiaWebBrowser" />.
        /// </returns>
        public static IExomiaWebBrowser Create(string name,
                                               string baseUrl = "about:blank",
                                               bool   debug   = false)
        {
            return new ExomiaWebBrowser(name, baseUrl, debug);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) { Paint -= OnPaint; }
            base.Dispose(disposing);
        }

        /// <summary>
        ///     The ui paint event.
        /// </summary>
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information to send to registered event handlers. </param>
        private void OnPaint(object sender, OnPaintEventArgs e)
        {
            using Texture2D tex = new Texture2D(
                _graphicsDevice.Device,
                new Texture2DDescription
                {
                    Format            = Format.B8G8R8A8_UNorm,
                    ArraySize         = 1,
                    MipLevels         = 1,
                    Width             = e.Width,
                    Height            = e.Height,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags         = BindFlags.ShaderResource,
                    CpuAccessFlags    = CpuAccessFlags.None,
                    OptionFlags       = ResourceOptionFlags.None,
                    Usage             = ResourceUsage.Default
                }, new DataRectangle(e.BufferHandle, e.Width * 4));
            Interlocked.Exchange(
                           ref _texture,
                           new Texture(
                               new ShaderResourceView1(_graphicsDevice.Device, tex), e.Width, e.Height))
                       ?.Dispose();
        }

        #region input handling

        /// <inheritdoc />
        void IInputHandler.KeyDown(int keyValue, KeyModifier modifiers)
        {
            _uiInputWrapper.KeyDown(keyValue, modifiers);
        }

        /// <inheritdoc />
        void IInputHandler.KeyPress(char key)
        {
            _uiInputWrapper.KeyPress(key);
        }

        /// <inheritdoc />
        void IInputHandler.KeyUp(int keyValue, KeyModifier modifiers)
        {
            _uiInputWrapper.KeyUp(keyValue, modifiers);
        }

        /// <inheritdoc />
        void IRawInputHandler.KeyEvent(ref Message message)
        {
            _uiInputWrapper.KeyEvent(ref message);
            GetBrowser()
                .GetHost()
                .SendKeyEvent(message.Msg, (int)message.WParam.ToInt64(), (int)message.LParam.ToInt64());
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseClick(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            _uiInputWrapper.MouseClick(x, y, buttons, clicks, wheelDelta);
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseDown(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            _uiInputWrapper.MouseDown(x, y, buttons, clicks, wheelDelta);
            if ((buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                GetBrowser()
                    .GetHost()
                    .SendMouseClickEvent(x, y, MouseButtonType.Left, false, clicks, CefEventFlags.LeftMouseButton);
            }
            if ((buttons & MouseButtons.Middle) == MouseButtons.Middle)
            {
                GetBrowser()
                    .GetHost()
                    .SendMouseClickEvent(x, y, MouseButtonType.Middle, false, clicks, CefEventFlags.MiddleMouseButton);
            }
            if ((buttons & MouseButtons.Right) == MouseButtons.Right)
            {
                GetBrowser()
                    .GetHost()
                    .SendMouseClickEvent(x, y, MouseButtonType.Right, false, clicks, CefEventFlags.RightMouseButton);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseMove(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            _uiInputWrapper.MouseMove(x, y, buttons, clicks, wheelDelta);
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
            GetBrowser()
                .GetHost()
                .SendMouseMoveEvent(x, y, false, cefEventFlags);
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseUp(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            _uiInputWrapper.MouseUp(x, y, buttons, clicks, wheelDelta);
            if ((buttons & MouseButtons.Left) == MouseButtons.Left)
            {
                GetBrowser()
                    .GetHost()
                    .SendMouseClickEvent(x, y, MouseButtonType.Left, true, clicks, CefEventFlags.LeftMouseButton);
            }
            if ((buttons & MouseButtons.Middle) == MouseButtons.Middle)
            {
                GetBrowser()
                    .GetHost()
                    .SendMouseClickEvent(x, y, MouseButtonType.Middle, true, clicks, CefEventFlags.MiddleMouseButton);
            }
            if ((buttons & MouseButtons.Right) == MouseButtons.Right)
            {
                GetBrowser()
                    .GetHost()
                    .SendMouseClickEvent(x, y, MouseButtonType.Right, true, clicks, CefEventFlags.RightMouseButton);
            }
        }

        /// <inheritdoc />
        void IRawInputHandler.MouseWheel(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            _uiInputWrapper.MouseWheel(x, y, buttons, clicks, wheelDelta);
            GetBrowser()
                .GetHost()
                .SendMouseWheelEvent(x, y, 0, wheelDelta, CefEventFlags.None);
        }

        #endregion
    }
}