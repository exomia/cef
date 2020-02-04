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
using Exomia.CEF.Custom;
using Exomia.CEF.UI;
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
    public sealed class ExomiaWebBrowser : ChromiumWebBrowser, IExomiaWebBrowser
    {
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
        ///     The texture.
        /// </summary>
        private Texture? _texture;

        /// <summary>
        ///     The graphics device.
        /// </summary>
        private IGraphicsDevice _graphicsDevice;

        /// <inheritdoc />
        public string Name
        {
            get { return _name; }
        }

        /// <inheritdoc />
        Texture? IExomiaWebBrowser.Texture
        {
            get { return _texture; }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExomiaWebBrowser" /> class.
        /// </summary>
        /// <param name="name">         The name. </param>
        /// <param name="baseUrl">      (Optional) URL of the base. </param>
        private ExomiaWebBrowser(string name, string baseUrl = "about:blank")
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
                    WebSecurity               = CefState.Enabled,
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

            ConsoleMessage += (sender, args) =>
            {
                Console.WriteLine("[{0}:{1}] [{2}] {3}", args.Level, args.Line, args.Source, args.Message);
            };

            JavascriptObjectRepository.ResolveObject += (sender, e) =>
            {
                if (e.ObjectName == null) { return; }
                switch (e.ObjectName)
                {
                    case "exui":
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
                    case "exuiinput":
                        {
                            lock (_services)
                            {
                                e.ObjectRepository.Register(
                                    e.ObjectName,
                                    _services.TryGetValue(typeof(IInputHandler), out object service)
                                        ? service
                                        : throw new KeyNotFoundException(
                                            $"No '{nameof(IInputHandler)}' available! Use the method '{nameof(IExomiaWebBrowser.SetUiInputHandler)}' first!"),
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

            JavascriptObjectRepository.ObjectBoundInJavascript += (sender, e) =>
            {
                Console.WriteLine(
                    $"Object '{e.ObjectName}' was bound successfully. (cached: {e.IsCached}; alreadyBound: {e.AlreadyBound})");
            };
        }

        /// <inheritdoc />
        public void ShowDevTools()
        {
            GetBrowser()
                .GetHost()
                .ShowDevTools();
        }

        /// <inheritdoc />
        public void CloseDevTools()
        {
            GetBrowser()
                .GetHost()
                .CloseDevTools();
        }

        /// <inheritdoc />
        IUiActionHandler IExomiaWebBrowser.CreateJsUiActions()
        {
            lock (_services)
            {
                if (_services.TryGetValue(typeof(IUiActionHandler), out object service))
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    JavascriptObjectRepository.UnRegister("exui");
                }
                UiActions uiActions = new UiActions();
                _services.Add(typeof(IUiActionHandler), uiActions);
                _services.Add(typeof(IJsUiActions), uiActions);
                return uiActions;
            }
        }

        /// <inheritdoc />
        void IExomiaWebBrowser.SetUiInputHandler(IInputHandler inputHandler)
        {
            lock (_services)
            {
                if (!_services.ContainsKey(typeof(IInputHandler)))
                {
                    _services.Add(typeof(IInputHandler), inputHandler);
                }
                else
                {
                    JavascriptObjectRepository.UnRegister("exuiinput");
                    _services[typeof(IInputHandler)] = inputHandler;
                }
            }
        }

        /// <inheritdoc />
        T IExomiaWebBrowser.AddUiCallback<T>(string name, T item)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }
            lock (_namedServices)
            {
                if (!_namedServices.ContainsKey(name))
                {
                    _namedServices.Add(name, item);
                }
                else
                {
                    if (_namedServices[name] is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    JavascriptObjectRepository.UnRegister(name);
                    _namedServices[name] = item;
                }
                return item;
            }
        }

        /// <inheritdoc />
        public void Initialize(IServiceRegistry registry)
        {
            _graphicsDevice = registry.GetService<IGraphicsDevice>();
            _graphicsDevice.ResizeFinished += v =>
            {
                GetBrowser().GetHost().NotifyMoveOrResizeStarted();
                Size = new Size((int)v.Width, (int)v.Height);
                GetBrowser().GetHost().WasResized();
            };
            Size = new Size((int)_graphicsDevice.Viewport.Width, (int)_graphicsDevice.Viewport.Height);

            if (!IsBrowserInitialized)
            {
                ManualResetEventSlim mre = new ManualResetEventSlim(IsBrowserInitialized);

                void OnBrowserInitialized(object sender, EventArgs e)
                {
                    mre.Set();
                }

                BrowserInitialized += OnBrowserInitialized;

                mre.Wait();

                BrowserInitialized -= OnBrowserInitialized;
            }

            Paint += OnPaint;
        }

        /// <inheritdoc />
        void IRawInputHandler.Input_KeyEvent(ref Message message)
        {
            GetBrowser()
                .GetHost()
                .SendKeyEvent(message.Msg, (int)message.WParam.ToInt64(), (int)message.LParam.ToInt64());
        }

        /// <inheritdoc />
        void IRawInputHandler.Input_MouseClick(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
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
        void IRawInputHandler.Input_MouseDown(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
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
        void IRawInputHandler.Input_MouseMove(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
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
            GetBrowser()
                .GetHost()
                .SendMouseMoveEvent(x, y, false, cefEventFlags);
        }

        /// <inheritdoc />
        void IRawInputHandler.Input_MouseUp(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
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
        void IRawInputHandler.Input_MouseWheel(int x, int y, MouseButtons buttons, int clicks, int wheelDelta)
        {
            GetBrowser()
                .GetHost()
                .SendMouseWheelEvent(x, y, 0, wheelDelta, CefEventFlags.None);
        }

        /// <summary>
        ///     Creates a new <see cref="IExomiaWebBrowser" />.
        /// </summary>
        /// <param name="name">         The name. </param>
        /// <param name="baseUrl">      (Optional) URL of the base. </param>
        /// <returns>
        ///     An <see cref="IExomiaWebBrowser" />.
        /// </returns>
        public static IExomiaWebBrowser Create(string name,
                                               string baseUrl = "about:blank")
        {
            return new ExomiaWebBrowser(name, baseUrl);
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
    }
}