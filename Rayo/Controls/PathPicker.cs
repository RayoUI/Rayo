namespace Rayo.Controls;

using Rayo.Core;
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
using static Rayo.Core.UIHelpers;

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
    private Frame? _dialogOverlay;
    private Frame? _dialogCard;
    private Label? _currentDirectoryLabel;
    private Label? _statusLabel;
    private VStack? _itemsStack;
    private ScrollView? _listScrollView;
    private Entry? _fileNameEntry;
    private string? _pendingSelection;
    private bool _pendingSelectionIsDirectory;
    private Action<string>? _dialogConfirmed;
    private Action? _dialogCanceled;
    private bool _isOpen;
    private bool _isPressed;

    public PathPicker()
    {
        Background = new Color(40, 40, 45);
        Width = 320;
        Height = 44;
        BorderBrush = new Color(100, 100, 100);
        BorderThickness = 1;
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
        set => this.SetProperty(ref field, value ?? new List<string>(), () => RebuildDialogContent());
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
        set => this.SetProperty(ref field, value, () => RebuildDialogContent());
    }
    #endregion

    #region MaxItems
    [LayoutProperty]
    public int MaxItems
    {
        get => field;
        set => this.SetProperty(ref field, Math.Max(1, value), () => RebuildDialogContent());
    } = 500;
    #endregion

    #region Styling
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateSelectedPathLabel);
    } = Color.White;

    [PaintProperty]
    public Brush PlaceholderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateSelectedPathLabel);
    } = new Color(128, 128, 128);

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
    } = ColorDefault.Primary;

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
        _dialogOverlay = BuildDialogOverlay();

        OverlayManager.AddOverlay(_dialogOverlay);
        OverlayManager.EventManager?.RegisterGlobalPointerHandler(this);
    }

    public void ClosePicker()
    {
        if (!_isOpen)
        {
            return;
        }

        if (_dialogOverlay != null)
        {
            OverlayManager.RemoveOverlay(_dialogOverlay);
        }

        _isOpen = false;
        _dialogOverlay = null;
        _dialogCard = null;
        _itemsStack = null;
        _listScrollView = null;
        _fileNameEntry = null;
        _currentDirectoryLabel = null;
        _statusLabel = null;
        _pendingSelection = null;
        _pendingSelectionIsDirectory = false;

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

        var browseIcon = new Icon(Icons.Search)
        {
            Width = 18,
            Height = 18,
            Color = PlaceholderColor
        };
        browseIcon.SetInputTransparent(true);

        var content = new HStack()
            .Spacing(10)
            .Alignment(Alignment.Center)
            .JustifyContent(JustifyContent.SpaceBetween)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
        content.AddChild(_modeIcon);
        content.AddChild(_selectedPathLabel);
        content.AddChild(browseIcon);

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
            .Background(new Color(30, 30, 35))
            .BorderBrush(new Color(50, 55, 65))
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

    private VisualElement BuildDialogContent()
    {
        var title = new Label(GetDialogTitle())
            .FontSize(18)
            .Height(28)
            .Foreground(Color.White);

        _currentDirectoryLabel = new Label(CurrentDirectory)
            .FontSize(13)
            .Foreground(ColorDefault.Secondary)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var pathFrame = new Frame()
            .Height(42)
            .Background(new Color(37, 39, 48))
            .BorderRadius(new CornerRadius(10))
            .Padding(new Thickness(12, 8, 12, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(_currentDirectoryLabel);

        _itemsStack = new VStack()
            .Spacing(4)
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var status = BuildItems();
        _statusLabel = new Label(status)
            .FontSize(12)
            .Foreground(string.IsNullOrEmpty(status) ? Color.Transparent : ColorDefault.Secondary);

        _listScrollView = new ScrollView()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(_itemsStack);

        var listFrame = new Frame()
            .Background(new Color(34, 36, 44))
            .BorderBrush(new Color(50, 55, 65))
            .BorderThickness(1)
            .BorderRadius(new CornerRadius(12))
            .Padding(new Thickness(8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(_listScrollView);
        listFrame.ClipToBounds = true;

        if (Mode == PathPickerMode.SaveFile)
        {
            return new Grid()
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
                .AddChild(listFrame, 4, 0)
                .AddChild(_statusLabel, 5, 0)
                .AddChild(BuildDialogButtons(), 6, 0);
        }

        return new Grid()
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
            .AddChild(listFrame, 3, 0)
            .AddChild(_statusLabel, 4, 0)
            .AddChild(BuildDialogButtons(), 5, 0);
    }

    private VisualElement BuildSaveFileNameRow()
    {
        string fileName = GetInitialSaveFileName();

        _fileNameEntry = new Entry(fileName)
        {
            Height = 34,
            Background = new Color(37, 39, 48),
            BorderBrush = new Color(50, 55, 65),
            BorderThickness = 1,
            TextColor = Color.White,
            Placeholder = "File name",
            PlaceholderColor = ColorDefault.Secondary,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _fileNameEntry.OnCompletedHandler(ConfirmSelection);

        return new VStack()
            .Spacing(4)
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(
                new Label("File name")
                    .FontSize(11)
                    .Foreground(ColorDefault.Secondary),
                _fileNameEntry);
    }

    private VisualElement BuildNavigationBar()
    {
        var parentButton = CreateToolbarButton(Icons.ChevronUp, NavigateToParent);
        var homeButton = CreateToolbarButton(Icons.Home, NavigateToHome);
        var refreshButton = CreateToolbarButton(Icons.Refresh, () => RebuildDialogContent());
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
            Background = new Color(45, 45, 52),
            HoverBackground = new Color(55, 55, 62),
            BorderThickness = 0,
            BorderRadius = new CornerRadius(6)
        };
        cancelButton.OnTapped(CancelSelection);

        var selectButton = new Button
        {
            Text = GetConfirmText(),
            Width = 130,
            Height = 36,
            Background = ColorDefault.Primary,
            HoverBackground = ColorDefault.Info,
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

    private string BuildItems()
    {
        if (_itemsStack == null)
        {
            return string.Empty;
        }

        _itemsStack.ClearChildren();

        var entries = GetEntries(CurrentDirectory, out var status).Take(MaxItems + 1).ToList();
        bool truncated = entries.Count > MaxItems;
        if (truncated)
        {
            entries = entries.Take(MaxItems).ToList();
        }

        if (entries.Count == 0 && string.IsNullOrEmpty(status))
        {
            status = Mode == PathPickerMode.Folder
                ? "This folder does not contain visible folders."
                : "This folder does not contain matching files.";
        }

        foreach (var entry in entries)
        {
            _itemsStack.AddChild(CreateEntryRow(entry));
        }

        if (truncated)
        {
            status = $"Showing the first {MaxItems} items.";
        }

        return status;
    }

    private VisualElement CreateEntryRow(FileSystemEntry entry)
    {
        var icon = new Icon(entry.IsDirectory ? Icons.Folder : Icons.File)
        {
            Width = 20,
            Height = 20,
            Color = entry.IsDirectory ? new Color(96, 165, 250) : new Color(203, 213, 225),
            VerticalAlignment = VerticalAlignment.Center
        };

        var name = new Label(entry.Name)
            .FontSize(14)
            .Foreground(Color.White)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center)
            .TextHorizontalAlignment(HorizontalAlignment.Left)
            .TextVerticalAlignment(VerticalAlignment.Center);

        var details = new Label(entry.IsDirectory ? "Folder" : FormatFileSize(entry.Size))
            .FontSize(12)
            .Foreground(ColorDefault.Secondary)
            .Width(86)
            .TextHorizontalAlignment(HorizontalAlignment.Right)
            .VerticalAlignment(VerticalAlignment.Center)
            .TextVerticalAlignment(VerticalAlignment.Center);

        var rowContent = new Grid()
            .Rows(GridLength.Star)
            .Columns(GridLength.Pixels(24), GridLength.Star, GridLength.Pixels(92))
            .ColumnSpacing(10)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .AddChild(icon, 0, 0)
            .AddChild(name, 0, 1)
            .AddChild(details, 0, 2);

        var row = new PickerRow(() => HandleEntryTap(entry))
        {
            Height = 38,
            BorderRadius = new CornerRadius(7),
            Padding = new Thickness(10, 6, 10, 6),
            NormalBackground = IsPendingSelection(entry.Path)
                ? new Color(59, 130, 246, 0.35f)
                : Color.Transparent,
            HoverBackground = new Color(255, 255, 255, 0.08f),
            PressedBackground = new Color(59, 130, 246, 0.45f),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        row.Content(rowContent);

        return row;
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
        RebuildDialogContent(preserveScrollOffset: true);
    }

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
        var root = Directory.GetLogicalDrives().FirstOrDefault()
            ?? Path.GetPathRoot(CurrentDirectory)
            ?? CurrentDirectory;
        NavigateTo(root);
    }

    private ButtonIcon CreateToolbarButton(IconData icon, Action action)
    {
        var button = new ButtonIcon(icon)
            .Size(34)
            .VerticalAlignment(VerticalAlignment.Center)
            .IconSize(16)
            .IconColor(ColorDefault.Secondary)
            .Background(new Color(45, 45, 52))
            .HoverBackground(new Color(55, 55, 62))
            .PressedBackground(new Color(35, 35, 42))
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
            ? _listScrollView?.VerticalScrollOffset ?? 0f
            : 0f;

        _dialogCard.Content(BuildDialogContent());
        RefreshDialogLayout();

        if (preserveScrollOffset && _listScrollView != null)
        {
            _listScrollView.VerticalScrollOffset = preservedScrollOffset;
            _listScrollView.MarkNeedsPaint();
            (UIApplication.Current?.Tree ?? UITree.Current)?.MarkNeedsRender();
        }
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
        _statusLabel.Foreground = ColorDefault.Secondary;
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
        if (ShowHidden)
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
        if (entry.IsDirectory || FileExtensions.Count == 0)
        {
            return true;
        }

        string extension = Path.GetExtension(entry.Path);
        return FileExtensions.Any(filter =>
        {
            var normalized = NormalizeExtension(filter);
            return normalized == "*" || string.Equals(extension, normalized, StringComparison.OrdinalIgnoreCase);
        });
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

        return ResolveDirectory(GetHomeDirectory())
            ?? ResolveDirectory(Directory.GetCurrentDirectory())
            ?? Path.GetPathRoot(Directory.GetCurrentDirectory())
            ?? Directory.GetCurrentDirectory();
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
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents) && Directory.Exists(documents))
        {
            return documents;
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile) && Directory.Exists(profile))
        {
            return profile;
        }

        return Directory.GetCurrentDirectory();
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

    private sealed class DialogOverlayFrame : Frame, IPointerHandler
    {
    }

    private sealed class PickerRow : Frame, IPointerHandler
    {
        private readonly Action _onTap;
        private bool _isPressed;
        private Vector2 _pressPosition;

        public PickerRow(Action onTap)
        {
            _onTap = onTap;
        }

        public Brush NormalBackground
        {
            get => field;
            set
            {
                field = value;
                Background = value;
            }
        } = Color.Transparent;

        public Brush HoverBackground { get; set; } = new Color(255, 255, 255, 0.08f);

        public Brush PressedBackground { get; set; } = new Color(59, 130, 246, 0.45f);

        public void OnPointerEntered(PointerEventArgs e)
        {
            if (!_isPressed)
            {
                Background = HoverBackground;
            }
        }

        public void OnPointerExited(PointerEventArgs e)
        {
            if (!_isPressed)
            {
                Background = NormalBackground;
            }
        }

        public void OnPointerPressed(PointerEventArgs e)
        {
            _isPressed = true;
            _pressPosition = e.Position;
            Background = PressedBackground;
        }

        public void OnPointerReleased(PointerEventArgs e)
        {
            if (!_isPressed)
            {
                return;
            }

            _isPressed = false;
            var delta = e.Position - _pressPosition;
            bool isTap = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y) < 15f;
            bool inside = e.Position.X >= ComputedX &&
                          e.Position.X <= ComputedX + ComputedWidth &&
                          e.Position.Y >= ComputedY &&
                          e.Position.Y <= ComputedY + ComputedHeight;

            Background = NormalBackground;

            if (isTap && inside)
            {
                _onTap();
            }
        }
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
