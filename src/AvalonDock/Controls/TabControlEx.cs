using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace AvalonDock.Controls
{
    /// <summary>
    /// This control added to mitigate issue with tab (document) switching speed
    /// See this https://stackoverflow.com/questions/2080764/how-to-preserve-control-state-within-tab-items-in-a-tabcontrol
    /// and this https://stackoverflow.com/questions/31030293/cefsharp-in-tabcontrol-not-working/37171847#37171847
    ///
    /// by implmenting an option to enable virtualization for tabbed document containers.
    /// </summary>
    [TemplatePart(Name = "PART_ItemsHolder", Type = typeof(Panel))]
    public class TabControlEx : TabControl
    {
        private readonly bool _isVirtualizing;

        private Panel _itemsHolderPanel = null;

        /// <summary>
        /// Class constructor from virtualization parameter.
        /// </summary>
        /// <param name="isVirtualizing">Whether tabbed items are virtualized or not.</param>
        public TabControlEx(bool isVirtualizing)
            : this()
        {
            _isVirtualizing = isVirtualizing;
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        protected TabControlEx()
            : base()
        {
            _isVirtualizing = true;

            // This is necessary so that we get the initial databound selected item
            ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }

        /// <summary>Gets whether the control and its inheriting classes are virtualizing their items or not.</summary>
        [Bindable(false)]
        [Description("Gets whether the control and its inheriting classes are virtualizing their items or not.")]
        [Category("Other")]
        public bool IsVirtualiting => _isVirtualizing;

        /// <summary>
        /// Get the ItemsHolder and generate any children
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Code below is required only if virtualization is turned ON
            if (_isVirtualizing)
            {
                return;
            }

            _itemsHolderPanel = CreateGrid();
            // exchange ContentPresenter for Grid
            var topGrid = (Grid)GetVisualChild(0);

            if (topGrid != null
                && topGrid.Children != null
                && topGrid.Children.Count > 2)
            {
                for (int i = 1; i <= 2; i++)
                {
                    if (topGrid.Children[i] is Border border)
                    {
                        border.Child = _itemsHolderPanel;
                        break;
                    }
                }
            }

            UpdateSelectedItem();
        }

        /// <summary>
        /// Gets the currently selected item (including its generation if Virtualization is currently switched on).
        /// </summary>
        /// <returns></returns>
        protected TabItem GetSelectedTabItem()
        {
            object selectedItem = base.SelectedItem;

            // Code below is required only if virtualization is turned ON
            if (_isVirtualizing)
            {
                return selectedItem as TabItem;
            }

            if (selectedItem == null)
            {
                return null;
            }

            if (selectedItem is not TabItem item)
            {
                item = base.ItemContainerGenerator.ContainerFromIndex(base.SelectedIndex) as TabItem;
            }

            return item;
        }

        /// <summary>
        /// When the items change we remove any generated panel children and add any new ones as necessary
        /// </summary>
        /// <param name="e"></param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            // Code below is required only if virtualization is turned ON
            if (_isVirtualizing)
            {
                return;
            }

            if (_itemsHolderPanel == null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    _itemsHolderPanel.Children.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            ContentPresenter cp = FindChildContentPresenter(item);
                            if (cp != null)
                            {
                                _itemsHolderPanel.Children.Remove(cp);
                            }
                        }
                    }

                    // Don't do anything with new items because we don't want to
                    // create visuals that aren't being shown

                    UpdateSelectedItem();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException("Replace not implemented yet");
            }
        }

        /// <summary>
        /// Raises the <see cref="System.Windows.Controls.Primitives.Selector.SelectionChanged"/> routed event.
        /// </summary>
        /// <param name="e">Provides data for <see cref="SelectionChangedEventArgs"/>.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            // Code below is required only if virtualization is turned ON
            if (_isVirtualizing)
            {
                return;
            }

            UpdateSelectedItem();
        }
        private ContentPresenter CreateChildContentPresenter(object item)
        {
            if (item == null)
            {
                return null;
            }

            ContentPresenter cp = FindChildContentPresenter(item);

            if (cp != null)
            {
                return cp;
            }

            // the actual child to be added.  cp.Tag is a reference to the TabItem
            cp = new ContentPresenter();
            cp.Content = (item is TabItem) ? (item as TabItem).Content : item;
            cp.ContentTemplate = this.SelectedContentTemplate;
            cp.ContentTemplateSelector = this.SelectedContentTemplateSelector;
            cp.ContentStringFormat = this.SelectedContentStringFormat;
            cp.Visibility = Visibility.Collapsed;
            cp.Tag = (item is TabItem) ? item : (this.ItemContainerGenerator.ContainerFromItem(item));
            _itemsHolderPanel.Children.Add(cp);
            return cp;
        }

        private Grid CreateGrid()
        {
            var grid = new Grid();
            Binding binding = new Binding(PaddingProperty.Name);
            binding.Source = this;  // view model?
            grid.SetBinding(Grid.MarginProperty, binding);

            binding = new Binding(SnapsToDevicePixelsProperty.Name);
            binding.Source = this;  // view model?
            grid.SetBinding(Grid.SnapsToDevicePixelsProperty, binding);

            return grid;
        }

        private ContentPresenter FindChildContentPresenter(object data)
        {
            if (data is TabItem tabItem)
            {
                data = tabItem.Content;
            }

            if (data == null)
            {
                return null;
            }

            if (_itemsHolderPanel == null)
            {
                return null;
            }

            foreach (ContentPresenter cp in _itemsHolderPanel.Children)
            {
                if (cp.Content == data)
                {
                    return cp;
                }
            }

            return null;
        }

        /// <summary>
        /// If containers are done, generate the selected item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (this.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                this.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
                UpdateSelectedItem();
            }
        }
        private void UpdateSelectedItem()
        {
            if (_itemsHolderPanel == null)
            {
                return;
            }

            // Generate a ContentPresenter if necessary
            TabItem item = GetSelectedTabItem();
            if (item != null)
            {
                CreateChildContentPresenter(item);
            }

            // show the right child
            foreach (ContentPresenter child in _itemsHolderPanel.Children)
            {
                child.Visibility = ((child.Tag as TabItem).IsSelected) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}