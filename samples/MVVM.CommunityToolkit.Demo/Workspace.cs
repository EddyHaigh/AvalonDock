using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

using AvalonDock.Themes;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Win32;

using MVVM.CommunityToolkit.Demo.ViewModels;

namespace MVVM.CommunityToolkit.Demo;

/// <summary>
/// Represents the workspace in the application.
/// </summary>
internal partial class Workspace : ObservableObject
{
    /// <summary>
    /// The singleton instance of the Workspace class.
    /// </summary>
    private static readonly Workspace _this = new Workspace();

    /// <summary>
    /// The collection of file view models in the workspace.
    /// </summary>
    private readonly ObservableCollection<FileViewModel> _files = new ObservableCollection<FileViewModel>();

    /// <summary>
    /// The currently active document in the workspace.
    /// </summary>
    private FileViewModel? _activeDocument;

    /// <summary>
    /// The view model for file statistics.
    /// </summary>
    private FileStatsViewModel? _fileStats;

    /// <summary>
    /// The read-only collection of file view models in the workspace.
    /// </summary>
    private ReadOnlyObservableCollection<FileViewModel>? _readOnlyFiles;

    /// <summary>
    /// The array of tool view models in the workspace.
    /// </summary>
    private ToolViewModel[] _tools = Array.Empty<ToolViewModel>();

    /// <summary>
    /// The selected theme in the workspace.
    /// </summary>
    [ObservableProperty]
    private ThemeSelector _selectedTheme;

    /// <summary>
    /// Initializes a new instance of the Workspace class.
    /// </summary>
    protected Workspace()
    {
        Themes =
        [
            new ThemeSelector(nameof(GenericTheme), new GenericTheme()),
            new ThemeSelector(nameof(AeroTheme), new AeroTheme()),
            new ThemeSelector(nameof(ExpressionDarkTheme), new ExpressionDarkTheme()),
            new ThemeSelector(nameof(ExpressionLightTheme), new ExpressionLightTheme()),
            new ThemeSelector(nameof(MetroTheme), new MetroTheme()),
            new ThemeSelector(nameof(VS2010Theme), new VS2010Theme()),
            new ThemeSelector(nameof(Vs2013BlueTheme), new Vs2013BlueTheme()),
            new ThemeSelector(nameof(Vs2013DarkTheme), new Vs2013DarkTheme()),
            new ThemeSelector(nameof(Vs2013LightTheme), new Vs2013LightTheme())
        ];
        _selectedTheme = Themes[0];

        NewCommand = new RelayCommand(OnNew);
        OpenCommand = new RelayCommand(OnOpen);
    }

    /// <summary>
    /// Event that is raised when the active document in the workspace changes.
    /// </summary>
    public event EventHandler ActiveDocumentChanged = default!;

    /// <summary>
    /// Gets the singleton instance of the Workspace class.
    /// </summary>
    public static Workspace This => _this;

    /// <summary>
    /// Gets or sets the currently active document in the workspace.
    /// </summary>
    public FileViewModel? ActiveDocument
    {
        get => _activeDocument;
        set
        {
            if (_activeDocument != value)
            {
                _activeDocument = value;
                OnPropertyChanged(nameof(ActiveDocument));
                ActiveDocumentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets the read-only collection of file view models in the workspace.
    /// </summary>
    public ReadOnlyObservableCollection<FileViewModel> Files
    {
        get
        {
            _readOnlyFiles ??= new ReadOnlyObservableCollection<FileViewModel>(_files);
            return _readOnlyFiles;
        }
    }

    /// <summary>
    /// Gets the view model for file statistics.
    /// </summary>
    public FileStatsViewModel FileStats => new FileStatsViewModel();

    /// <summary>
    /// Gets the command for creating a new document.
    /// </summary>
    public ICommand NewCommand { get; init; }

    /// <summary>
    /// Gets the command for opening an existing document.
    /// </summary>
    public ICommand OpenCommand { get; init; }

    /// <summary>
    /// Gets or sets the array of available themes in the workspace.
    /// </summary>
    public ThemeSelector[] Themes { get; init; }

    /// <summary>
    /// Gets the collection of tool view models in the workspace.
    /// </summary>
    public IEnumerable<ToolViewModel> Tools
    {
        get
        {
            if (_tools.Length == 0)
            {
                _tools = [FileStats];
            }

            return _tools;
        }
    }
    /// <summary>
    /// Closes the specified file in the workspace.
    /// </summary>
    /// <param name="fileToClose">The file view model to close.</param>
    internal void Close(FileViewModel fileToClose)
    {
        if (fileToClose.IsDirty)
        {
            var res = MessageBox.Show($"Save changes for file '{fileToClose.FileName}'?", "AvalonDock Test App", MessageBoxButton.YesNoCancel);
            if (res == MessageBoxResult.Cancel)
            {
                return;
            }

            if (res == MessageBoxResult.Yes)
            {
                Save(fileToClose);
            }
        }

        _files.Remove(fileToClose);
    }

    /// <summary>
    /// Opens the specified file in the workspace.
    /// </summary>
    /// <param name="filepath">The path of the file to open.</param>
    /// <returns>The file view model representing the opened file.</returns>
    internal FileViewModel Open(string filepath)
    {
        var fileViewModel = _files.FirstOrDefault(fm => fm.FilePath == filepath);
        if (fileViewModel != null)
        {
            return fileViewModel;
        }

        fileViewModel = new FileViewModel(filepath);
        _files.Add(fileViewModel);
        return fileViewModel;
    }

    /// <summary>
    /// Saves the specified file in the workspace.
    /// </summary>
    /// <param name="fileToSave">The file view model to save.</param>
    /// <param name="saveAsFlag">A flag indicating whether to save the file with a new name.</param>
    internal void Save(FileViewModel fileToSave, bool saveAsFlag = false)
    {
        if (string.IsNullOrWhiteSpace(fileToSave.FilePath) || saveAsFlag)
        {
            var dlg = new SaveFileDialog()
            {
                DefaultExt = "xml",
            };

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                fileToSave.FilePath = dlg.SafeFileName;
            }
        }

        if (string.IsNullOrWhiteSpace(fileToSave.FilePath))
        {
            return;
        }

        File.WriteAllText(fileToSave.FilePath, fileToSave.TextContent);
        if (ActiveDocument is not null)
        {
            ActiveDocument.IsDirty = false;
        }
    }

    /// <summary>
    /// Handles the action when the "New" command is executed.
    /// </summary>
    private void OnNew()
    {
        _files.Add(new FileViewModel("Tab"));
        ActiveDocument = _files[^1];
    }

    /// <summary>
    /// Handles the action when the "Open" command is executed.
    /// </summary>
    private void OnOpen()
    {
        var dlg = new OpenFileDialog();
        if (dlg.ShowDialog().GetValueOrDefault())
        {
            var fileViewModel = Open(dlg.FileName);
            ActiveDocument = fileViewModel;
        }
    }

    /// <summary>
    /// Represents a theme selector in the workspace.
    /// </summary>
    public record class ThemeSelector(string Name, Theme Theme);
}
