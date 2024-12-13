using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

using AvalonDock.Controls;
using AvalonDock.Converters;
using AvalonDock.Layout;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AvalonDockTest;

[STATestClass]
public sealed class LayoutAnchorableTest
{
    [STATestMethod]
    public void ClearBindingOfHiddenWindowTest()
    {
        LayoutAnchorable layoutAnchorable = new()
        {
            FloatingWidth = 50,
            FloatingHeight = 100,
            ContentId = "Test"
        };

        LayoutAnchorablePane layoutAnchorablePane = new(layoutAnchorable);
        LayoutAnchorablePaneGroup layoutAnchorablePaneGroup = new(layoutAnchorablePane);
        LayoutAnchorableFloatingWindow layoutFloatingWindow = new()
        {
            RootPanel = layoutAnchorablePaneGroup
        };

        var ctor = typeof(LayoutAnchorableFloatingWindowControl)
          .GetTypeInfo()
          .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
          .First(x => x.GetParameters().Length == 1);

        LayoutAnchorableFloatingWindowControl floatingWindowControl = ctor.Invoke([layoutFloatingWindow]) as LayoutAnchorableFloatingWindowControl;
        floatingWindowControl.SetBinding(
          UIElement.VisibilityProperty,
          new Binding("IsVisible")
          {
              Source = floatingWindowControl.Model,
              Converter = new BoolToVisibilityConverter(),
              Mode = BindingMode.OneWay,
              ConverterParameter = Visibility.Hidden
          });

        BindingExpression visibilityBinding = floatingWindowControl.GetBindingExpression(UIElement.VisibilityProperty);
        Assert.IsNotNull(visibilityBinding);

        layoutAnchorable.Show();
        layoutAnchorable.Hide();

        visibilityBinding = floatingWindowControl.GetBindingExpression(UIElement.VisibilityProperty);
        Assert.IsNotNull(visibilityBinding);

        floatingWindowControl.Hide();

        visibilityBinding = floatingWindowControl.GetBindingExpression(UIElement.VisibilityProperty);
        Assert.IsNull(visibilityBinding);
    }
}