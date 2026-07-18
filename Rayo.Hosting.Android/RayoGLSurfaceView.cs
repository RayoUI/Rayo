using Android.Content;
using Android.Opengl;
using Android.Runtime;
using Android.Util;
using Java.Nio;
using Javax.Microedition.Khronos.Egl;
using Javax.Microedition.Khronos.Opengles;
using Microsoft.Extensions.DependencyInjection;
using Rayo.Animation;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Platform;
using Rayo.DevTools;
using Rayo.Rendering;
using Rayo.Rendering.SkiaSharp;
using Rayo.Hosting.Abstractions;
using System.Collections.Concurrent;
using System;
using System.Diagnostics;

namespace Rayo.Hosting.Android;

/// <summary>
/// Custom GLSurfaceView that hosts Rayo rendering using SkiaSharp.
/// This is the core rendering surface for Android applications.
/// </summary>
public class RayoGLSurfaceView : GLSurfaceView
{
    private readonly RayoRenderer _renderer;
    private readonly AndroidVirtualKeyboardService _virtualKeyboardService;
    private int _firstFrameNotificationSent;

    internal event Action? FirstFramePresented;

    public RayoGLSurfaceView(
        Context context,
        AndroidApplicationContext appContext,
        WindowConfiguration config) : base(context)
    {
        SetEGLContextClientVersion(2);
        SetEGLConfigChooser(new MultisampleEglConfigChooser(config.Samples));
        _renderer = new RayoRenderer(context, this, appContext, config);
        SetRenderer(_renderer);
        
        // Configure for 60 fps continuous rendering on Android
        RenderMode = Rendermode.Continuously;

        _virtualKeyboardService = new AndroidVirtualKeyboardService(this, context);
        VirtualKeyboardManager.SetService(_virtualKeyboardService);

        FocusableInTouchMode = true;
        Focusable = true;
        RequestFocus();
    }

    private void AttachOverlayTree(UITree tree)
    {
        _virtualKeyboardService.AttachOverlayTree(tree);
    }

    private void NotifyFirstFramePresented()
    {
        if (Interlocked.Exchange(ref _firstFrameNotificationSent, 1) != 0)
        {
            return;
        }

        // GLSurfaceView swaps the EGL buffer after OnDrawFrame returns. Post the
        // notification to the Android UI thread so the host can keep its startup
        // cover visible until that first buffer is ready for composition.
        Post(() => FirstFramePresented?.Invoke());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            VirtualKeyboardManager.ClearService(_virtualKeyboardService);
        }

        base.Dispose(disposing);
    }

    private sealed class MultisampleEglConfigChooser : Java.Lang.Object, GLSurfaceView.IEGLConfigChooser
    {
        private const int EglOpenGlEs2Bit = 4;
        private const int EglRenderableType = 0x3040;
        private const int EglSampleBuffers = 0x3032;
        private const int EglSamples = 0x3031;

        private readonly int _requestedSamples;

        public MultisampleEglConfigChooser(int requestedSamples)
        {
            _requestedSamples = Math.Max(0, requestedSamples);
        }

        public global::Javax.Microedition.Khronos.Egl.EGLConfig? ChooseConfig(
            IEGL10? egl,
            global::Javax.Microedition.Khronos.Egl.EGLDisplay? display)
        {
            if (egl == null || display == null)
            {
                return null;
            }

            int[] attributes =
            [
                IEGL10.EglRedSize, 8,
                IEGL10.EglGreenSize, 8,
                IEGL10.EglBlueSize, 8,
                IEGL10.EglAlphaSize, 8,
                IEGL10.EglDepthSize, 0,
                IEGL10.EglStencilSize, 0,
                EglRenderableType, EglOpenGlEs2Bit,
                IEGL10.EglNone
            ];

            var configCount = new int[1];
            if (!egl.EglChooseConfig(display, attributes, null, 0, configCount) || configCount[0] <= 0)
            {
                return null;
            }

            var configs = new global::Javax.Microedition.Khronos.Egl.EGLConfig[configCount[0]];
            if (!egl.EglChooseConfig(display, attributes, configs, configs.Length, configCount))
            {
                return null;
            }

            var selected = configs
                .Where(config => config != null)
                .Select(config => new
                {
                    Config = config,
                    Samples = GetConfigAttribute(egl, display, config, EglSamples),
                    SampleBuffers = GetConfigAttribute(egl, display, config, EglSampleBuffers)
                })
                .OrderByDescending(config => config.SampleBuffers > 0)
                .ThenBy(config => config.Samples >= _requestedSamples ? config.Samples - _requestedSamples : int.MaxValue)
                .ThenByDescending(config => config.Samples)
                .FirstOrDefault();

            if (selected != null)
            {
                RayoLog.Info($"Android EGL config selected with {selected.Samples}x MSAA");
            }

            return selected?.Config;
        }

        private static int GetConfigAttribute(
            IEGL10 egl,
            global::Javax.Microedition.Khronos.Egl.EGLDisplay display,
            global::Javax.Microedition.Khronos.Egl.EGLConfig config,
            int attribute)
        {
            var value = new int[1];
            return egl.EglGetConfigAttrib(display, config, attribute, value) ? value[0] : 0;
        }
    }

    public override bool OnTouchEvent(global::Android.Views.MotionEvent? e)
    {
        if (e == null) return base.OnTouchEvent(e);
        _renderer.HandleTouchEvent(e);
        return true;
    }

    public override bool OnKeyDown(global::Android.Views.Keycode keyCode, global::Android.Views.KeyEvent? e)
    {
        if (keyCode == global::Android.Views.Keycode.Del)
        {
            DispatchKeyDown(Rayo.Core.InputKey.Backspace);
            return true;
        }

        if (keyCode == global::Android.Views.Keycode.ForwardDel)
        {
            DispatchKeyDown(Rayo.Core.InputKey.Delete);
            return true;
        }

        if (keyCode == global::Android.Views.Keycode.Enter)
        {
            DispatchKeyDown(Rayo.Core.InputKey.Return);
            return true;
        }

        if (TryGetPrintableText(e, out var text))
        {
            DispatchTextInput(text);
            return true;
        }

        return base.OnKeyDown(keyCode, e);
    }

    public override bool OnKeyUp(global::Android.Views.Keycode keyCode, global::Android.Views.KeyEvent? e)
    {
        if (keyCode is global::Android.Views.Keycode.Del
            or global::Android.Views.Keycode.ForwardDel
            or global::Android.Views.Keycode.Enter)
        {
            return true;
        }

        return base.OnKeyUp(keyCode, e);
    }

    public override bool OnCheckIsTextEditor()
    {
        return true;
    }

    public override global::Android.Views.InputMethods.IInputConnection? OnCreateInputConnection(global::Android.Views.InputMethods.EditorInfo? outAttrs)
    {
        if (outAttrs == null)
        {
            return null;
        }

        var options = Rayo.Core.OverlayManager.EventManager?.FocusedElement as Rayo.Core.Platform.IVirtualKeyboardOptions;
        bool isMultiline = options?.IsMultiline ?? false;
        var keyboardType = options?.KeyboardType ?? Rayo.Core.Platform.VirtualKeyboardType.Default;

        outAttrs.InputType = GetInputType(keyboardType, isMultiline);
        outAttrs.ImeOptions = isMultiline
            ? global::Android.Views.InputMethods.ImeFlags.NoEnterAction
            : global::Android.Views.InputMethods.ImeFlags.NoFullscreen;

        return new RayoInputConnection(this, true);
    }

    internal void DispatchTextInput(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(() =>
        {
            var eventManager = Rayo.Core.OverlayManager.EventManager;
            if (eventManager == null)
            {
                return;
            }

            foreach (var ch in text)
            {
                eventManager.ProcessTextInput(ch);
            }
        });
    }

    internal void DispatchKeyDown(Rayo.Core.InputKey key)
    {
        Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(
            () => Rayo.Core.OverlayManager.EventManager?.ProcessKeyDown(key));
    }

    internal void ScheduleResumeRender()
    {
        Post(RequestForegroundFrame);
        PostDelayed(RequestForegroundFrame, 16);
        PostDelayed(RequestForegroundFrame, 50);
        PostDelayed(RequestForegroundFrame, 150);
        PostDelayed(RequestForegroundFrame, 300);
        PostDelayed(RequestForegroundFrame, 600);
        PostDelayed(RequestForegroundFrame, 1000);
    }

    internal void NotifyPaused()
    {
        _renderer.NotifyPaused();
    }

    internal void NotifyActivityPaused()
    {
        VirtualKeyboardManager.NotifyAppPaused();
        NotifyPaused();
    }

    internal void NotifyWindowFocusLost()
    {
        VirtualKeyboardManager.NotifyAppPaused();
        NotifyPaused();
    }

    internal void RestoreVirtualKeyboard()
    {
        var options = OverlayManager.EventManager?.FocusedElement as IVirtualKeyboardOptions;
        VirtualKeyboardManager.RestoreAfterResume(options);
    }

    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        ScheduleResumeRender();
    }

    protected override void OnDetachedFromWindow()
    {
        NotifyPaused();
        base.OnDetachedFromWindow();
    }

    protected override void OnWindowVisibilityChanged(global::Android.Views.ViewStates visibility)
    {
        base.OnWindowVisibilityChanged(visibility);

        if (visibility == global::Android.Views.ViewStates.Visible)
        {
            ScheduleResumeRender();
        }
        else
        {
            NotifyPaused();
        }
    }

    private void RequestForegroundFrame()
    {
        RequestFocus();
        RequestLayout();
        Invalidate();
        RenderMode = Rendermode.Continuously;
        QueueEvent(() =>
        {
            _renderer.NotifyForegrounded();
            RequestRender();
        });
        RequestRender();
    }

    private static global::Android.Text.InputTypes GetInputType(Rayo.Core.Platform.VirtualKeyboardType type, bool isMultiline)
    {
        global::Android.Text.InputTypes inputType = type switch
        {
            Rayo.Core.Platform.VirtualKeyboardType.Numeric => global::Android.Text.InputTypes.ClassNumber,
            Rayo.Core.Platform.VirtualKeyboardType.Email => global::Android.Text.InputTypes.ClassText | global::Android.Text.InputTypes.TextVariationEmailAddress,
            Rayo.Core.Platform.VirtualKeyboardType.Url => global::Android.Text.InputTypes.ClassText | global::Android.Text.InputTypes.TextVariationUri,
            Rayo.Core.Platform.VirtualKeyboardType.Phone => global::Android.Text.InputTypes.ClassPhone,
            Rayo.Core.Platform.VirtualKeyboardType.Password => global::Android.Text.InputTypes.ClassText | global::Android.Text.InputTypes.TextVariationPassword,
            _ => global::Android.Text.InputTypes.ClassText
        };

        if (isMultiline)
        {
            inputType |= global::Android.Text.InputTypes.TextFlagMultiLine;
        }

        return inputType;
    }

    private static bool TryGetPrintableText(global::Android.Views.KeyEvent? keyEvent, out string text)
    {
        var unicode = keyEvent?.UnicodeChar ?? 0;
        if (unicode < ' ')
        {
            text = string.Empty;
            return false;
        }

        text = char.ConvertFromUtf32(unicode);
        return true;
    }

    private sealed class RayoInputConnection : global::Android.Views.InputMethods.BaseInputConnection
    {
        private readonly RayoGLSurfaceView _view;

        public RayoInputConnection(RayoGLSurfaceView view, bool fullEditor)
            : base(view, fullEditor)
        {
            _view = view;
        }

        public override bool CommitText(global::Java.Lang.ICharSequence? text, int newCursorPosition)
        {
            if (text != null)
            {
                _view.DispatchTextInput(text.ToString() ?? string.Empty);
            }

            return true;
        }

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            DispatchDelete(beforeLength, afterLength);
            return base.DeleteSurroundingText(beforeLength, afterLength);
        }

        public override bool DeleteSurroundingTextInCodePoints(int beforeLength, int afterLength)
        {
            DispatchDelete(beforeLength, afterLength);
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
            {
                return base.DeleteSurroundingTextInCodePoints(beforeLength, afterLength);
            }

            return base.DeleteSurroundingText(beforeLength, afterLength);
        }

        public override bool SendKeyEvent(global::Android.Views.KeyEvent? e)
        {
            if (e?.KeyCode == global::Android.Views.Keycode.Del)
            {
                if (e.Action == global::Android.Views.KeyEventActions.Down)
                {
                    _view.DispatchKeyDown(Rayo.Core.InputKey.Backspace);
                }

                return true;
            }

            if (e?.KeyCode == global::Android.Views.Keycode.Enter)
            {
                if (e.Action == global::Android.Views.KeyEventActions.Down)
                {
                    _view.DispatchKeyDown(Rayo.Core.InputKey.Return);
                }

                return true;
            }

            if (e?.Action == global::Android.Views.KeyEventActions.Down &&
                TryGetPrintableText(e, out var text))
            {
                _view.DispatchTextInput(text);
                return true;
            }

            if (e?.Action == global::Android.Views.KeyEventActions.Up &&
                TryGetPrintableText(e, out _))
            {
                return true;
            }

            return base.SendKeyEvent(e);
        }

        private void DispatchDelete(int beforeLength, int afterLength)
        {
            for (var i = 0; i < beforeLength; i++)
            {
                _view.DispatchKeyDown(Rayo.Core.InputKey.Backspace);
            }

            for (var i = 0; i < afterLength; i++)
            {
                _view.DispatchKeyDown(Rayo.Core.InputKey.Delete);
            }
        }
    }

    private class RayoRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        private readonly Context _context;
        private readonly RayoGLSurfaceView _view;
        private readonly AndroidApplicationContext _appContext;
        private readonly WindowConfiguration _config;
        private UITree? _tree;
        private SkiaSharpRenderer? _skiaRenderer;
        private int _width;
        private int _height;
        private bool _isInitialized = false;
        private bool _hotReloadSubscribed;

        // OpenGL resources
        private int _textureId;
        private int _programId;
        private int _vertexBufferId;
        private bool _glInitialized;
        private bool _surfaceRecreated;

        private ByteBuffer? _pixelBuffer;
        private readonly Stopwatch _frameStopwatch = Stopwatch.StartNew();
        private double _lastFrameTimestamp;
        private bool _resumeRenderPending;
        private bool _foregroundRenderPending;
        private volatile bool _resumeRequiresRebind;
        private bool _hasPresentedFrame;

        private readonly ConcurrentQueue<TouchEvent> _touchEventQueue = new();
        private readonly Dictionary<int, TouchEvent> _pendingTouchMoves = new();
        private const int MaxEventsPerFrame = 128;

        private readonly record struct TouchEvent(
            TouchEventType Type,
            int PointerId,
            float X,
            float Y,
            float Pressure
        );

        private enum TouchEventType : byte
        {
            Down,
            Move,
            Up,
            Cancel
        }

        private const string VertexShader = @"
            attribute vec4 aPosition;
            attribute vec2 aTexCoord;
            varying vec2 vTexCoord;
            void main() {
                gl_Position = aPosition;
                vTexCoord = aTexCoord;
            }";

        private const string FragmentShader = @"
            precision mediump float;
            varying vec2 vTexCoord;
            uniform sampler2D uTexture;
            void main() {
                gl_FragColor = texture2D(uTexture, vTexCoord);
            }";

        private static readonly float[] QuadVertices = {
            -1f,  1f,   0f,  0f,
            -1f, -1f,   0f,  1f,
             1f,  1f,   1f,  0f,
             1f, -1f,   1f,  1f,
        };

        public RayoRenderer(
            Context context,
            RayoGLSurfaceView view,
            AndroidApplicationContext appContext,
            WindowConfiguration config)
        {
            _context = context;
            _view = view;
            _appContext = appContext;
            _config = config;
        }

        public void OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
        {
            GLES20.GlClearColor(0.12f, 0.12f, 0.12f, 1.0f);
            GLES20.GlEnable(GLES20.GlBlend);
            GLES20.GlBlendFunc(GLES20.GlOne, GLES20.GlOneMinusSrcAlpha);

            _programId = CreateProgram(VertexShader, FragmentShader);
            if (_programId == 0)
            {
                RayoLog.Error("Failed to create shader program");
                return;
            }

            var buffers = new int[1];
            GLES20.GlGenBuffers(1, buffers, 0);
            _vertexBufferId = buffers[0];

            GLES20.GlBindBuffer(GLES20.GlArrayBuffer, _vertexBufferId);
            var vertexBuffer = ByteBuffer.AllocateDirect(QuadVertices.Length * 4)
                .Order(ByteOrder.NativeOrder()!)
                .AsFloatBuffer();
            vertexBuffer!.Put(QuadVertices);
            vertexBuffer.Position(0);
            GLES20.GlBufferData(GLES20.GlArrayBuffer, QuadVertices.Length * 4, vertexBuffer, GLES20.GlStaticDraw);

            var textures = new int[1];
            GLES20.GlGenTextures(1, textures, 0);
            _textureId = textures[0];

            GLES20.GlBindTexture(GLES20.GlTexture2d, _textureId);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMinFilter, GLES20.GlLinear);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMagFilter, GLES20.GlLinear);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);

            _glInitialized = true;
            _surfaceRecreated = true;
            RayoLog.Info("OpenGL surface created successfully");
        }

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            _width = width;
            _height = height;
            GLES20.GlViewport(0, 0, width, height);

            int bufferSize = width * height * 4;
            _pixelBuffer = ByteBuffer.AllocateDirect(bufferSize);
            _pixelBuffer.Order(ByteOrder.NativeOrder()!);

            // Allocate the texture storage once for this surface size. Reallocating it
            // with GlTexImage2D on every frame can expose an empty texture while the
            // compositor is presenting rapid text/layout updates.
            GLES20.GlBindTexture(GLES20.GlTexture2d, _textureId);
            GLES20.GlTexImage2D(
                GLES20.GlTexture2d, 0, GLES20.GlRgba,
                width, height, 0,
                GLES20.GlRgba, GLES20.GlUnsignedByte,
                null);

            RayoLog.Info($"Surface changed: {width}x{height}");

            if (_tree == null)
            {
                InitializeRayo(width, height);
            }
            else
            {
                RecreateOrResizeSkiaSurface(width, height);
                _tree.MarkNeedsLayout();
                _tree.MarkNeedsRender();
            }

            _surfaceRecreated = false;

            float logicalWidth = width / SkiaSharpRenderer.GetDpiScaleFactor();
            float logicalHeight = height / SkiaSharpRenderer.GetDpiScaleFactor();
            Rayo.Core.OverlayManager.SetWindowSize(logicalWidth, logicalHeight);
        }

        private void RecreateOrResizeSkiaSurface(int width, int height, bool forceGpuRebind = false)
        {
            if (_skiaRenderer == null)
            {
                return;
            }

            if (_surfaceRecreated || forceGpuRebind)
            {
                if (TryInitializeGpuSkia(width, height))
                {
                    RayoLog.Info("SkiaSharp GPU surface rebound to current Android framebuffer");
                }
                else
                {
                    _skiaRenderer.Initialize(width, height);
                    RayoLog.Info("SkiaSharp GPU surface unavailable; using CPU fallback");
                }

                return;
            }

            _skiaRenderer.Resize(width, height);
        }

        private void InitializeRayo(int width, int height)
        {
            try
            {
                _skiaRenderer = new SkiaSharpRenderer();
                float scaleFactor = SkiaSharpRenderer.GetDpiScaleFactor();
                RayoLog.Info($"Using renderer scale factor: {scaleFactor:F2}x");

                if (TryInitializeGpuSkia(width, height))
                {
                    RayoLog.Info("SkiaSharp initialized with direct OpenGL GPU rendering");
                }
                else
                {
                    _skiaRenderer.Initialize(width, height);
                    RayoLog.Info("SkiaSharp GPU initialization unavailable; using CPU fallback");
                }

                _tree = new UITree();
                UITree.Current = _tree;

                // In continuous rendering mode, no need for render callbacks
                _tree.OnNeedsRenderChanged = null;

                _tree.InitializeEventManager(null);

                // Set UITree reference for components that need overlays (Drawer, Dialog, etc.)
                Rayo.Controls.Drawer.UITree(_tree);
                Rayo.Core.OverlayManager.SetTree(_tree);
                _view.AttachOverlayTree(_tree);

                // Set the service provider for DependencyInjector
                var serviceProvider = _appContext.Services;
                if (serviceProvider != null)
                {
                    DependencyInjector.SetServiceProvider(serviceProvider);
                }

                // Create the UI from the configured view type
                if (_appContext.ViewType != null)
                {
                    try
                    {
                        // Try to resolve from DI first, then create instance
                        object? view = serviceProvider?.GetService(_appContext.ViewType);
                        
                        if (view == null && serviceProvider != null)
                        {
                            // Create instance using ActivatorUtilities (supports constructor injection)
                            view = ActivatorUtilities.CreateInstance(serviceProvider, _appContext.ViewType);
                        }

                        if (view == null)
                        {
                            // Fallback to parameterless constructor (matches pre-hosting behavior)
                            view = Activator.CreateInstance(_appContext.ViewType);
                        }
                        
                        if (view is VisualElement element)
                        {
                            // Inject any [Inject] properties
                            DependencyInjector.Inject(element, serviceProvider);
                            
                            _tree.SetRoot(element);
                            RayoLog.Info($"Successfully created view: {_appContext.ViewType.Name}");
                        }
                        else
                        {
                            RayoLog.Error($"View type {_appContext.ViewType.Name} is not a UIElementBase (got {view?.GetType().Name ?? "null"})");
                        }
                    }
                    catch (Exception viewEx)
                    {
                        RayoLog.Error($"Failed to create view {_appContext.ViewType.Name}: {viewEx.Message}", viewEx);
                    }
                }
                else
                {
                    RayoLog.Error("No view type configured! Call context.SetUI<YourView>() in ConfigureApp");
                }

                float logicalWidth = width / SkiaSharpRenderer.GetDpiScaleFactor();
                float logicalHeight = height / SkiaSharpRenderer.GetDpiScaleFactor();
                Rayo.Core.OverlayManager.SetWindowSize(logicalWidth, logicalHeight);
                _tree.Update(logicalWidth, logicalHeight);

                if (_appContext.EnableDevTools && _skiaRenderer != null)
                {
                    DevToolExtensions.EnableDevTools(_tree, _skiaRenderer, _appContext.DevToolsPort);
                }

                _isInitialized = true;
                RegisterHotReload();

                // No need to request render - continuous rendering mode runs automatically at 60 fps

                RayoLog.Info("Rayo initialized successfully with initial layout complete");
            }
            catch (Exception ex)
            {
                RayoLog.Error($"Failed to initialize Rayo: {ex.Message}", ex);
            }
        }

        private bool TryInitializeGpuSkia(int width, int height)
        {
            if (_skiaRenderer == null)
            {
                return false;
            }

            BindDefaultFramebuffer(width, height);

            var framebuffer = new int[1];
            var samples = new int[1];
            var stencilBits = new int[1];
            GLES20.GlGetIntegerv(GLES20.GlFramebufferBinding, framebuffer, 0);
            GLES20.GlGetIntegerv(GLES20.GlSamples, samples, 0);
            GLES20.GlGetIntegerv(GLES20.GlStencilBits, stencilBits, 0);

            return _skiaRenderer.TryInitializeGpu(
                width,
                height,
                unchecked((uint)framebuffer[0]),
                samples[0],
                stencilBits[0]);
        }

        private void RegisterHotReload()
        {
            if (_hotReloadSubscribed)
            {
                return;
            }

            HotReloadMediator.ReloadRequested += OnHotReloadRequested;
            _hotReloadSubscribed = true;
            RayoLog.Info("Hot reload bridge registered for Android renderer");
        }

        private void UnregisterHotReload()
        {
            if (!_hotReloadSubscribed)
            {
                return;
            }

            HotReloadMediator.ReloadRequested -= OnHotReloadRequested;
            _hotReloadSubscribed = false;
            RayoLog.Info("Hot reload bridge unregistered");
        }

        private void OnHotReloadRequested(System.Type[]? updatedTypes)
        {
            if (!_isInitialized)
            {
                return;
            }

            _view.QueueEvent(() =>
            {
                try
                {
                    ReloadRoot();
                }
                catch (Exception ex)
                {
                    RayoLog.Error($"Hot reload failed: {ex.Message}", ex);
                }
            });
        }

        private void ReloadRoot()
        {
            if (_tree == null)
            {
                return;
            }

            // Recreate the UI from the configured view type
            if (_appContext.ViewType != null)
            {
                try
                {
                    var serviceProvider = _appContext.Services;
                    object? view = serviceProvider?.GetService(_appContext.ViewType);
                    
                    if (view == null && serviceProvider != null)
                    {
                        view = ActivatorUtilities.CreateInstance(serviceProvider, _appContext.ViewType);
                    }

                    if (view == null)
                    {
                        view = Activator.CreateInstance(_appContext.ViewType);
                    }
                    
                    if (view is VisualElement element)
                    {
                        DependencyInjector.Inject(element, serviceProvider);
                        _tree.SetRoot(element);
                        
                        float logicalWidth = _width / SkiaSharpRenderer.GetDpiScaleFactor();
                        float logicalHeight = _height / SkiaSharpRenderer.GetDpiScaleFactor();

                        _tree.MarkNeedsLayout();
                        _tree.Update(logicalWidth, logicalHeight);
                        _tree.MarkNeedsRender();

                        // No need to request render - continuous rendering mode handles it

                        RayoLog.Info("UI tree reloaded after hot reload update");
                    }
                }
                catch (Exception ex)
                {
                    RayoLog.Error($"Failed to reload view during hot reload: {ex.Message}", ex);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnregisterHotReload();
            }

            base.Dispose(disposing);
        }

        public void OnDrawFrame(IGL10? gl)
        {
            if (!_glInitialized || _tree == null || _skiaRenderer == null || _pixelBuffer == null)
                return;

            float deltaTime = GetDeltaTime();

            try
            {
                // STEP 1: Process queued touch events from Main Thread (lock-free)
                ProcessTouchEvents();

                if (_surfaceRecreated || _resumeRenderPending || _foregroundRenderPending)
                {
                    bool shouldRebindSurface = _surfaceRecreated || _resumeRenderPending;
                    bool forceGpuRebind = _resumeRenderPending;
                    _resumeRenderPending = false;
                    _foregroundRenderPending = false;
                    _lastFrameTimestamp = 0;

                    if (shouldRebindSurface)
                    {
                        RecreateOrResizeSkiaSurface(_width, _height, forceGpuRebind);
                        _surfaceRecreated = false;
                    }

                    _tree.ResetRenderCache();
                    _tree.MarkNeedsLayout();
                    _tree.MarkNeedsRender();
                }

                // STEP 2: Tick animations
                AnimationManager.Instance.Update(deltaTime * 1000.0f);
                FrameAnimationTicker.Tick(deltaTime);
                _tree.NotifyRenderStarted();

                // STEP 3: Process scroll inertia BEFORE layout (retained mode compliance)
                // This ensures scroll offsets are updated before Measure/Arrange
                ProcessScrollInertia();

                // STEP 4: Process pending reactive updates BEFORE layout/render
                Rayo.Reactivity.UIUpdateQueue.ProcessPendingUpdates();

                // STEP 5: Update layout - pass logical dimensions (physical / scale)
                float logicalWidth = _width / SkiaSharpRenderer.GetDpiScaleFactor();
                float logicalHeight = _height / SkiaSharpRenderer.GetDpiScaleFactor();
                _tree.Update(logicalWidth, logicalHeight);

                // STEP 6: Render UI to SkiaSharp surface
                if (_skiaRenderer.IsGpuBacked)
                {
                    BindDefaultFramebuffer(_width, _height);
                }

                _skiaRenderer.BeginFrame();
                _skiaRenderer.Clear(new Rayo.Rendering.Color(30, 30, 30));

                // Use UITree.Render() to properly traverse the element tree
                _tree.Render(_skiaRenderer);

                // Render DevTool highlight overlay on top of everything (if DevTools enabled)
                Rayo.DevTools.DevToolExtensions.RenderDevToolOverlay(_skiaRenderer);

                _skiaRenderer.EndFrame();

                if (_skiaRenderer.IsGpuBacked)
                {
                    // Skia rendered directly into the GLSurfaceView framebuffer.
                    GLES20.GlFlush();
                }
                else
                {
                    // CPU fallback: copy into the reusable native buffer and upload.
                    if (_skiaRenderer.CopyPixelsTo(
                        _pixelBuffer.GetDirectBufferAddress(),
                        _pixelBuffer.Capacity()))
                    {
                        _pixelBuffer.Position(0);

                        GLES20.GlBindTexture(GLES20.GlTexture2d, _textureId);
                        GLES20.GlTexSubImage2D(
                            GLES20.GlTexture2d, 0,
                            0, 0, _width, _height,
                            GLES20.GlRgba, GLES20.GlUnsignedByte,
                            _pixelBuffer);
                    }

                    // Present the CPU surface through a fullscreen textured quad.
                    GLES20.GlClear(GLES20.GlColorBufferBit);
                    GLES20.GlUseProgram(_programId);

                    GLES20.GlBindBuffer(GLES20.GlArrayBuffer, _vertexBufferId);

                    int posLoc = GLES20.GlGetAttribLocation(_programId, "aPosition");
                    GLES20.GlEnableVertexAttribArray(posLoc);
                    GLES20.GlVertexAttribPointer(posLoc, 2, GLES20.GlFloat, false, 16, 0);

                    int texLoc = GLES20.GlGetAttribLocation(_programId, "aTexCoord");
                    GLES20.GlEnableVertexAttribArray(texLoc);
                    GLES20.GlVertexAttribPointer(texLoc, 2, GLES20.GlFloat, false, 16, 8);

                    GLES20.GlActiveTexture(GLES20.GlTexture0);
                    GLES20.GlBindTexture(GLES20.GlTexture2d, _textureId);
                    GLES20.GlUniform1i(GLES20.GlGetUniformLocation(_programId, "uTexture"), 0);

                    GLES20.GlDrawArrays(GLES20.GlTriangleStrip, 0, 4);

                    GLES20.GlDisableVertexAttribArray(posLoc);
                    GLES20.GlDisableVertexAttribArray(texLoc);
                }

                // Allow next invalidation to trigger another frame
                // In continuous rendering mode, this runs every frame targeting 60 fps
                _tree.ClearRenderFlag();
                _hasPresentedFrame = true;
                _view.NotifyFirstFramePresented();
            }
            catch (Exception ex)
            {
                RayoLog.Error($"Error in OnDrawFrame: {ex.Message}", ex);
            }
        }

        public void NotifyResumed()
        {
            NotifyForegrounded();
        }

        public void NotifyForegrounded()
        {
            if (!_isInitialized || !_hasPresentedFrame)
            {
                return;
            }

            _foregroundRenderPending = true;

            if (_resumeRequiresRebind)
            {
                _resumeRequiresRebind = false;
                _resumeRenderPending = true;
            }
        }

        public void NotifyPaused()
        {
            if (_isInitialized && _hasPresentedFrame)
            {
                _resumeRequiresRebind = true;
            }
        }

        private float GetDeltaTime()
        {
            double now = _frameStopwatch.Elapsed.TotalSeconds;

            if (_lastFrameTimestamp <= 0)
            {
                _lastFrameTimestamp = now;
                return 1f / 60f;
            }

            double delta = now - _lastFrameTimestamp;
            _lastFrameTimestamp = now;

            if (delta <= 0 || delta > 1)
            {
                return 1f / 60f;
            }

            return (float)delta;
        }

        private static void BindDefaultFramebuffer(int width, int height)
        {
            GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, 0);
            GLES20.GlViewport(0, 0, width, height);
        }

        public void HandleTouchEvent(global::Android.Views.MotionEvent e)
        {
            if (!_isInitialized) return;

            float scaleFactor = SkiaSharpRenderer.GetDpiScaleFactor();

            var action = e.ActionMasked;
            int pointerIndex = e.ActionIndex;
            int pointerId = e.GetPointerId(pointerIndex);
            float x = e.GetX(pointerIndex) / scaleFactor;
            float y = e.GetY(pointerIndex) / scaleFactor;
            float pressure = e.GetPressure(pointerIndex);

            TouchEventType eventType = action switch
            {
                global::Android.Views.MotionEventActions.Down or
                global::Android.Views.MotionEventActions.PointerDown => TouchEventType.Down,
                global::Android.Views.MotionEventActions.Move => TouchEventType.Move,
                global::Android.Views.MotionEventActions.Up or
                global::Android.Views.MotionEventActions.PointerUp => TouchEventType.Up,
                global::Android.Views.MotionEventActions.Cancel => TouchEventType.Cancel,
                _ => TouchEventType.Cancel
            };

            if (action == global::Android.Views.MotionEventActions.Move)
            {
                int pointerCount = e.PointerCount;
                for (int i = 0; i < pointerCount && _touchEventQueue.Count < MaxEventsPerFrame; i++)
                {
                    int id = e.GetPointerId(i);
                    float px = e.GetX(i) / scaleFactor;
                    float py = e.GetY(i) / scaleFactor;
                    float pr = e.GetPressure(i);
                    _touchEventQueue.Enqueue(new TouchEvent(TouchEventType.Move, id, px, py, pr));
                }
            }
            else
            {
                _touchEventQueue.Enqueue(new TouchEvent(eventType, pointerId, x, y, pressure));
            }

            // A terminal touch event must reach the render thread even if Android
            // temporarily throttles the GLSurfaceView while the IME is changing.
            _view.RequestRender();
        }

        private void ProcessTouchEvents()
        {
            if (_tree?.EventManager == null) return;

            int processedCount = 0;

            // Check the budget before dequeuing. The previous order removed one
            // unprocessed event whenever the queue exceeded the frame budget;
            // if that event was Up/Cancel, the pressed control remained captured
            // forever and its tap action was never invoked.
            while (processedCount < MaxEventsPerFrame &&
                   _touchEventQueue.TryDequeue(out var touchEvent))
            {
                processedCount++;

                if (touchEvent.Type == TouchEventType.Move)
                {
                    _pendingTouchMoves[touchEvent.PointerId] = touchEvent;
                    continue;
                }

                FlushPendingMoves();
                ProcessTouchEvent(touchEvent);
            }

            FlushPendingMoves();

            // If we hit the limit and there are more events, request another render
            if (!_touchEventQueue.IsEmpty)
            {
                _view.RequestRender();
            }

            void FlushPendingMoves()
            {
                foreach (var move in _pendingTouchMoves.Values)
                {
                    ProcessTouchEvent(move);
                }

                _pendingTouchMoves.Clear();
            }

            void ProcessTouchEvent(TouchEvent current)
            {
                var position = new System.Numerics.Vector2(current.X, current.Y);

                switch (current.Type)
                {
                    case TouchEventType.Down:
                        var downArgs = PointerEventArgs.FromTouch(current.PointerId, position, current.Pressure);
                        downArgs.IsInContact = true;
                        _tree.EventManager.ProcessTouchDown(downArgs);
                        RayoLog.Debug($"Touch DOWN: ID={current.PointerId} at ({current.X:F0}, {current.Y:F0})");
                        break;

                    case TouchEventType.Move:
                        var moveArgs = PointerEventArgs.FromTouch(current.PointerId, position, current.Pressure);
                        moveArgs.IsInContact = true;
                        _tree.EventManager.ProcessTouchMove(moveArgs);
                        break;

                    case TouchEventType.Up:
                        var upArgs = PointerEventArgs.FromTouch(current.PointerId, position, 0f);
                        upArgs.IsInContact = false;
                        _tree.EventManager.ProcessTouchUp(upArgs);
                        RayoLog.Debug($"Touch UP: ID={current.PointerId} at ({current.X:F0}, {current.Y:F0})");
                        break;

                    case TouchEventType.Cancel:
                        var cancelArgs = PointerEventArgs.FromTouch(current.PointerId, position, 0f);
                        cancelArgs.IsInContact = false;
                        _tree.EventManager.ProcessTouchCancel(cancelArgs);
                        RayoLog.Debug($"Touch CANCEL: ID={current.PointerId}");
                        break;
                }
            }
        }

        /// <summary>
        /// Processes scroll inertia for all ScrollView elements in the UI tree.
        /// This must be called BEFORE UITree.Update() to ensure scroll offsets are updated
        /// before the layout phase (retained mode compliance).
        /// </summary>
        private void ProcessScrollInertia()
        {
            Controls.ScrollView.ProcessActiveInertia();
        }

        private int CreateProgram(string vertexSource, string fragmentSource)
        {
            int vertexShader = LoadShader(GLES20.GlVertexShader, vertexSource);
            if (vertexShader == 0) return 0;

            int fragmentShader = LoadShader(GLES20.GlFragmentShader, fragmentSource);
            if (fragmentShader == 0) return 0;

            int program = GLES20.GlCreateProgram();
            GLES20.GlAttachShader(program, vertexShader);
            GLES20.GlAttachShader(program, fragmentShader);
            GLES20.GlLinkProgram(program);

            var linkStatus = new int[1];
            GLES20.GlGetProgramiv(program, GLES20.GlLinkStatus, linkStatus, 0);
            if (linkStatus[0] == 0)
            {
                RayoLog.Error($"Program link error: {GLES20.GlGetProgramInfoLog(program)}");
                GLES20.GlDeleteProgram(program);
                return 0;
            }

            return program;
        }

        private int LoadShader(int type, string source)
        {
            int shader = GLES20.GlCreateShader(type);
            GLES20.GlShaderSource(shader, source);
            GLES20.GlCompileShader(shader);

            var compiled = new int[1];
            GLES20.GlGetShaderiv(shader, GLES20.GlCompileStatus, compiled, 0);
            if (compiled[0] == 0)
            {
                RayoLog.Error($"Shader compile error: {GLES20.GlGetShaderInfoLog(shader)}");
                GLES20.GlDeleteShader(shader);
                return 0;
            }

            return shader;
        }
    }
}
