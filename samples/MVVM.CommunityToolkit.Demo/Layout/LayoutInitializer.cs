using System.Diagnostics;

using AvalonDock.Layout;

namespace MVVM.CommunityToolkit.Demo.Layout;

public class LayoutInitializer : ILayoutUpdateStrategy
{
    /// <summary>
    /// Determines whether to insert the anchorable before the specified destination container.
    /// </summary>
    /// <param name="layout">The layout root.</param>
    /// <param name="anchorableToShow">The anchorable to show.</param>
    /// <param name="destinationContainer">The destination container.</param>
    /// <returns><c>true</c> if the anchorable should be inserted before the destination container; otherwise, <c>false</c>.</returns>
    public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
    {
        //AD wants to add the anchorable into destinationContainer
        //just for test provide a new anchorablepane 
        //if the pane is floating let the manager go ahead
        if (destinationContainer is LayoutAnchorablePane layoutAnchorablePane && layoutAnchorablePane.FindParent<LayoutFloatingWindow>() != null)
        {
            return false;
        }

        var toolsPane = layout
            .Descendents()
            .OfType<LayoutAnchorablePane>()
            .FirstOrDefault(d => d.Name.Equals("ToolsPane"));

        if (toolsPane != null)
        {
            toolsPane.Children.Add(anchorableToShow);
            return true;
        }

        return false;

    }

    /// <summary>
    /// Performs actions after inserting the anchorable.
    /// </summary>
    /// <param name="layout">The layout root.</param>
    /// <param name="anchorableShown">The anchorable that was shown.</param>
    public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
    {
        Debug.Write("Anchorable added");
    }

    /// <summary>
    /// Determines whether to insert the document before the specified destination container.
    /// </summary>
    /// <param name="layout">The layout root.</param>
    /// <param name="anchorableToShow">The document to show.</param>
    /// <param name="destinationContainer">The destination container.</param>
    /// <returns><c>true</c> if the document should be inserted before the destination container; otherwise, <c>false</c>.</returns>
    public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer)
    {
        return false;
    }

    /// <summary>
    /// Performs actions after inserting the document.
    /// </summary>
    /// <param name="layout">The layout root.</param>
    /// <param name="anchorableShown">The document that was shown.</param>
    public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
    {
        Debug.Write("Document added");
    }
}
