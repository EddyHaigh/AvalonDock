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
        Anchorables = new CustomObservableCollection<object>();
        Documents = new CustomObservableCollection<object>();
    }

    public CustomObservableCollection<object> Anchorables { get; private set; }
    public CustomObservableCollection<object> Documents { get; private set; }
}