#region License

// Copyright (c) 2018-2021, exomia
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

namespace Exomia.CEF
{
    /// <summary>
    ///     An exomia web browser. This class cannot be inherited.
    /// </summary>
    public sealed class ExomiaWebBrowser : ChromiumWebBrowser, IExomiaWebBrowser,
                                           IComponent, IInitializable, IInputHandler
    {
        private const string EX_UI       = "exUi";
        private const string EX_UI_INPUT = "exUiInput";
        private const string EX_UI_STORE = "exUiStore";

        private readonly string                     _name;
        private readonly Dictionary<Type, object>   _services;
        private readonly Dictionary<string, object> _namedServices;
        private          IUiInputWrapper            _uiInputWrapper = null!;
        private          Texture?                   _texture;
        private          IGraphicsDevice            _graphicsDevice = null!;

        string IComponent.Name
        {
            get { return _name; }
        }

        Texture? IExomiaWebBrowser.Texture
        {
            get { return _texture; }
        }

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
                    JavascriptAccessClipboard = CefState.Enabled,
                    JavascriptDomPaste        = CefState.Enabled,
                    ImageLoading              = CefState.Enabled,
                    WebSecurity               = CefState.Disabled,
                    LocalStorage              = CefState.Enabled,
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

            _name = name;

            _services      = new Dictionary<Type, object>();
            _namedServices = new Dictionary<string, object>();

            MenuHandler = new CustomContextMenuHandler();

            SetupDebug(debug);
        }

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

                    // ReSharper disable once HeapView.PossibleBoxingAllocation
                    _namedServices[name] = item;
                }
                else
                {
                    // ReSharper disable once HeapView.PossibleBoxingAllocation
                    _namedServices.Add(name, item);
                }
                return item;
            }
        }

        /// <inheritdoc />
        void IInitializable.Initialize(IServiceRegistry registry)
        {
            _graphicsDevice = registry.GetService<IGraphicsDevice>();

            // ReSharper disable once HeapView.DelegateAllocation
            // ReSharper disable once HeapView.ClosureAllocation
            _graphicsDevice.ResizeFinished += v =>
            {
                GetBrowser().GetHost().NotifyMoveOrResizeStarted();
                Size = new Size((int)v.Width, (int)v.Height);
                GetBrowser().GetHost().WasResized();
            };
            Size = new Size((int)_graphicsDevice.Viewport.Width, (int)_graphicsDevice.Viewport.Height);

            // ReSharper disable once HeapView.DelegateAllocation
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
            ExomiaWebBrowser browser = new ExomiaWebBrowser(name, baseUrl, debug);
            browser.Initialize();
            return browser;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // ReSharper disable once HeapView.DelegateAllocation
                Paint -= OnPaint;

                static void DisposeDictionary<T>(IDictionary<T, object> dictionary)
                {
                    // ReSharper disable once HeapView.ObjectAllocation.Possible
                    foreach (object v in dictionary.Values)
                    {
                        if (v is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                    dictionary.Clear();
                }

                lock (_services)
                {
                    DisposeDictionary(_services);
                }
                lock (_namedServices)
                {
                    DisposeDictionary(_namedServices);
                }
            }
            base.Dispose(disposing);
        }

        internal void Initialize()
        {
            while (!IsBrowserInitialized)
            {
                // ReSharper disable once HeapView.ClosureAllocation
                ManualResetEventSlim mre = new ManualResetEventSlim(IsBrowserInitialized);

                void OnBrowserInitialized(object sender, EventArgs e)
                {
                    mre.Set();
                }

                // ReSharper disable once HeapView.DelegateAllocation
                BrowserInitialized += OnBrowserInitialized;

                mre.Wait(2000);

                // ReSharper disable once HeapView.DelegateAllocation
                BrowserInitialized -= OnBrowserInitialized;
            }

            _uiInputWrapper = new UiInputWrapper(GetBrowser().GetHost());

            SetupJavascriptObjectRepository();
        }

        private void SetupDebug(bool debug)
        {
            if (debug)
            {
                // ReSharper disable once HeapView.ClosureAllocation
                ConsoleMessage += (sender, args) =>
                {
                    // ReSharper disable once HeapView.BoxingAllocation
                    // ReSharper disable once HeapView.ObjectAllocation
                    Console.WriteLine(
                        "[{0}:{1}] [{2}] {3}", args.Level.ToString(), args.Line.ToString(), args.Source, args.Message);
                };
                JavascriptObjectRepository.ObjectBoundInJavascript += (sender, e) =>
                {
                    Console.WriteLine(
                        $"Object '{e.ObjectName}' was bound successfully. (cached: {e.IsCached.ToString()}; alreadyBound: {e.AlreadyBound.ToString()})");
                };
            }
        }

        private void SetupJavascriptObjectRepository()
        {
            // ReSharper disable once HeapView.ClosureAllocation
            // ReSharper disable once HeapView.DelegateAllocation
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

        private void OnPaint(object sender, OnPaintEventArgs e)
        {
            // ReSharper disable once HeapView.ObjectAllocation
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

                           // ReSharper disable once HeapView.ObjectAllocation.Evident
                           new Texture(

                               // ReSharper disable once HeapView.ObjectAllocation.Evident
                               new ShaderResourceView1(_graphicsDevice.Device, tex), e.Width, e.Height))
                       ?.Dispose();
        }

        #region input handling

        void IInputHandler.RegisterInput(IInputDevice device)
        {
            _uiInputWrapper.RegisterInput(device);
        }

        void IInputHandler.UnregisterInput(IInputDevice device)
        {
            _uiInputWrapper.UnregisterInput(device);
        }

        #endregion
    }
}