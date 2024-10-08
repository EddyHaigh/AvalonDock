using CommunityToolkit.Mvvm.ComponentModel;

using MVVM.CommunityToolkit.Demo.ViewModels.Abstractions;

namespace MVVM.CommunityToolkit.Demo.ViewModels;

/// <summary>
/// Represents a view model for a tool.
/// </summary>
public partial class ToolViewModel : PaneViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolViewModel"/> class.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    public ToolViewModel(string name)
    {
        _name = name;
        Title = name;
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isVisible = true;
}
