using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RetailInventorySales.Data;

namespace RetailInventorySales.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly DataService _data = DataService.Instance;

        public DashboardView()
        {
            InitializeComponent();
            RefreshData();
        }

        public void RefreshData()
        {
            ActiveProductsText.Text = _data.TotalActiveProducts.ToString();
            UnitsInStockText.Text = _data.TotalUnitsInStock.ToString();
            LowStockText.Text = _data.LowStockCount.ToString();
            TodaysSalesText.Text = _data.TodaysSalesCount.ToString();
            TodaysRevenueText.Text = $"Rs. {_data.TodaysRevenue:N2}";
            AllTimeRevenueText.Text = $"Rs. {_data.TotalRevenueAllTime:N2}";

            var lowStock = _data.Products
                .Where(p => p.IsActive && p.IsLowStock)
                .OrderBy(p => p.QuantityInStock)
                .ToList();
            LowStockGrid.ItemsSource = lowStock;
            NoLowStockText.Visibility = lowStock.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var topSelling = _data.Sales
                .SelectMany(s => s.Items)
                .GroupBy(i => new { i.ProductId, i.ProductName })
                .Select(g => new ProductSalesSummary
                {
                    ProductName = g.Key.ProductName,
                    UnitsSold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.LineTotal)
                })
                .OrderByDescending(s => s.UnitsSold)
                .Take(10)
                .ToList();
            TopSellingGrid.ItemsSource = topSelling;
            NoSalesText.Visibility = topSelling.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }
    }

    /// <summary>
    /// Simple aggregate row used only to populate the Top Selling Products
    /// grid on the dashboard.
    /// </summary>
    public class ProductSalesSummary
    {
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}
