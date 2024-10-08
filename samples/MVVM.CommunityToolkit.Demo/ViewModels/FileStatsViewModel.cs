using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace MVVM.CommunityToolkit.Demo.ViewModels;

/// <summary>
/// View model for displaying file statistics.
/// </summary>
public class FileStatsViewModel : ToolViewModel
{
    /// <summary>
    /// The content ID of the file stats tool.
    /// </summary>
    public const string ToolContentId = "FileStatsTool";

    private string _fileName = string.Empty;
    private string _filePath = string.Empty;
    private long _fileSize;
    private DateTime _lastModified;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStatsViewModel"/> class.
    /// </summary>
    public FileStatsViewModel()
        : base("File Stats")
    {
        Workspace.This.ActiveDocumentChanged += new EventHandler(OnActiveDocumentChanged);
        ContentId = ToolContentId;

        BitmapImage bi = new();
        bi.BeginInit();
        bi.UriSource = new Uri(GetResourceUri("Assets/property-blue.png"));
        bi.EndInit();
        IconSource = bi;
    }

    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string FileName
    {
        get => _fileName;
        protected set
        {
            if (!_fileName.Equals(value))
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }
    }

    /// <summary>
    /// Gets or sets the path of the file.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        protected set
        {
            if (!_filePath.Equals(value))
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }
    }

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long FileSize
    {
        get => _fileSize;
        protected set
        {
            if (_fileSize != value)
            {
                _fileSize = value;
                OnPropertyChanged(nameof(FileSize));
            }
        }
    }

    /// <summary>
    /// Gets or sets the last modified date and time of the file.
    /// </summary>
    public DateTime LastModified
    {
        get => _lastModified;
        protected set
        {
            if (_lastModified != value)
            {
                _lastModified = value;
                OnPropertyChanged(nameof(LastModified));
            }
        }
    }

    private static string GetResourceUri(string relativePath)
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        return $"pack://application:,,,/{assemblyName};component/{relativePath}";
    }

    private void OnActiveDocumentChanged(object? sender, EventArgs e)
    {
        if (Workspace.This.ActiveDocument != null &&
            Workspace.This.ActiveDocument.FilePath != null &&
            File.Exists(Workspace.This.ActiveDocument.FilePath))
        {
            var fi = new FileInfo(Workspace.This.ActiveDocument.FilePath);
            FileSize = fi.Length;
            LastModified = fi.LastWriteTime;
            FileName = fi.Name;
            FilePath = fi.Directory?.FullName ?? string.Empty;
        }
        else
        {
            FileSize = 0;
            LastModified = DateTime.MinValue;
            FileName = string.Empty;
            FilePath = string.Empty;
        }
    }
}
