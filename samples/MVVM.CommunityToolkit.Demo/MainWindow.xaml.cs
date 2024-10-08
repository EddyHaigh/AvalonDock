using System.IO;
using System.Windows;
using System.Windows.Input;

using AvalonDock.Layout.Serialization;

using CommunityToolkit.Mvvm.Input;

using MVVM.CommunityToolkit.Demo.ViewModels;

namespace MVVM.CommunityToolkit.Demo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly RelayCommand _loadLayoutCommand;

    private readonly RelayCommand _saveLayoutCommand;

    private readonly RelayCommand _dumpToConsoleCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        this.DataContext = Workspace.This;

        _loadLayoutCommand = new RelayCommand(OnLoadLayout, CanLoadLayout);
        _saveLayoutCommand = new RelayCommand(OnSaveLayout);
        _dumpToConsoleCommand = new RelayCommand(OnDumpToConsole);

        this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        this.Unloaded += new RoutedEventHandler(MainWindow_Unloaded);
    }

    /// <summary>
    /// Gets the command to load the layout.
    /// </summary>
    public ICommand LoadLayoutCommand => _loadLayoutCommand;

    /// <summary>
    /// Gets the command to save the layout.
    /// </summary>
    public ICommand SaveLayoutCommand => _saveLayoutCommand;

    public ICommand DumpToConsoleCommand => _dumpToConsoleCommand;

    /// <summary>
    /// Determines whether the layout can be loaded.
    /// </summary>
    /// <returns><c>true</c> if the layout can be loaded; otherwise, <c>false</c>.</returns>
    private static bool CanLoadLayout()
    {
        return File.Exists(@".\AvalonDock.Layout.config");
    }

    /// <summary>
    /// Handles the Loaded event of the MainWindow.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var serializer = new XmlLayoutSerializer(DockManager);

        if (File.Exists(@".\AvalonDock.config"))
        {
            serializer.Deserialize(@".\AvalonDock.config");
        }
    }

    /// <summary>
    /// Handles the Unloaded event of the MainWindow.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        var serializer = new XmlLayoutSerializer(DockManager);
        serializer.Serialize(@".\AvalonDock.config");
    }

    /// <summary>
    /// Handles the DumpToConsole event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void OnDumpToConsole()
    {
        // Uncomment when TRACE is activated on AvalonDock project
        DockManager.Layout.ConsoleDump(0);
    }

    /// <summary>
    /// Handles the LoadLayout command.
    /// </summary>
    private void OnLoadLayout()
    {
        var layoutSerializer = new XmlLayoutSerializer(DockManager);

        // Here I've implemented the LayoutSerializationCallback just to show
        //  a way to feed layout desarialization with content loaded at runtime
        // Actually I could in this case let AvalonDock to attach the contents
        // from current layout using the content ids
        // LayoutSerializationCallback should anyway be handled to attach contents
        // not currently loaded
        layoutSerializer.LayoutSerializationCallback += (s, e) =>
        {
            //if (e.Model.ContentId == FileStatsViewModel.ToolContentId)
            //{
            //    e.Content = Workspace.This.FileStats;
            //}
            //else if (!string.IsNullOrWhiteSpace(e.Model.ContentId)
            //        && File.Exists(e.Model.ContentId))
            //{
            //    e.Content = Workspace.This.Open(e.Model.ContentId);
            //}
        };

        layoutSerializer.Deserialize(@".\AvalonDock.Layout.config");
    }

    /// <summary>
    /// Handles the SaveLayout command.
    /// </summary>
    private void OnSaveLayout()
    {
        var layoutSerializer = new XmlLayoutSerializer(DockManager);
        layoutSerializer.Serialize(@".\AvalonDock.Layout.config");
    }
}
