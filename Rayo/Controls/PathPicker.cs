namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static Rayo.Core.UIHelpers;
using Rayo.Styling;

public enum PathPickerMode
{
    File,
    OpenFile = File,
    Folder,
    FileOrFolder,
    SaveFile
}

public enum SaveFileConflictBehavior
{
    Overwrite,
    Reject
}

/// <summary>
/// File-system picker control that can select files, folders, or either.
/// </summary>
public class PathPicker : BorderCompositeView<PathPicker>, IPointerHandler, IGlobalPointerHandler
{
    private static PathPicker? _currentlyOpenPathPicker;

    private Frame? _pickerButton;
    private Label? _selectedPathLabel;
    private Icon? _modeIcon;
    private Icon? _browseIcon;
    private Frame? _dialogOverlay;
    private Frame? _dialogCard;
    private Frame? _listFrame;
    private Label? _currentDirectoryLabel;
    private Label? _statusLabel;
    private ListView<FileSystemEntry>? _entriesListView;
    private Entry? _fileNameEntry;
    private string? _pendingSelection;
    private bool _pendingSelectionIsDirectory;
    private Action<string>? _dialogConfirmed;
    private Action? _dialogCanceled;
    private bool _isOpen;
    private bool _isPressed;
    private CancellationTokenSource? _entriesLoadCancellation;
    private int _entriesLoadVersion;
    private string? _loadedEntriesDirectory;
    private DirectoryLoadResult? _loadedEntries;
    private readonly Dictionary<string, ListViewItem> _visibleEntryRowsByPath = new();
    private readonly Dictionary<ListViewItem, string> _pathByVisibleEntryRow = new();

    public PathPicker()
    {
        InitializeTheme();
        CurrentDirectory = ResolveInitialDirectory(null);
        BuildComponents();

        if (_pickerButton != null)
        {
            AddChild(_pickerButton);
        }
    }

    #region Mode
    [LayoutProperty]
    public PathPickerMode Mode
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            ClearLoadedEntries();
            UpdateTriggerContent();
            RebuildDialogContent();
        });
    } = PathPickerMode.File;
    #endregion

    #region SelectedPath
    [LayoutProperty]
    public string? SelectedPath
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            UpdateSelectedPathLabel();
            if (!string.IsNullOrWhiteSpace(value))
            {
                PathChanged?.Invoke(value);
            }
        });
    }
    #endregion

    #region InitialDirectory
    [LayoutProperty]
    public string? InitialDirectory
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            CurrentDirectory = ResolveInitialDirectory(value);
            ClearLoadedEntries();
            RebuildDialogContent();
        });
    }
    #endregion

    #region DefaultDirectory
    [LayoutProperty]
    public string? DefaultDirectory
    {
        get => InitialDirectory;
        set => InitialDirectory = value;
    }
    #endregion

    #region Placeholder
    [PaintProperty]
    public string Placeholder
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateSelectedPathLabel);
    } = "Select path...";
    #endregion

    #region DialogTitle
    [PaintProperty]
    public string? DialogTitle
    {
        get => field;
        set => this.SetProperty(ref field, value, () => RebuildDialogContent());
    }
    #endregion

    #region FileExtensions
    [LayoutProperty]
    public List<string> FileExtensions
    {
        get => field;
        set => this.SetProperty(ref field, value ?? new List<string>(), () =>
        {
            ClearLoadedEntries();
            RebuildDialogContent();
        });
    } = new();
    #endregion

    #region SupportedFileExtensions
    [LayoutProperty]
    public List<string> SupportedFileExtensions
    {
        get => FileExtensions;
        set => FileExtensions = value;
    }
    #endregion

    #region DefaultFileName
    [LayoutProperty]
    public string DefaultFileName
    {
        get => field;
        set => this.SetProperty(ref field, value ?? string.Empty, () =>
        {
            if (Mode == PathPickerMode.SaveFile && _fileNameEntry != null && string.IsNullOrWhiteSpace(_fileNameEntry.Text))
            {
                _fileNameEntry.Text = field;
            }
        });
    } = "untitled";
    #endregion

    #region SaveConflictBehavior
    [LayoutProperty]
    public SaveFileConflictBehavior SaveConflictBehavior
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = SaveFileConflictBehavior.Overwrite;
    #endregion

    #region ShowHidden
    [LayoutProperty]
    public bool ShowHidden
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            ClearLoadedEntries();
            RebuildDialogContent();
        });
    }
    #endregion

    #region MaxItems
    [LayoutProperty]
    public int MaxItems
    {
        get => field;
        set => this.SetProperty(ref field, Math.Max(1, value), () =>
        {
            ClearLoadedEntries();
            RebuildDialogContent();
        });
    } = 500;
    #endregion

    #region Styling
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateSelectedPathLabel);
    } = Color.Transparent;

    [PaintProperty]
    public Brush PlaceholderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateSelectedPathLabel);
    } = Color.Transparent;

    [PaintProperty]
    public Brush AccentColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_modeIcon != null)
            {
                _modeIcon.Color = value;
            }
        });
    } = Color.Transparent;

    [LayoutProperty]
    public float FieldCornerRadius
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_pickerButton != null)
            {
                _pickerButton.BorderRadius = new CornerRadius(value);
            }
        });
    } = 4;
    #endregion

    [NotFluent]
    public string CurrentDirectory { get; private set; }

    public event Action<string>? PathChanged;

    public event Action? SelectionCanceled;

    protected override void OnThemeApplied(ThemeData theme)
    {
        var palette = theme.Colors;
        SetThemeValue(nameof(Background), (Brush)palette.Surface, value => Background = value);
        SetThemeValue(nameof(TextColor), (Brush)palette.OnSurface, value => TextColor = value);
        SetThemeValue(nameof(PlaceholderColor), (Brush)palette.OnDisabled, value => PlaceholderColor = value);
        SetThemeValue(nameof(AccentColor), (Brush)palette.Primary, value => AccentColor = value);
        SetThemeValue(nameof(BorderBrush), (Brush)palette.Border, value => BorderBrush = value);

        if (_browseIcon != null)
            _browseIcon.Color = PlaceholderColor;
        if (_pickerButton != null)
            _pickerButton.Background = Background;

        if (_isOpen)
            RebuildDialogContent(preserveScrollOffset: true);
    }

    public static void CloseCurrentPathPicker()
    {
        _currentlyOpenPathPicker?.ClosePicker();
    }

    public static PathPicker ShowDialog(PathPickerMode mode, Action<string> onConfirm, Action? onCancel = null, Action<PathPicker>? configure = null)
    {
        var picker = new PathPicker
        {
            Mode = mode
        };
        configure?.Invoke(picker);
        picker.OpenDialog(onConfirm, onCancel);
        return picker;
    }

    public static PathPicker ShowDialog(string initialDirectory, PathPickerMode mode, Action<string> onConfirm, Action? onCancel = null, Action<PathPicker>? configure = null)
    {
        return ShowDialog(mode, onConfirm, onCancel, picker =>
        {
            picker.InitialDirectory = initialDirectory;
            configure?.Invoke(picker);
        });
    }

    public void OpenPicker()
    {
        if (_isOpen)
        {
            return;
        }

        if (_currentlyOpenPathPicker != null && _currentlyOpenPathPicker != this)
        {
            _currentlyOpenPathPicker.ClosePicker();
        }

        _currentlyOpenPathPicker = this;
        _isOpen = true;
        _pendingSelection = null;
        _pendingSelectionIsDirectory = false;
        CurrentDirectory = ResolveInitialDirectory(InitialDirectory ?? SelectedPath);
        ClearLoadedEntries();
        _dialogOverlay = BuildDialogOverlay();

        OverlayManager.AddOverlay(_dialogOverlay, this);
        OverlayManager.EventManager?.RegisterGlobalPointerHandler(this);
    }

    public void ClosePicker()
    {
        if (!_isOpen)
        {
            return;
        }

        CancelEntriesLoad();

        if (_dialogOverlay != null)
        {
            OverlayManager.RemoveOverlay(_dialogOverlay);
        }

        _isOpen = false;
        _dialogOverlay = null;
        _dialogCard = null;
        _listFrame = null;
        _entriesListView = null;
        _fileNameEntry = null;
        _currentDirectoryLabel = null;
        _statusLabel = null;
        _pendingSelection = null;
        _pendingSelectionIsDirectory = false;
        ClearLoadedEntries();

        if (_currentlyOpenPathPicker == this)
        {
            _currentlyOpenPathPicker = null;
        }

        OverlayManager.EventManager?.UnregisterGlobalPointerHandler(this);
    }

    public void TogglePicker()
    {
        if (_isOpen)
        {
            ClosePicker();
        }
        else
        {
            OpenPicker();
        }
    }

    protected void OpenDialog(Action<string> onConfirm, Action? onCancel = null)
    {
        _dialogConfirmed = onConfirm;
        _dialogCanceled = onCancel;
        OpenPicker();
    }

    public void NavigateTo(string directory)
    {
        var normalized = ResolveDirectory(directory);
        if (normalized == null)
        {
            SetStatus("The selected folder is not available.");
            return;
        }

        CurrentDirectory = normalized;
        _pendingSelection = null;
        _pendingSelectionIsDirectory = false;
        ClearLoadedEntries();
        RebuildDialogContent();
    }

    private void BuildComponents()
    {
        _modeIcon = new Icon(GetModeIcon())
        {
            Width = 20,
            Height = 20,
            Color = AccentColor
        };
        _modeIcon.SetInputTransparent(true);

        _selectedPathLabel = new Label()
            .FontSize(14)
            .TextVerticalAlignment(VerticalAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .SetInputTransparent(true);

        _browseIcon = new Icon(Icons.Search)
        {
            Width = 18,
            Height = 18,
            Color = PlaceholderColor
        };
        _browseIcon.SetInputTransparent(true);

        var selectedPathScroll = new ScrollView()
        {
            Orientation = ScrollOrientation.Horizontal,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            ClipToBounds = true
        }
            .Height(24)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center)
            .Content(_selectedPathLabel);

        var content = new Grid()
            .Rows(GridLength.Star)
            .Columns(GridLength.Pixels(24), GridLength.Star, GridLength.Pixels(22))
            .ColumnSpacing(10)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .AddChild(_modeIcon, 0, 0)
            .AddChild(selectedPathScroll, 0, 1)
            .AddChild(_browseIcon, 0, 2);

        _pickerButton = new Frame()
            .Background(Background)
            .BorderBrush(BorderBrush)
            .BorderThickness(BorderThickness)
            .Padding(new Thickness(12, 6, 12, 6))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
        _pickerButton.BorderRadius = new CornerRadius(FieldCornerRadius);
        _pickerButton.Content(content);

        UpdateTriggerContent();
    }

    protected override void OnBorderBrushChanged()
    {
        base.OnBorderBrushChanged();
        _pickerButton?.BorderBrush(BorderBrush);
    }

    protected override void OnBorderThicknessChanged()
    {
        base.OnBorderThicknessChanged();
        _pickerButton?.BorderThickness(BorderThickness);
    }

    private Frame BuildDialogOverlay()
    {
        var overlay = new DialogOverlayFrame()
            .Background(new Color(0, 0, 0, 0.65f))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        _dialogCard = new Frame()
            .Width(620)
            .Height(560)
            .Background(EffectiveTheme.Colors.Surface)
            .BorderBrush(EffectiveTheme.Colors.Border)
            .BorderThickness(1)
            .BorderRadius(new CornerRadius(14))
            .Padding(new Thickness(16))
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center);
        _dialogCard.ClipToBounds = true;

        _dialogCard.Content(BuildDialogContent());
        overlay.Content(_dialogCard);
        return overlay;
    }

    private VisualElement BuildDialogContent(float scrollOffset = 0f, bool restoreScrollOffset = false)
    {
        var title = new Label(GetDialogTitle())
            .FontSize(18)
            .Height(28)
            .Foreground(TextColor);

        _currentDirectoryLabel = new Label(CurrentDirectory)
            .FontSize(13)
            .Foreground(PlaceholderColor)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var pathFrame = new Frame()
            .Height(42)
            .Background(EffectiveTheme.Colors.SurfaceHover)
            .BorderRadius(new CornerRadius(10))
            .Padding(new Thickness(12, 8, 12, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(_currentDirectoryLabel);

        var loadedEntries = GetLoadedEntriesForCurrentDirectory();
        var statusText = loadedEntries != null ? GetStatusText(loadedEntries) : string.Empty;
        _statusLabel = new Label(statusText)
            .FontSize(12)
            .Foreground(string.IsNullOrEmpty(statusText) ? Color.Transparent : PlaceholderColor);

        _listFrame = new Frame()
            .Background(EffectiveTheme.Colors.Surface)
            .BorderBrush(EffectiveTheme.Colors.Border)
            .BorderThickness(1)
            .BorderRadius(new CornerRadius(12))
            .Padding(new Thickness(8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
        _listFrame.ClipToBounds = true;

        if (loadedEntries != null)
        {
            BuildLoadedItems(loadedEntries);
        }
        else
        {
            ShowLoadingItems();
        }

        var content = Mode == PathPickerMode.SaveFile
            ? new Grid()
                .Rows(
                    GridLength.Pixels(28),
                    GridLength.Pixels(34),
                    GridLength.Pixels(42),
                    GridLength.Pixels(58),
                    GridLength.Star,
                    GridLength.Pixels(20),
                    GridLength.Pixels(36))
                .Columns(GridLength.Star)
                .RowSpacing(10)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .AddChild(title, 0, 0)
                .AddChild(BuildNavigationBar(), 1, 0)
                .AddChild(pathFrame, 2, 0)
                .AddChild(BuildSaveFileNameRow(), 3, 0)
                .AddChild(_listFrame, 4, 0)
                .AddChild(_statusLabel, 5, 0)
                .AddChild(BuildDialogButtons(), 6, 0)
            : new Grid()
                .Rows(
                    GridLength.Pixels(28),
                    GridLength.Pixels(34),
                    GridLength.Pixels(42),
                    GridLength.Star,
                    GridLength.Pixels(20),
                    GridLength.Pixels(36))
                .Columns(GridLength.Star)
                .RowSpacing(10)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .AddChild(title, 0, 0)
                .AddChild(BuildNavigationBar(), 1, 0)
                .AddChild(pathFrame, 2, 0)
                .AddChild(_listFrame, 3, 0)
                .AddChild(_statusLabel, 4, 0)
                .AddChild(BuildDialogButtons(), 5, 0);

        if (loadedEntries == null)
        {
            StartEntriesLoad(scrollOffset, restoreScrollOffset);
        }
        return content;
    }

    private DirectoryLoadResult? GetLoadedEntriesForCurrentDirectory()
    {
        return _loadedEntries != null &&
            string.Equals(_loadedEntriesDirectory, CurrentDirectory, PathComparison)
            ? _loadedEntries
            : null;
    }

    private void ClearLoadedEntries()
    {
        _loadedEntries = null;
        _loadedEntriesDirectory = null;
        _visibleEntryRowsByPath.Clear();
        _pathByVisibleEntryRow.Clear();
    }

    private void ShowLoadingItems()
    {
        if (_listFrame == null)
        {
            return;
        }

        _entriesListView = null;
        _listFrame.Content(
            new Grid()
                .Rows(GridLength.Star, GridLength.Pixels(28), GridLength.Star)
                .Columns(GridLength.Star, GridLength.Pixels(28), GridLength.Star)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .AddChild(
                    new Loading()
                        .Size(new Size(28))
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .VerticalAlignment(VerticalAlignment.Center),
                    1,
                    1));
    }

    private void StartEntriesLoad(float scrollOffset = 0f, bool restoreScrollOffset = false)
    {
        if (!_isOpen)
        {
            return;
        }

        CancelEntriesLoad();

        var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _entriesLoadCancellation = cancellation;
        int version = ++_entriesLoadVersion;
        string directory = CurrentDirectory;
        var mode = Mode;
        var showHidden = ShowHidden;
        var maxItems = MaxItems;
        var extensions = FileExtensions.Select(NormalizeExtension).ToList();

        _ = Task.Run(
            () => LoadEntries(directory, mode, extensions, showHidden, maxItems, token),
            token)
            .ContinueWith(task =>
            {
                if (task.IsCanceled || token.IsCancellationRequested)
                {
                    return;
                }

                DirectoryLoadResult result = task.IsFaulted
                    ? new DirectoryLoadResult([], "This folder cannot be opened.", false)
                    : task.Result;

                UIUpdateQueue.EnqueueUIUpdate(() =>
                    ApplyEntriesLoadResult(version, directory, result, scrollOffset, restoreScrollOffset));
                (UIApplication.Current?.Tree ?? UITree.Current)?.MarkNeedsRender();
            }, CancellationToken.None);
    }

    private void CancelEntriesLoad()
    {
        _entriesLoadCancellation?.Cancel();
        _entriesLoadCancellation?.Dispose();
        _entriesLoadCancellation = null;
    }

    private void ApplyEntriesLoadResult(
        int version,
        string directory,
        DirectoryLoadResult result,
        float scrollOffset,
        bool restoreScrollOffset)
    {
        if (!_isOpen ||
            version != _entriesLoadVersion ||
            !string.Equals(directory, CurrentDirectory, PathComparison) ||
            _listFrame == null)
        {
            return;
        }

        _loadedEntries = result;
        _loadedEntriesDirectory = directory;
        BuildLoadedItems(result);
        string status = GetStatusText(result);

        if (_statusLabel != null)
        {
            _statusLabel.Text = status;
            _statusLabel.Foreground = string.IsNullOrEmpty(status) ? Color.Transparent : PlaceholderColor;
        }

        if (restoreScrollOffset && _entriesListView != null)
        {
            _entriesListView.VerticalScrollOffset = scrollOffset;
            _entriesListView.MarkNeedsPaint();
        }

        RefreshDialogLayout();
    }

    private void BuildLoadedItems(DirectoryLoadResult result)
    {
        if (_listFrame == null)
        {
            return;
        }

        var entries = result.Entries as IList<FileSystemEntry> ?? result.Entries.ToList();

        _entriesListView = new ListView<FileSystemEntry>
        {
            Items = entries,
            ItemHeight = 38,
            ItemSpacing = 4,
            ItemBackground = Color.Transparent,
            ItemHoverBackground = EffectiveTheme.Colors.SurfaceHover,
            ItemSelectedBackground = GetSelectedEntryBackground(),
            ItemFactory = CreateEntryRow,
            ItemBinder = BindEntryRow,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        int selectedIndex = -1;
        for (int i = 0; i < entries.Count; i++)
        {
            if (IsPendingSelection(entries[i].Path))
            {
                selectedIndex = i;
                break;
            }
        }

        _listFrame.Content(_entriesListView);
    }

    private string GetStatusText(DirectoryLoadResult result)
    {
        if (result.Truncated)
        {
            return $"Showing the first {MaxItems} items.";
        }

        if (!string.IsNullOrEmpty(result.Status))
        {
            return result.Status;
        }

        if (result.Entries.Count == 0)
        {
            return Mode == PathPickerMode.Folder
                ? "This folder does not contain visible folders."
                : "This folder does not contain matching files.";
        }

        return string.Empty;
    }

    private DirectoryLoadResult LoadEntries(
        string directory,
        PathPickerMode mode,
        IReadOnlyList<string> extensions,
        bool showHidden,
        int maxItems,
        CancellationToken cancellationToken)
    {
        var entries = new List<FileSystemEntry>();
        string status = string.Empty;
        var normalizedDirectory = ResolveDirectory(directory);
        if (normalizedDirectory == null)
        {
            return new DirectoryLoadResult([], "The selected folder is not available.", false);
        }

        try
        {
            var directories = new List<FileSystemEntry>();
            foreach (var path in Directory.EnumerateDirectories(normalizedDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = CreateEntry(path, true);
                if (entry != null && IsEntryVisible(entry.Value, showHidden))
                {
                    directories.Add(entry.Value);
                }
            }

            directories.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));
            entries.AddRange(directories);

            if (mode != PathPickerMode.Folder)
            {
                var files = new List<FileSystemEntry>();
                foreach (var path in Directory.EnumerateFiles(normalizedDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var entry = CreateEntry(path, false);
                    if (entry != null &&
                        IsEntryVisible(entry.Value, showHidden) &&
                        MatchesFileExtensions(entry.Value, extensions))
                    {
                        files.Add(entry.Value);
                    }
                }

                files.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));
                entries.AddRange(files);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            status = "This folder cannot be opened.";
        }

        bool truncated = entries.Count > maxItems;
        if (truncated)
        {
            entries = entries.Take(maxItems).ToList();
        }

        return new DirectoryLoadResult(entries, status, truncated);
    }
    private VisualElement BuildSaveFileNameRow()
    {
        string fileName = GetInitialSaveFileName();

        _fileNameEntry = new Entry(fileName)
        {
            Height = 34,
            Background = EffectiveTheme.Colors.SurfaceHover,
            BorderBrush = EffectiveTheme.Colors.Border,
            BorderThickness = 1,
            TextColor = TextColor,
            Placeholder = "File name",
            PlaceholderColor = PlaceholderColor,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _fileNameEntry.OnCompletedHandler(ConfirmSelection);

        return new VStack()
            .Spacing(4)
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(
                new Label("File name")
                    .FontSize(11)
                    .Foreground(PlaceholderColor),
                _fileNameEntry);
    }

    private VisualElement BuildNavigationBar()
    {
        var parentButton = CreateToolbarButton(Icons.ArrowBack, NavigateToParent);
        var homeButton = CreateToolbarButton(Icons.Home, NavigateToHome);
        var refreshButton = CreateToolbarButton(Icons.Refresh, RefreshCurrentDirectory);
        var rootsButton = CreateToolbarButton(Icons.Folder, NavigateToFirstRoot);

        return new HStack()
            .Spacing(8)
            .Height(34)
            .Alignment(Alignment.Center)
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(parentButton, homeButton, rootsButton, refreshButton);
    }

    private VisualElement BuildDialogButtons()
    {
        var cancelButton = new Button
        {
            Text = "Cancel",
            Width = 100,
            Height = 36,
            Variant = ButtonVariant.Secondary,
            BorderThickness = 0,
            BorderRadius = new CornerRadius(6)
        };
        cancelButton.OnTapped(CancelSelection);

        var selectButton = new Button
        {
            Text = GetConfirmText(),
            Width = 130,
            Height = 36,
            Variant = ButtonVariant.Primary,
            BorderThickness = 0,
            BorderRadius = new CornerRadius(6)
        };
        selectButton.OnTapped(ConfirmSelection);

        return new HStack()
            .Spacing(10)
            .Height(36)
            .Alignment(Alignment.Center)
            .JustifyContent(JustifyContent.End)
            .HorizontalAlignment(HorizontalAlignment.Right)
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(cancelButton, selectButton);
    }

    private VisualElement CreateEntryRow()
    {
        return new ListViewItem
        {
            Height = 38,
            BorderRadius = new CornerRadius(7),
            Padding = new Thickness(10, 6, 10, 6),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    private void BindEntryRow(VisualElement element, FileSystemEntry entry, int index, bool isSelected)
    {
        if (element is not ListViewItem row)
        {
            return;
        }

        bool isSameEntry = _pathByVisibleEntryRow.TryGetValue(row, out var previousPath) &&
            string.Equals(previousPath, entry.Path, PathComparison);
        if (!isSameEntry)
        {
            if (!string.IsNullOrWhiteSpace(previousPath))
            {
                _visibleEntryRowsByPath.Remove(previousPath);
            }

            _pathByVisibleEntryRow[row] = entry.Path;
            _visibleEntryRowsByPath[entry.Path] = row;
            row.Content(CreateEntryRowContent(entry));
        }

        row.NormalBackground = isSelected || IsPendingSelection(entry.Path)
            ? GetSelectedEntryBackground()
            : Color.Transparent;
        row.HoverBackground = EffectiveTheme.Colors.SurfaceHover;
        row.PressedBackground = AccentColor.PrimaryColor.WithAlpha(0.45f);
        row.OnTap(() => HandleEntryTap(entry));
    }

    private VisualElement CreateEntryRowContent(FileSystemEntry entry)
    {
        var icon = new Icon(entry.IsDirectory ? Icons.Folder : Icons.File)
        {
            Width = 20,
            Height = 20,
            Color = entry.IsDirectory ? AccentColor : TextColor,
            VerticalAlignment = VerticalAlignment.Center
        };

        var name = new Label(entry.Name)
            .FontSize(14)
            .Foreground(TextColor)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center)
            .TextHorizontalAlignment(HorizontalAlignment.Left)
            .TextVerticalAlignment(VerticalAlignment.Center);

        var details = new Label(entry.IsDirectory ? "Folder" : FormatFileSize(entry.Size))
            .FontSize(12)
            .Foreground(PlaceholderColor)
            .Width(86)
            .TextHorizontalAlignment(HorizontalAlignment.Right)
            .VerticalAlignment(VerticalAlignment.Center)
            .TextVerticalAlignment(VerticalAlignment.Center);

        return new Grid()
            .Rows(GridLength.Star)
            .Columns(GridLength.Pixels(24), GridLength.Star, GridLength.Pixels(92))
            .ColumnSpacing(10)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .AddChild(icon, 0, 0)
            .AddChild(name, 0, 1)
            .AddChild(details, 0, 2);
    }

    private void HandleEntryTap(FileSystemEntry entry)
    {
        if (entry.IsDirectory)
        {
            NavigateTo(entry.Path);
            return;
        }

        if (Mode == PathPickerMode.Folder)
        {
            return;
        }

        _pendingSelection = entry.Path;
        _pendingSelectionIsDirectory = false;
        if (Mode == PathPickerMode.SaveFile && _fileNameEntry != null)
        {
            _fileNameEntry.Text = entry.Name;
        }
        UpdateSelectionRows();
    }

    private void UpdateSelectionRows()
    {
        foreach (var (path, row) in _visibleEntryRowsByPath.ToArray())
        {
            row.NormalBackground = IsPendingSelection(path)
                ? GetSelectedEntryBackground()
                : Color.Transparent;
            row.MarkNeedsPaint();
        }

        _entriesListView?.MarkNeedsPaint();
    }

    private Brush GetSelectedEntryBackground() => AccentColor.PrimaryColor.WithAlpha(0.35f);

    private void ConfirmSelection()
    {
        string? selected = Mode switch
        {
            PathPickerMode.File => _pendingSelection,
            PathPickerMode.Folder => CurrentDirectory,
            PathPickerMode.FileOrFolder => _pendingSelection ?? CurrentDirectory,
            PathPickerMode.SaveFile => BuildSaveFilePath(),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(selected))
        {
            if (Mode != PathPickerMode.SaveFile)
            {
                SetStatus("Select an item first.");
            }
            return;
        }

        SelectedPath = selected;
        ClosePicker();

        var confirmedHandler = _dialogConfirmed;
        ClearDialogCallbacks();
        confirmedHandler?.Invoke(selected);
    }

    private void CancelSelection()
    {
        ClosePicker();

        var canceledHandler = _dialogCanceled;
        ClearDialogCallbacks();
        SelectionCanceled?.Invoke();
        canceledHandler?.Invoke();
    }

    private void ClearDialogCallbacks()
    {
        _dialogConfirmed = null;
        _dialogCanceled = null;
    }

    private void NavigateToParent()
    {
        var parent = Directory.GetParent(CurrentDirectory);
        if (parent == null)
        {
            NavigateToFirstRoot();
            return;
        }

        NavigateTo(parent.FullName);
    }

    private void NavigateToHome()
    {
        NavigateTo(GetHomeDirectory());
    }

    private void NavigateToFirstRoot()
    {
        var root = GetRootDirectories().FirstOrDefault()
            ?? Path.GetPathRoot(CurrentDirectory)
            ?? CurrentDirectory;
        NavigateTo(root);
    }

    private void RefreshCurrentDirectory()
    {
        ClearLoadedEntries();
        RebuildDialogContent();
    }

    private ButtonIcon CreateToolbarButton(IconData icon, Action action)
    {
        var button = new ButtonIcon(icon)
            .Variant(ButtonVariant.Ghost)
            .Size(34)
            .VerticalAlignment(VerticalAlignment.Center)
            .IconSize(16)
            .IconColor(PlaceholderColor)
            .BorderRadius(new CornerRadius(7));
        button.OnTapped(action);
        return button;
    }

    private void RebuildDialogContent(bool preserveScrollOffset = false)
    {
        if (!_isOpen || _dialogCard == null)
        {
            return;
        }

        float preservedScrollOffset = preserveScrollOffset
            ? _entriesListView?.VerticalScrollOffset ?? 0f
            : 0f;

        _dialogCard.Content(BuildDialogContent(preservedScrollOffset, preserveScrollOffset));
        RefreshDialogLayout();
    }

    private void RefreshDialogLayout()
    {
        if (_dialogCard == null)
        {
            return;
        }

        if (_dialogCard.ComputedWidth > 0 && _dialogCard.ComputedHeight > 0)
        {
            float measureWidth = float.IsNaN(_dialogCard.LastMeasuredAvailableWidth)
                ? _dialogCard.ComputedWidth
                : _dialogCard.LastMeasuredAvailableWidth;
            float measureHeight = float.IsNaN(_dialogCard.LastMeasuredAvailableHeight)
                ? _dialogCard.ComputedHeight
                : _dialogCard.LastMeasuredAvailableHeight;

            _dialogCard.ForceMeasure(measureWidth, measureHeight);
            _dialogCard.ForceArrange(
                _dialogCard.ComputedX,
                _dialogCard.ComputedY,
                _dialogCard.ComputedWidth,
                _dialogCard.ComputedHeight);
        }
        else
        {
            _dialogCard.InvalidateMeasure();
            _dialogOverlay?.InvalidateMeasure();
        }

        _dialogCard.MarkNeedsPaint();
        _dialogOverlay?.MarkNeedsPaint();
        (UIApplication.Current?.Tree ?? UITree.Current)?.MarkNeedsRender();
    }

    private void SetStatus(string text)
    {
        if (_statusLabel == null)
        {
            return;
        }

        _statusLabel.Text = text;
        _statusLabel.Foreground = PlaceholderColor;
    }

    private void UpdateTriggerContent()
    {
        if (_modeIcon != null)
        {
            _modeIcon.IconData = GetModeIcon();
            _modeIcon.Color = AccentColor;
        }

        UpdateSelectedPathLabel();
    }

    private void UpdateSelectedPathLabel()
    {
        if (_selectedPathLabel == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            _selectedPathLabel.Text(Placeholder).Foreground(PlaceholderColor);
        }
        else
        {
            _selectedPathLabel.Text(SelectedPath).Foreground(TextColor);
        }
    }

    private IconData GetModeIcon()
    {
        return Mode is PathPickerMode.File or PathPickerMode.SaveFile ? Icons.File : Icons.Folder;
    }

    private string GetDialogTitle()
    {
        if (!string.IsNullOrWhiteSpace(DialogTitle))
        {
            return DialogTitle;
        }

        return Mode switch
        {
            PathPickerMode.File => "Select a file",
            PathPickerMode.Folder => "Select a folder",
            PathPickerMode.FileOrFolder => "Select a file or folder",
            PathPickerMode.SaveFile => "Save file",
            _ => "Select path"
        };
    }

    private string GetConfirmText()
    {
        return Mode switch
        {
            PathPickerMode.File => "Select file",
            PathPickerMode.Folder => "Select folder",
            PathPickerMode.FileOrFolder => _pendingSelection == null || _pendingSelectionIsDirectory ? "Select folder" : "Select file",
            PathPickerMode.SaveFile => "Save",
            _ => "Select"
        };
    }

    private string? BuildSaveFilePath()
    {
        string fileName = _fileNameEntry?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            SetStatus("Enter a file name.");
            return null;
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            SetStatus("The file name contains invalid characters.");
            return null;
        }

        fileName = EnsureConfiguredExtension(fileName);
        string selectedPath = Path.Combine(CurrentDirectory, fileName);
        if (File.Exists(selectedPath) && SaveConflictBehavior == SaveFileConflictBehavior.Reject)
        {
            SetStatus("A file with this name already exists.");
            return null;
        }

        return selectedPath;
    }

    private string GetInitialSaveFileName()
    {
        if (!string.IsNullOrWhiteSpace(_fileNameEntry?.Text))
        {
            return _fileNameEntry.Text;
        }

        if (!string.IsNullOrWhiteSpace(_pendingSelection) && File.Exists(_pendingSelection))
        {
            return Path.GetFileName(_pendingSelection);
        }

        if (!string.IsNullOrWhiteSpace(SelectedPath))
        {
            return Path.GetFileName(SelectedPath);
        }

        return EnsureConfiguredExtension(DefaultFileName);
    }

    private string EnsureConfiguredExtension(string fileName)
    {
        var extension = FileExtensions
            .Select(NormalizeExtension)
            .FirstOrDefault(ext => ext != "*");

        if (string.IsNullOrWhiteSpace(extension) || string.Equals(Path.GetExtension(fileName), extension, StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }

        return Path.ChangeExtension(fileName, extension);
    }

    private bool IsPendingSelection(string path)
    {
        return !string.IsNullOrEmpty(_pendingSelection) &&
            string.Equals(_pendingSelection, path, PathComparison);
    }

    private IEnumerable<FileSystemEntry> GetEntries(string directory, out string status)
    {
        status = string.Empty;
        var normalizedDirectory = ResolveDirectory(directory);
        if (normalizedDirectory == null)
        {
            status = "The selected folder is not available.";
            return [];
        }

        try
        {
            var directories = Directory.EnumerateDirectories(normalizedDirectory)
                .Select(path => CreateEntry(path, true))
                .Where(entry => entry != null)
                .Select(entry => entry!.Value)
                .Where(IsEntryVisible)
                .OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase);

            if (Mode == PathPickerMode.Folder)
            {
                return directories;
            }

            var files = Directory.EnumerateFiles(normalizedDirectory)
                .Select(path => CreateEntry(path, false))
                .Where(entry => entry != null)
                .Select(entry => entry!.Value)
                .Where(IsEntryVisible)
                .Where(MatchesFileExtensions)
                .OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase);

            return directories.Concat(files);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            status = "This folder cannot be opened.";
            return [];
        }
    }

    private FileSystemEntry? CreateEntry(string path, bool isDirectory)
    {
        try
        {
            var name = GetDisplayName(path);
            var attributes = File.GetAttributes(path);
            long size = isDirectory ? 0 : new FileInfo(path).Length;
            return new FileSystemEntry(path, name, isDirectory, size, attributes);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            return null;
        }
    }

    private bool IsEntryVisible(FileSystemEntry entry)
    {
        return IsEntryVisible(entry, ShowHidden);
    }

    private static bool IsEntryVisible(FileSystemEntry entry, bool showHidden)
    {
        if (showHidden)
        {
            return true;
        }

        if ((entry.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
        {
            return false;
        }

        return !entry.Name.StartsWith(".", StringComparison.Ordinal);
    }

    private bool MatchesFileExtensions(FileSystemEntry entry)
    {
        return MatchesFileExtensions(entry, FileExtensions.Select(NormalizeExtension).ToList());
    }

    private static bool MatchesFileExtensions(FileSystemEntry entry, IReadOnlyList<string> extensions)
    {
        if (entry.IsDirectory || extensions.Count == 0)
        {
            return true;
        }

        string extension = Path.GetExtension(entry.Path);
        return extensions.Any(filter =>
            filter == "*" || string.Equals(extension, filter, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension) || extension == "*")
        {
            return "*";
        }

        return extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : "." + extension;
    }

    private static string ResolveInitialDirectory(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            try
            {
                if (File.Exists(requested))
                {
                    var parent = Path.GetDirectoryName(Path.GetFullPath(requested));
                    if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
                    {
                        return parent;
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException or System.Security.SecurityException)
            {
            }

            var fromRequest = ResolveDirectory(requested);
            if (fromRequest != null)
            {
                return fromRequest;
            }
        }

        foreach (var candidate in GetInitialDirectoryCandidates())
        {
            var resolved = ResolveDirectory(candidate);
            if (resolved != null)
            {
                return resolved;
            }
        }

        var currentDirectory = Directory.GetCurrentDirectory();
        return Path.GetPathRoot(currentDirectory) ?? currentDirectory;
    }

    private static string? ResolveDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            if (Directory.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return null;
        }

        return null;
    }

    private static string GetHomeDirectory()
    {
        return GetInitialDirectoryCandidates().FirstOrDefault() ?? Directory.GetCurrentDirectory();
    }

    private static IEnumerable<string> GetInitialDirectoryCandidates()
    {
        if (PlatformDetector.IsAndroid)
        {
            foreach (var directory in GetAndroidDirectoryCandidates())
            {
                if (IsUsableDirectory(directory))
                {
                    yield return directory;
                }
            }
        }

        foreach (var specialFolder in new[]
        {
            Environment.SpecialFolder.MyDocuments,
            Environment.SpecialFolder.UserProfile,
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolder.ApplicationData
        })
        {
            var directory = Environment.GetFolderPath(specialFolder);
            if (IsUsableDirectory(directory))
            {
                yield return directory;
            }
        }

        var currentDirectory = Directory.GetCurrentDirectory();
        if (IsUsableDirectory(currentDirectory))
        {
            yield return currentDirectory;
        }
    }

    private static IEnumerable<string> GetRootDirectories()
    {
        if (PlatformDetector.IsAndroid)
        {
            foreach (var directory in GetAndroidDirectoryCandidates())
            {
                if (IsUsableDirectory(directory))
                {
                    yield return directory;
                }
            }
        }

        foreach (var drive in Directory.GetLogicalDrives())
        {
            if (IsUsableDirectory(drive))
            {
                yield return drive;
            }
        }
    }

    private static IEnumerable<string> GetAndroidDirectoryCandidates()
    {
        yield return "/storage/emulated/0";
        yield return "/sdcard";
        yield return "/storage/emulated/0/Download";
        yield return "/storage/emulated/0/Documents";
        yield return "/storage/emulated/0/Pictures";
        yield return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    private static bool IsUsableDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        try
        {
            if (!Directory.Exists(directory))
            {
                return false;
            }

            using var enumerator = Directory.EnumerateFileSystemEntries(directory).GetEnumerator();
            _ = enumerator.MoveNext();
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private static string GetDisplayName(string path)
    {
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} B"
            : $"{value:0.#} {units[unit]}";
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    bool IGlobalPointerHandler.HandleGlobalPointer(Vector2 position, VisualElement? hitElement)
    {
        if (!_isOpen || _dialogCard == null)
        {
            return false;
        }

        var current = hitElement;
        while (current != null)
        {
            if (current == this || current == _dialogCard)
            {
                return true;
            }

            current = current.Parent;
        }

        bool insideCard = position.X >= _dialogCard.ComputedX &&
                          position.X <= _dialogCard.ComputedX + _dialogCard.ComputedWidth &&
                          position.Y >= _dialogCard.ComputedY &&
                          position.Y <= _dialogCard.ComputedY + _dialogCard.ComputedHeight;

        if (insideCard)
        {
            return true;
        }

        CancelSelection();
        return false;
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 320;
        float measuredHeight = Height > 0 ? Height : 44;

        _pickerButton?.MeasureUpdate(measuredWidth, measuredHeight);

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _pickerButton?.ArrangeUpdate(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
    }

    void IPointerHandler.OnPointerPressed(PointerEventArgs e)
    {
        _isPressed = true;
    }

    void IPointerHandler.OnPointerReleased(PointerEventArgs e)
    {
        if (!_isPressed)
        {
            return;
        }

        _isPressed = false;

        bool inside = e.Position.X >= ComputedX &&
                      e.Position.X <= ComputedX + ComputedWidth &&
                      e.Position.Y >= ComputedY &&
                      e.Position.Y <= ComputedY + ComputedHeight;

        if (inside)
        {
            TogglePicker();
        }
    }

    private readonly record struct FileSystemEntry(
        string Path,
        string Name,
        bool IsDirectory,
        long Size,
        FileAttributes Attributes);

    private sealed record DirectoryLoadResult(
        IReadOnlyList<FileSystemEntry> Entries,
        string Status,
        bool Truncated);

    private sealed class DialogOverlayFrame : Frame, IPointerHandler
    {
    }
}

/// <summary>
/// Convenience picker configured for file selection.
/// </summary>
public class FilePicker : PathPicker
{
    public FilePicker()
    {
        Mode = PathPickerMode.File;
        Placeholder = "Select file...";
    }

    public static FilePicker ShowDialog(Action<string> onConfirm, Action? onCancel = null, Action<FilePicker>? configure = null)
    {
        var picker = new FilePicker();
        configure?.Invoke(picker);
        picker.OpenDialog(onConfirm, onCancel);
        return picker;
    }

}

/// <summary>
/// Convenience picker configured for folder selection.
/// </summary>
public class FolderPicker : PathPicker
{
    public FolderPicker()
    {
        Mode = PathPickerMode.Folder;
        Placeholder = "Select folder...";
    }

    public static FolderPicker ShowDialog(Action<string> onConfirm, Action? onCancel = null, Action<FolderPicker>? configure = null)
    {
        var picker = new FolderPicker();
        configure?.Invoke(picker);
        picker.OpenDialog(onConfirm, onCancel);
        return picker;
    }

}

/// <summary>
/// Convenience picker configured for save-file selection.
/// </summary>
public class SaveFilePicker : PathPicker
{
    public SaveFilePicker()
    {
        Mode = PathPickerMode.SaveFile;
        Placeholder = "Save file...";
    }

    public static SaveFilePicker ShowDialog(Action<string> onConfirm, Action? onCancel = null, Action<SaveFilePicker>? configure = null)
    {
        var picker = new SaveFilePicker();
        configure?.Invoke(picker);
        picker.OpenDialog(onConfirm, onCancel);
        return picker;
    }
}
