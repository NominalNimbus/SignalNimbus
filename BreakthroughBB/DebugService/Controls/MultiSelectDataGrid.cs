using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace DebugService.Controls
{
    /// <summary>
    /// Data Grid that allow to bind selected items
    /// </summary>
    public class MultiSelectDataGrid : DataGrid
    {
        public MultiSelectDataGrid()
        {
            SelectionChanged += CustomDataGrid_SelectionChanged;
        }

        void CustomDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItemsList = this.SelectedItems;
        }

        public IList SelectedItemsList
        {
            get { return (IList)GetValue(SelectedItemsListProperty); }
            set { SetValue(SelectedItemsListProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsListProperty =
            DependencyProperty.Register("SelectedItemsList", typeof(IList), 
                typeof(MultiSelectDataGrid), new PropertyMetadata(null));
    }
}
