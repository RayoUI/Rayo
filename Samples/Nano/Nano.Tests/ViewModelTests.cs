using System.Numerics;
using Nano.ViewModels;
using Nano.Navigation;
using Nano.Views.ProjectAssetStore;
using Nano.Views.ProjectAssetStore.Components;
using Nano.Views.Game;
using Nano.Views.CodeEditor.Components;
using Nano.GameEngine;
using Nano.Views.SpriteEditor;
using Rayo.Rendering;
using Rayo.Animation;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Xunit;

namespace Nano.Tests;

public sealed class ViewModelTests
{
    [Theory]
    [InlineData('(', ')')]
    [InlineData('[', ']')]
    [InlineData('{', '}')]
    public void Code_editor_inserts_pairs_and_skips_an_existing_closer(char opening, char closing)
    {
        var editor = new CodeEdit(string.Empty, new LuaCodeLanguage());

        editor.HandleInput(TextInput(opening));
        editor.HandleInput(TextInput('x'));
        editor.HandleInput(TextInput(closing));
        editor.HandleInput(TextInput('y'));

        Assert.Equal($"{opening}x{closing}y", editor.Text);
    }

    [Fact]
    public void Code_editor_new_line_inherits_indentation_and_indents_lua_blocks()
    {
        var inherited = new CodeEdit("    local value = 1", new LuaCodeLanguage());
        inherited.HandleInput(KeyDown(InputKey.Return));
        inherited.HandleInput(TextInput('x'));

        var block = new CodeEdit("if ready then", new LuaCodeLanguage());
        block.HandleInput(KeyDown(InputKey.Return));
        block.HandleInput(TextInput('x'));

        Assert.Equal("    local value = 1\n    x", inherited.Text);
        Assert.Equal("if ready then\n    x", block.Text);
    }

    [Fact]
    public void Code_editor_new_line_between_a_pair_creates_an_indented_block()
    {
        var editor = new CodeEdit(string.Empty, new LuaCodeLanguage());

        editor.HandleInput(TextInput('{'));
        editor.HandleInput(KeyDown(InputKey.Return));
        editor.HandleInput(TextInput('x'));

        Assert.Equal("{\n    x\n}", editor.Text);
    }

    [Fact]
    public void Code_editor_backspace_removes_an_empty_pair()
    {
        var editor = new CodeEdit(string.Empty, new LuaCodeLanguage());
        editor.HandleInput(TextInput('['));

        editor.HandleInput(KeyDown(InputKey.Backspace));

        Assert.Equal(string.Empty, editor.Text);
    }

    [Fact]
    public void Code_editor_tab_and_shift_tab_indent_selected_lines()
    {
        var editor = new CodeEdit("first\nsecond", new LuaCodeLanguage());
        editor.SelectAll();

        editor.HandleInput(KeyDown(InputKey.Tab));
        Assert.Equal("    first\n    second", editor.Text);

        editor.SelectAll();
        editor.HandleInput(KeyDown(InputKey.Tab, shift: true));
        Assert.Equal("first\nsecond", editor.Text);
    }

    [Fact]
    public void Code_editor_tab_expands_lua_snippet_and_visits_each_placeholder()
    {
        var editor = new CodeEdit("if", new LuaCodeLanguage());

        editor.HandleInput(KeyDown(InputKey.Tab));
        Assert.Equal("if A then\n    B\nend", editor.Text);

        editor.HandleInput(TextInput('x'));
        editor.HandleInput(KeyDown(InputKey.Tab));
        editor.HandleInput(TextInput('y'));
        editor.HandleInput(KeyDown(InputKey.Tab));
        editor.HandleInput(TextInput('!'));

        Assert.Equal("if x then\n    y\nend!", editor.Text);
    }

    [Fact]
    public void Code_editor_snippets_are_language_extensible_and_keep_line_indentation()
    {
        var editor = new CodeEdit("    when", new TestSnippetLanguage());

        editor.HandleInput(KeyDown(InputKey.Tab));

        Assert.Equal("    when A\n        B", editor.Text);
    }

    [Fact]
    public void Code_editor_only_requests_keyboard_restoration_while_editable()
    {
        var editor = new CodeEdit(string.Empty, new LuaCodeLanguage());

        Assert.True(editor.ShouldShowVirtualKeyboard);
        editor.IsReadOnly = true;
        Assert.False(editor.ShouldShowVirtualKeyboard);
    }

    [Fact]
    public void Navigation_stack_pushes_game_page_and_restores_root()
    {
        var navigation = new NanoNavigationStack();
        var root = new Rayo.Controls.Frame();
        var game = new Rayo.Controls.Frame();

        navigation.SetRoot(root);
        navigation.Push(game);

        Assert.Equal(2, navigation.Count);
        Assert.Same(game, navigation.Current);

        var previous = navigation.Pop();

        Assert.Equal(1, navigation.Count);
        Assert.Same(root, previous);
        Assert.Same(root, navigation.Current);
    }

    [Fact]
    public void New_project_contains_a_runnable_root_main_lua()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"nano-test-{Guid.NewGuid():N}");
        var archivePath = Path.Combine(directory, "Game.nn");

        try
        {
            var store = new NanoProjectStore(archivePath);

            var main = store.ReadText("main.lua");

            Assert.Contains("function update", main);
            Assert.Contains("function draw", main);
            Assert.Contains("nano.input.x", main);
            Assert.Contains("nano.draw.circle", main);
            Assert.DoesNotContain("nano.draw.line", main);
            Assert.Contains(
                store.GetChildren(string.Empty),
                asset => asset.Path.Equals("main.lua", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Previous_starter_is_upgraded_without_overwriting_custom_scripts()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"nano-test-{Guid.NewGuid():N}");
        var archivePath = Path.Combine(directory, "Game.nn");

        try
        {
            var store = new NanoProjectStore(archivePath);
            store.WriteText(
                "main.lua",
                """
                local x = 60
                local y = 80

                function update(dt)
                    x = math.max(0, math.min(nano.width - 48, x + nano.input.x * 140 * dt))
                    y = math.max(0, math.min(nano.height - 48, y + nano.input.y * 140 * dt))
                end

                function draw()
                    nano.draw.clear(12, 16, 24)
                    if nano.input.a then
                        nano.draw.rect(x, y, 48, 48, 75, 225, 145)
                    else
                        nano.draw.rect(x, y, 48, 48, 65, 180, 255)
                    end
                    nano.draw.line(16, nano.height - 32, nano.width - 16, nano.height - 32, 90, 110, 145)
                end
                """);

            store = new NanoProjectStore(archivePath);
            Assert.Contains("nano.draw.circle", store.ReadText("main.lua"));
            Assert.DoesNotContain("nano.draw.line", store.ReadText("main.lua"));

            const string customScript = "function draw() nano.draw.circle(1, 2, 3, 4, 5, 6) end";
            store.WriteText("main.lua", customScript);
            store = new NanoProjectStore(archivePath);
            Assert.Equal(customScript, store.ReadText("main.lua"));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Lua_game_executes_main_and_accesses_project_files()
    {
        var store = new FakeProjectAssetStore();
        store.WriteText(
            "main.lua",
            """
            nano.file.write("score.txt", "42")
            local score = tonumber(nano.file.read("score.txt"))

            function draw()
                nano.draw.clear(1, 2, 3)
                nano.draw.rect(4, 5, score, 7, 8, 9, 10)
            end
            """);

        using var engine = new NanoGameEngine(store);
        engine.RunFrame(1f / 60f, 320, 180);

        Assert.Null(engine.Error);
        Assert.Equal("42", store.ReadText("score.txt"));
        Assert.IsType<ClearCommand>(engine.Commands[0]);
        var rectangle = Assert.IsType<RectCommand>(engine.Commands[1]);
        Assert.Equal(42, rectangle.Width);
    }

    [Fact]
    public void Virtual_controls_support_joystick_and_action_button_multitouch()
    {
        var input = new NanoGameInputState();
        var controls = new VirtualGameControls(input);
        var tree = new UITree();
        tree.SetRoot(controls);
        tree.Update(390, 720);

        controls.OnPointerPressed(PointerEventArgs.FromTouch(1, new Vector2(88, 628)));
        controls.OnPointerMoved(PointerEventArgs.FromTouch(1, new Vector2(146, 628)));
        controls.OnPointerPressed(PointerEventArgs.FromTouch(2, new Vector2(325, 618)));

        Assert.True(input.X > 0.95f);
        Assert.True(input.Right);
        Assert.True(input.A);

        controls.OnPointerReleased(PointerEventArgs.FromTouch(2, new Vector2(325, 618)));
        controls.OnPointerReleased(PointerEventArgs.FromTouch(1, new Vector2(146, 628)));

        Assert.False(input.A);
        Assert.Equal(0, input.X);
        Assert.Equal(0, input.Y);
    }

    [Fact]
    public void Lua_input_reports_axes_directions_and_pressed_edges()
    {
        var store = new FakeProjectAssetStore();
        store.WriteText(
            "main.lua",
            """
            function draw()
                if nano.input.right and nano.input.a_pressed then
                    nano.draw.rect(0, 0, nano.input.x * 100, 10, 255, 255, 255)
                end
            end
            """);
        var input = new NanoGameInputState { X = 0.75f, A = true };

        using var engine = new NanoGameEngine(store, input);
        engine.RunFrame(1f / 60f, 320, 180);

        var rectangle = Assert.IsType<RectCommand>(Assert.Single(engine.Commands));
        Assert.Equal(75, rectangle.Width);

        engine.RunFrame(1f / 60f, 320, 180);
        Assert.Empty(engine.Commands);
    }

    [Fact]
    public void Embedded_debug_game_archive_executes_its_main_lua()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"nano-test-{Guid.NewGuid():N}");
        var archivePath = Path.Combine(directory, "game.nn");

        try
        {
            Directory.CreateDirectory(directory);
            using (var source = typeof(NanoProjectStore).Assembly
                       .GetManifestResourceStream("Nano.Assets.game.nn"))
            {
                Assert.NotNull(source);
                using var destination = File.Create(archivePath);
                source.CopyTo(destination);
            }

            var store = new NanoProjectStore(archivePath);
            Assert.Contains(
                store.GetChildren(string.Empty),
                asset => asset.Path.Equals("sprites", StringComparison.OrdinalIgnoreCase));

            var input = new NanoGameInputState { X = 1 };
            using var engine = new NanoGameEngine(store, input);
            engine.RunFrame(1f / 60f, 390, 720);

            Assert.Null(engine.Error);
            Assert.Contains(engine.Commands, command => command is CircleCommand);

            void AssertFps() => Assert.Contains(
                engine.Commands,
                command => command is TextCommand text && text.Text.StartsWith("FPS ", StringComparison.Ordinal));

            AssertFps();

            void Click(float x, float y)
            {
                input.PointerX = x;
                input.PointerY = y;
                input.PointerDown = true;
                input.PointerPressed = true;
                engine.RunFrame(1f / 60f, 390, 720);
                input.PointerDown = false;
                input.PointerReleased = true;
                engine.RunFrame(1f / 60f, 390, 720);
                engine.RunFrame(1f / 60f, 390, 720);
                Assert.Null(engine.Error);
            }

            Click(120, 240);
            Assert.Contains(engine.Commands, command => command is TextCommand { Text: "PLATFORMER  COINS 0/3" });
            AssertFps();

            Click(345, 91);
            Assert.Equal(0, engine.ResourceCounts.PhysicsWorlds);
            Assert.Equal(0, engine.ResourceCounts.PhysicsBodies);
            Click(120, 325);
            Assert.Contains(engine.Commands, command => command is TextCommand { Text: "PHYSICS LAB" });
            AssertFps();

            Click(345, 91);
            Assert.Equal(0, engine.ResourceCounts.PhysicsWorlds);
            Assert.Equal(0, engine.ResourceCounts.PhysicsBodies);
            Click(120, 410);
            Assert.Contains(engine.Commands, command => command is TextCommand { Text: "ENGINE SHOWCASE" });
            AssertFps();
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Engine_dispose_invokes_lua_shutdown_and_clears_physics_resources()
    {
        var store = new FakeProjectAssetStore();
        store.WriteText(
            "main.lua",
            """
            local world = nano.physics.new_world(0, 100)
            nano.physics.new_circle(world, 10, 10, 4)
            function shutdown()
                nano.file.write("shutdown.txt", "called")
                nano.physics.destroy_world(world)
            end
            """);
        var engine = new NanoGameEngine(store);

        Assert.Equal(1, engine.ResourceCounts.PhysicsWorlds);
        Assert.Equal(1, engine.ResourceCounts.PhysicsBodies);

        engine.Dispose();

        Assert.Equal("called", store.ReadText("shutdown.txt"));
        Assert.Equal(0, engine.ResourceCounts.PhysicsWorlds);
        Assert.Equal(0, engine.ResourceCounts.PhysicsBodies);
    }

    [Fact]
    public void Network_release_removes_request_state_immediately()
    {
        using var network = new NanoNetworkService();
        var handle = network.Get("invalid://request");

        Assert.Equal(1, network.RequestCount);
        network.Release(handle);

        Assert.Equal(0, network.RequestCount);
    }

    [Fact]
    public void Audio_preload_reads_an_asset_only_once()
    {
        var reads = 0;
        using var audio = new NanoAudioService(_ =>
        {
            reads++;
            return [0, 1, 2, 3];
        });

        audio.Preload("sound.wav");
        audio.Preload("sound.wav");

        Assert.Equal(1, reads);
    }


    [Fact]
    public void Physics_service_integrates_gravity_and_resolves_a_circle_against_a_floor()
    {
        var physics = new NanoPhysicsService();
        var world = physics.CreateWorld(0, 100);
        var ball = physics.CreateCircle(world, 0, 0, 1, 1, BodyKind.Dynamic);
        physics.CreateBox(world, 0, 10, 30, 2, 1, BodyKind.Static);
        physics.SetRestitution(ball, 0);

        for (var frame = 0; frame < 240; frame++)
            physics.Step(world, 1f / 60f, 6);

        var state = physics.GetBody(ball);
        Assert.InRange(state.Y, 7.8f, 8.1f);
        Assert.InRange(Math.Abs(state.VelocityY), 0, 0.2f);
    }

    [Fact]
    public void Lua_physics_api_can_move_and_query_a_body()
    {
        var store = new FakeProjectAssetStore();
        store.WriteText(
            "main.lua",
            """
            local world = nano.physics.new_world(0, 0)
            local body = nano.physics.new_circle(world, 10, 20, 4)
            nano.physics.apply_impulse(body, 30, 0)

            function update(dt)
                nano.physics.step(world, dt)
            end

            function draw()
                local state = nano.physics.body(body)
                nano.draw.circle(state.x, state.y, 4, 255, 255, 255)
            end
            """);

        using var engine = new NanoGameEngine(store);
        engine.RunFrame(0.5f, 320, 180);

        Assert.Null(engine.Error);
        var circle = Assert.IsType<CircleCommand>(Assert.Single(engine.Commands));
        Assert.True(circle.CenterX > 10);
        Assert.Equal(20, circle.CenterY);
    }

    [Fact]
    public void Engine_ui_renders_bitmap_text_and_handles_a_button_click()
    {
        var store = new FakeProjectAssetStore();
        store.WriteText(
            "main.lua",
            """
            function draw()
                nano.ui.panel(8, 8, 140, 80, "MENU")
                if nano.ui.button("play", "PLAY", 18, 42, 100, 30) then
                    nano.file.write("clicked.txt", "yes")
                end
            end
            """);
        var input = new NanoGameInputState();
        using var engine = new NanoGameEngine(store, input);

        engine.RunFrame(1f / 60f, 320, 180);
        Assert.Contains(engine.Commands, command => command is TextCommand { Text: "MENU" });

        input.PointerX = 40;
        input.PointerY = 55;
        input.PointerDown = true;
        input.PointerPressed = true;
        engine.RunFrame(1f / 60f, 320, 180);

        input.PointerDown = false;
        input.PointerReleased = true;
        engine.RunFrame(1f / 60f, 320, 180);

        Assert.Equal("yes", store.ReadText("clicked.txt"));
    }

    [Fact]
    public void Sdl_memory_surface_renders_without_a_video_window()
    {
        using var scene = new NanoSdlScene(forceMemorySurface: true);
        NanoGameCommand[] commands =
        [
            new ClearCommand(new GameColor(1, 2, 3)),
            new CircleCommand(16, 16, 8, new GameColor(40, 120, 220)),
            new TextCommand("UI", 2, 2, 1, new GameColor(255, 255, 255))
        ];

        var pixels = scene.RenderFrame(32, 32, commands);

        Assert.Equal(32 * 32 * 4, pixels.Length);
        Assert.Contains(pixels, value => value != 0);

        var nextPixels = scene.RenderFrame(32, 32, commands);
        Assert.Same(pixels, nextPixels);
    }

    [Fact]
    public void Frame_ticker_does_not_throttle_ordinary_frame_animations()
    {
        var animation = new CountingFrameAnimation();
        FrameAnimationTicker.Register(animation);
        try
        {
            FrameAnimationTicker.Tick(0.005f);
            Assert.Equal(1, animation.TickCount);
            Assert.Equal(0.005f, animation.Elapsed, 4);
        }
        finally
        {
            FrameAnimationTicker.Unregister(animation);
        }
    }

    [Fact]
    public void Lua_fps_is_averaged_instead_of_following_each_frame_delta()
    {
        var store = new FakeProjectAssetStore();
        store.WriteText(
            "main.lua",
            """
            function draw()
                nano.draw.rect(0, 0, nano.time.fps, nano.time.frame_time, 255, 255, 255)
            end
            """);
        using var engine = new NanoGameEngine(store);
        var samples = new List<float>();

        for (var frame = 0; frame < 30; frame++)
        {
            engine.RunFrame(frame % 2 == 0 ? 1f / 50f : 1f / 30f, 320, 180);
            if (frame >= 22)
                samples.Add(Assert.IsType<RectCommand>(Assert.Single(engine.Commands)).Width);
        }

        Assert.Single(samples.Distinct());
        Assert.InRange(samples[0], 36, 39);
    }

    [Fact]
    public void Sprite_asset_document_round_trips_canvas_frames_and_animations()
    {
        var document = SpriteAssetDocument.CreateBlank(24, 12);
        document.Frames.Add(SpriteFrameDocument.FromFrame(document.Frames[0].ToFrame(24, 12)));
        document.Animations[0].FrameIndices = [0, 1];

        var restored = SpriteAssetDocument.Deserialize(document.Serialize());
        restored.Validate();

        Assert.Equal(24, restored.Width);
        Assert.Equal(12, restored.Height);
        Assert.Equal(2, restored.Frames.Count);
        Assert.Equal([0, 1], restored.Animations[0].FrameIndices);
        Assert.Equal(24 * 12 * 4, restored.Frames[0].Pixels.Length);
        Assert.All(
            restored.Frames[0].ToFrame(24, 12).Pixels.Cast<Color>(),
            color => Assert.Equal(0f, color.A));
    }

    [Fact]
    public void Project_explorer_view_model_owns_navigation_and_view_mode()
    {
        var store = new FakeProjectAssetStore();
        var viewModel = new ProjectAssetExplorerViewModel(store);

        viewModel.NavigateTo("scripts");
        viewModel.SetViewMode(AssetViewMode.Grid);

        Assert.Equal("scripts", viewModel.CurrentDirectory.Value);
        Assert.Equal(AssetViewMode.Grid, viewModel.ViewMode.Value);
        Assert.Equal("main.lua", Assert.Single(viewModel.Assets).Name);

        viewModel.NavigateUp();

        Assert.Equal(string.Empty, viewModel.CurrentDirectory.Value);
    }

    [Fact]
    public void Project_explorer_restores_the_last_navigation_directory()
    {
        var store = new FakeProjectAssetStore();
        var rememberedDirectory = string.Empty;
        var first = new ProjectAssetExplorerViewModel(
            store,
            rememberedDirectory,
            directory => rememberedDirectory = directory);

        first.NavigateTo("scripts");

        var reopened = new ProjectAssetExplorerViewModel(
            store,
            rememberedDirectory,
            directory => rememberedDirectory = directory);

        Assert.Equal("scripts", rememberedDirectory);
        Assert.Equal("scripts", reopened.CurrentDirectory.Value);
        Assert.Equal("main.lua", Assert.Single(reopened.Assets).Name);
    }

    [Fact]
    public void Asset_tile_inside_scroll_view_opens_on_touch_and_recovers_after_cancel()
    {
        var openCount = 0;
        var tile = new AssetTile(() => openCount++)
            .Height(40);
        var root = new ScrollView()
            .Content(
                new Rayo.Layout.VStack()
                    .Children(tile, new Frame().Height(800)));
        var tree = new UITree();
        tree.SetRoot(root);
        tree.InitializeEventManager(null);
        tree.Update(320, 480);

        var position = new Vector2(
            tile.ComputedX + tile.ComputedWidth / 2,
            tile.ComputedY + tile.ComputedHeight / 2);
        var canceled = PointerEventArgs.FromTouch(1, position, 0);
        canceled.IsInContact = false;

        tree.EventManager!.ProcessTouchDown(PointerEventArgs.FromTouch(1, position, 1));
        tree.EventManager.ProcessTouchCancel(canceled);

        Assert.Equal(0, openCount);

        tree.EventManager.ProcessTouchDown(PointerEventArgs.FromTouch(2, position, 1));
        var released = PointerEventArgs.FromTouch(2, position, 0);
        released.IsInContact = false;
        tree.EventManager.ProcessTouchUp(released);

        Assert.Equal(1, openCount);
    }

    [Fact]
    public void Home_view_model_reuses_and_reindexes_document_tabs()
    {
        var viewModel = new HomeViewModel(fixedTabCount: 3);

        var first = viewModel.OpenTextAsset("a.lua", "a", _ => { });
        var second = viewModel.OpenTextAsset("b.lua", "b", _ => { });
        var existing = viewModel.OpenTextAsset("a.lua", "ignored", _ => { });

        Assert.Equal(3, first.TabIndex);
        Assert.Equal(4, second.TabIndex);
        Assert.False(existing.IsNew);
        Assert.Equal(3, existing.TabIndex);

        Assert.True(viewModel.CloseTextAsset(3));
        Assert.Equal(
            3,
            viewModel.OpenTextAsset("b.lua", "ignored", _ => { }).TabIndex);
    }

    [Fact]
    public void Open_text_document_saves_only_changes_and_keeps_latest_text_for_rebuilds()
    {
        var saved = new List<string>();
        var viewModel = new HomeViewModel();
        var document = viewModel.OpenTextAsset("main.lua", "old", saved.Add).Document!;

        document.UpdateText("old");
        document.UpdateText("updated");
        document.UpdateText("updated");
        viewModel.SaveAllTextAssets();

        Assert.Equal("updated", document.Text);
        Assert.False(document.IsModified);
        Assert.Equal(["updated"], saved);
    }

    [Fact]
    public void Editing_an_open_asset_persists_the_new_text()
    {
        var saved = new List<string>();
        var home = new Nano.Views.HomePage();
        var tabs = Assert.IsType<TabControl>(home.Build());
        home.OpenTextAsset("main.lua", string.Empty, saved.Add);
        var editor = Assert.IsType<CodeEdit>(tabs.SelectedTab!.Content);

        editor.HandleInput(TextInput('x'));

        Assert.Equal("x", editor.Text);
        Assert.Equal(["x"], saved);
        Assert.Equal("x", Assert.Single(home.ViewModel.Documents).Text);

        home.OpenTextAsset("main.lua", "stale", saved.Add);

        Assert.Equal(0, tabs.SelectedIndex);
        Assert.Single(home.ViewModel.Documents);
    }

    [Fact]
    public void Level_editor_places_tiles_and_replaces_objects_per_cell()
    {
        var viewModel = new LevelEditorViewModel();

        viewModel.PlaceAt(2, 3);
        Assert.Same(viewModel.Tiles[0], viewModel.GetTileAt(2, 3));
        viewModel.PlaceAt(-4, -7);
        Assert.Same(viewModel.Tiles[0], viewModel.GetTileAt(-4, -7));

        viewModel.ShowObjects();
        viewModel.SelectObject(viewModel.Objects[0]);
        viewModel.PlaceAt(2, 3);
        viewModel.SelectObject(viewModel.Objects[1]);
        viewModel.PlaceAt(2, 3);

        var placedObject = Assert.Single(viewModel.ObjectInstances);
        Assert.Equal(viewModel.Objects[1].Id, placedObject.DefinitionId);
    }

    [Fact]
    public void Sprite_editor_view_model_manages_frames_and_history()
    {
        var viewModel = new SpriteEditorViewModel();
        var red = new Color(220, 40, 40);
        var blue = new Color(40, 80, 220);

        viewModel.CurrentFrame.Pixels[0, 0] = red;
        viewModel.RecordCurrentFrameState();
        viewModel.CurrentFrame.Pixels[0, 0] = blue;
        viewModel.RecordCurrentFrameState();

        Assert.True(viewModel.Undo());
        Assert.Equal(red, viewModel.CurrentFrame.Pixels[0, 0]);

        var initialCount = viewModel.Frames.Count;
        viewModel.CloneFrame(0);

        Assert.Equal(initialCount + 1, viewModel.Frames.Count);
        Assert.Equal(1, viewModel.SelectedFrameIndex.Value);
    }

    private sealed class FakeProjectAssetStore : IProjectAssetStore
    {
        private readonly Dictionary<string, string> _files =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["scripts/main.lua"] = "print('Nano')",
                ["main.lua"] = "function draw() end"
            };

        public string ArchivePath => "Test.nn";

        public IReadOnlyList<VirtualAsset> GetChildren(string directory)
        {
            if (directory == "scripts")
            {
                return [new VirtualAsset("scripts/main.lua", "main.lua", false)];
            }

            return [new VirtualAsset("scripts", "scripts", true)];
        }

        public void CreateDirectory(string parentDirectory, string name)
        {
        }

        public VirtualAsset CreateSprite(string parentDirectory, string name)
        {
            var fileName = name.EndsWith(".sprite", StringComparison.OrdinalIgnoreCase)
                ? name
                : $"{name}.sprite";
            var path = string.IsNullOrEmpty(parentDirectory)
                ? fileName
                : $"{parentDirectory}/{fileName}";
            _files[path] = string.Empty;
            return new VirtualAsset(path, fileName, false);
        }

        public string ReadText(string path) => _files[path];

        public byte[] ReadBytes(string path) => System.Text.Encoding.UTF8.GetBytes(_files[path]);

        public void WriteText(string path, string text) => _files[path] = text;

        public bool IsTextFile(string path) =>
            Path.GetExtension(path).Equals(
                ".lua",
                StringComparison.OrdinalIgnoreCase);

        public bool IsSpriteFile(string path) =>
            path.EndsWith(".sprite", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CountingFrameAnimation : IFrameAnimation
    {
        public int TickCount { get; private set; }
        public float Elapsed { get; private set; }

        public void Tick(float deltaTime)
        {
            TickCount++;
            Elapsed += deltaTime;
        }
    }

    private static InputEventArgs TextInput(char character) => new()
    {
        EventType = InputEventType.TextInput,
        Character = character
    };

    private static InputEventArgs KeyDown(InputKey key, bool shift = false) => new()
    {
        EventType = InputEventType.KeyDown,
        KeyCode = key,
        IsShiftPressed = shift
    };

    private sealed class TestSnippetLanguage : ICodeLanguage, ICodeSnippetProvider
    {
        public IReadOnlyList<CodeSnippet> Snippets { get; } =
        [new("when", "when ${1:A}\n\t${2:B}${0}")];

        public IEnumerable<CodeToken> Tokenize(string line)
        {
            yield return new CodeToken(line, CodeTokenKind.Plain);
        }
    }
}
