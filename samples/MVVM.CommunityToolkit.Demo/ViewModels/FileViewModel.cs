using System.IO;
using System.Windows.Media;

using CommunityToolkit.Mvvm.Input;

using MVVM.CommunityToolkit.Demo.ViewModels.Abstractions;

namespace MVVM.CommunityToolkit.Demo.ViewModels;

/// <summary>
/// Represents a view model for a file.
/// </summary>
public class FileViewModel : PaneViewModel
{
    private string _filePath = string.Empty;
    private string _textContent = string.Empty;
    private bool _isDirty = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileViewModel"/> class with the specified file path.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    public FileViewModel(string filePath)
    {
        FilePath = filePath;
        Title = FileName;

        var imageSourceConverter = new ImageSourceConverter();
        //Set the icon only for open documents (just a test)
        IconSource = imageSourceConverter.ConvertFromInvariantString(@"pack://application:,,/Assets/document.png") as ImageSource;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileViewModel"/> class.
    /// </summary>
    public FileViewModel()
    {
        IsDirty = true;
        Title = FileName;
    }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                SetProperty(ref _filePath, value);
                OnPropertyChanged(FileName);
                OnPropertyChanged(nameof(Title));

                if (File.Exists(_filePath))
                {
                    _textContent = File.ReadAllText(_filePath);
                    ContentId = _filePath;
                }
            }
        }
    }

    /// <summary>
    /// Gets the name of the file.
    /// </summary>
    public string FileName
    {
        get
        {
            string fileName = FilePath == null ? "Noname" : Path.GetFileName(FilePath);
            string dirtyIndicator = IsDirty ? "*" : string.Empty;
            return $"{fileName}{dirtyIndicator}";
        }
    }

    /// <summary>
    /// Gets or sets the text content of the file.
    /// </summary>
    public string TextContent
    {
        get => _textContent;
        set
        {
            if (_textContent != value)
            {
                _textContent = value;
                OnPropertyChanged(nameof(TextContent));
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the file is dirty.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(FileName));
            }
        }
    }

    /// <summary>
    /// Gets the command to save the file.
    /// </summary>
    public RelayCommand SaveCommand => new(() => Workspace.This.Save(this, false), () => IsDirty);

    /// <summary>
    /// Gets the command to save the file as a new file.
    /// </summary>
    public RelayCommand SaveAsCommand => new(() => Workspace.This.Save(this, true), () => IsDirty);

    /// <summary>
    /// Gets the command to close the file.
    /// </summary>
    public RelayCommand CloseCommand => new(() => Workspace.This.Close(this), static () => true);
}
