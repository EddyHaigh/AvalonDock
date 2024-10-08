using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

namespace MVVM.CommunityToolkit.Demo.ViewModels.Abstractions;

public partial class PaneViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the title of the pane.
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// Gets or sets the content ID of the pane.
    /// </summary>
    [ObservableProperty]
    private string _contentId = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the pane is active.
    /// </summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>
    /// Gets or sets the icon source of the pane.
    /// </summary>
    public ImageSource? IconSource { get; protected set; }
}
