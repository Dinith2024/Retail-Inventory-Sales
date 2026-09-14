using System.Windows;
using System.Windows.Controls;
namespace RetailInventorySales
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Each view keeps its own local snapshot for display (e.g. a
        /// DataGrid's items). Whenever the user switches tabs, refresh the
        /// destination tab so it reflects any changes made elsewhere (for
        /// example, completing a sale on the Sales tab should immediately
        /// be visible on the Dashboard and Stock Management tabs)
        /// </summary>
        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != MainTabs) return;

            switch (MainTabs.SelectedIndex)
            {
                case 0:
                    DashboardTab.RefreshData();
                    break;
                case 1:
                    ProductsTab.RefreshData();
                    break;
                case 2:
                    SalesTab.RefreshData();
                    break;
                case 3:
                    StockTab.RefreshData();
                    break;
                case 4:
                    HistoryTab.RefreshData();
                    break;
            }
        }
    }
}