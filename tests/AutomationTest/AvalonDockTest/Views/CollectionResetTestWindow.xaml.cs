using System.Windows;

namespace AvalonDockTest.Views;

/// <summary>
/// Interaction logic for CollectionResetTestWindow.xaml
/// </summary>
public partial class CollectionResetTestWindow : Window
{
    public CollectionResetTestWindow()
    {
        InitializeComponent();
        DataContext = this;
        Anchorables = [];
        Documents = [];
    }

    public CustomObservableCollection<object> Anchorables { get; private set; }
    public CustomObservableCollection<object> Documents { get; private set; }
}