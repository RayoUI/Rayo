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

    public RayoGLSurfaceView(
        Context context,
        AndroidApplicationContext appContext,
        WindowConfiguration config) : base(context)
    {
        SetEGLContextClientVersion(2);
        SetEGLConfigChooser(8, 8, 8, 8, 0, 0);
        _renderer = new RayoRenderer(context, this, appContext, config);
        SetRenderer(_renderer);
        
        // Configure for 60 fps continuous rendering on Android
        RenderMode = Rendermode.Continuously;

        Rayo.Core.Platform.VirtualKeyboardManager.SetService(
            new Rayo.Hosting.Android.AndroidVirtualKeyboardService(this, context));

        FocusableInTouchMode = true;
        Focusable = true;
        RequestFocus();
    }

    public override bool OnTouchEvent(global::Android.Views.MotionEvent? e)
    {
        if (e == null) return base.OnTouchEvent(e);
        _renderer.HandleTouchEvent(e);
        return true;
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

        var eventManager = Rayo.Core.OverlayManager.EventManager;
        if (eventManager == null)
        {
            return;
        }

        foreach (var ch in text)
        {
            eventManager.ProcessTextInput(ch);
        }
    }

    internal void DispatchKeyDown(Rayo.Core.InputKey key)
    {
        Rayo.Core.OverlayManager.EventManager?.ProcessKeyDown(key);
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

            return base.CommitText(text, newCursorPosition);
        }

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            if (beforeLength > 0)
            {
                _view.DispatchKeyDown(Rayo.Core.InputKey.Backspace);
            }

            return base.DeleteSurroundingText(beforeLength, afterLength);
        }

        public override bool SendKeyEvent(global::Android.Views.KeyEvent? e)
        {
            if (e != null && e.Action == global::Android.Views.KeyEventActions.Down)
            {
                if (e.KeyCode == global::Android.Views.Keycode.Del)
                {
                    _view.DispatchKeyDown(Rayo.Core.InputKey.Backspace);
                    return true;
                }

                if (e.KeyCode == global::Android.Views.Keycode.Enter)
                {
                    _view.DispatchKeyDown(Rayo.Core.InputKey.Return);
                    return true;
                }
            }

            return base.SendKeyEvent(e);
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

        private ByteBuffer? _pixelBuffer;
        private readonly Stopwatch _frameStopwatch = Stopwatch.StartNew();
        private double _lastFrameTimestamp;

        private readonly ConcurrentQueue<TouchEvent> _touchEventQueue = new();
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

            RayoLog.Info($"Surface changed: {width}x{height}");

            if (_tree == null)
            {
                InitializeRayo(width, height);
            }
            else
            {
                _skiaRenderer?.Resize(width, height);
            }

            float logicalWidth = width / SkiaSharpRenderer.GetDpiScaleFactor();
            float logicalHeight = height / SkiaSharpRenderer.GetDpiScaleFactor();
            Rayo.Core.OverlayManager.SetWindowSize(logicalWidth, logicalHeight);
        }

        private void InitializeRayo(int width, int height)
        {
            try
            {
                _skiaRenderer = new SkiaSharpRenderer();
                float scaleFactor = SkiaSharpRenderer.GetDpiScaleFactor();
                RayoLog.Info($"Using renderer scale factor: {scaleFactor:F2}x");
                
                _skiaRenderer.Initialize(width, height);

                _tree = new UITree();
                UITree.Current = _tree;

                // In continuous rendering mode, no need for render callbacks
                _tree.OnNeedsRenderChanged = null;

                _tree.InitializeEventManager(null);

                // Set UITree reference for components that need overlays (Drawer, Dialog, etc.)
                Rayo.Controls.Drawer.UITree(_tree);
                Rayo.Core.OverlayManager.SetTree(_tree);

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
                _skiaRenderer.BeginFrame();
                _skiaRenderer.Clear(new Rayo.Rendering.Color(30, 30, 30));

                // Use UITree.Render() to properly traverse the element tree
                _tree.Render(_skiaRenderer);

                // Render DevTool highlight overlay on top of everything (if DevTools enabled)
                Rayo.DevTools.DevToolExtensions.RenderDevToolOverlay(_skiaRenderer);

                _skiaRenderer.EndFrame();

                // STEP 7: Get pixels and upload to OpenGL texture
                var pixels = _skiaRenderer.GetPixels();
                if (pixels != null && pixels.Length > 0)
                {
                    // Copy pixels to direct buffer
                    _pixelBuffer.Position(0);
                    _pixelBuffer.Put(pixels);
                    _pixelBuffer.Position(0);

                    GLES20.GlBindTexture(GLES20.GlTexture2d, _textureId);
                    GLES20.GlTexImage2D(
                        GLES20.GlTexture2d, 0, GLES20.GlRgba,
                        _width, _height, 0,
                        GLES20.GlRgba, GLES20.GlUnsignedByte,
                        _pixelBuffer);
                }

                // STEP 8: Clear and draw fullscreen quad
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

                // Allow next invalidation to trigger another frame
                // In continuous rendering mode, this runs every frame targeting 60 fps
                _tree.ClearRenderFlag();
            }
            catch (Exception ex)
            {
                RayoLog.Error($"Error in OnDrawFrame: {ex.Message}", ex);
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

            // No need to request render explicitly - continuous rendering mode runs at 60 fps
        }

        private void ProcessTouchEvents()
        {
            if (_tree?.EventManager == null) return;

            int processedCount = 0;
            while (_touchEventQueue.TryDequeue(out var touchEvent) && processedCount < MaxEventsPerFrame)
            {
                processedCount++;

                var position = new System.Numerics.Vector2(touchEvent.X, touchEvent.Y);

                switch (touchEvent.Type)
                {
                    case TouchEventType.Down:
                        var downArgs = PointerEventArgs.FromTouch(touchEvent.PointerId, position, touchEvent.Pressure);
                        downArgs.IsInContact = true;
                        _tree.EventManager.ProcessTouchDown(downArgs);
                        RayoLog.Debug($"Touch DOWN: ID={touchEvent.PointerId} at ({touchEvent.X:F0}, {touchEvent.Y:F0})");
                        break;

                    case TouchEventType.Move:
                        var moveArgs = PointerEventArgs.FromTouch(touchEvent.PointerId, position, touchEvent.Pressure);
                        moveArgs.IsInContact = true;
                        _tree.EventManager.ProcessTouchMove(moveArgs);
                        // No logging for move events - too frequent
                        break;

                    case TouchEventType.Up:
                        var upArgs = PointerEventArgs.FromTouch(touchEvent.PointerId, position, 0f);
                        // IsInContact = false for release: finger is no longer in contact
                        // TapRecognizer depends on this to detect release events
                        upArgs.IsInContact = false;
                        _tree.EventManager.ProcessTouchUp(upArgs);
                        RayoLog.Debug($"Touch UP: ID={touchEvent.PointerId} at ({touchEvent.X:F0}, {touchEvent.Y:F0})");
                        break;

                    case TouchEventType.Cancel:
                        var cancelArgs = PointerEventArgs.FromTouch(touchEvent.PointerId, position, 0f);
                        cancelArgs.IsInContact = false;
                        _tree.EventManager.ProcessTouchUp(cancelArgs);
                        RayoLog.Debug($"Touch CANCEL: ID={touchEvent.PointerId}");
                        break;
                }
            }

            // If we hit the limit and there are more events, request another render
            if (!_touchEventQueue.IsEmpty)
            {
                _view.RequestRender();
            }
        }

        /// <summary>
        /// Processes scroll inertia for all ScrollView elements in the UI tree.
        /// This must be called BEFORE UITree.Update() to ensure scroll offsets are updated
        /// before the layout phase (retained mode compliance).
        /// </summary>
        private void ProcessScrollInertia()
        {
            if (_tree?.Root == null) return;

            // Process inertia for ScrollViews in the main tree
            ProcessElementInertia(_tree.Root);

            // Process inertia for ScrollViews in overlays (Drawer, Dialog, etc.)
            foreach (var overlay in _tree.Overlays)
            {
                ProcessElementInertia(overlay);
            }
        }

        /// <summary>
        /// Recursively processes inertia for ScrollView elements in the tree.
        /// </summary>
        private void ProcessElementInertia(VisualElement element)
        {
            // Process this element if it's a ScrollView (use full name to avoid Android.Widget.ScrollView conflict)
            if (element is Controls.ScrollView scrollView)
            {
                scrollView.ProcessInertia();
            }

            // Recursively process children
            //foreach (var child in element)
            //{
            //    ProcessElementInertia(child);
            //}
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
