using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RetailInventorySales.Data;
using RetailInventorySales.Models;

namespace RetailInventorySales.Views
{
    public partial class HistoryView : UserControl
    {
        private readonly DataService _data = DataService.Instance;

        public HistoryView()
        {
            InitializeComponent();
            RefreshData();
        }

        public void RefreshData()
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string search = SearchBox.Text?.Trim() ?? string.Empty;

            var query = _data.Sales.AsEnumerable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s =>
                    s.TransactionNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.PaymentMethod.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            SalesGrid.ItemsSource = query.OrderByDescending(s => s.Timestamp).ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void SalesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SalesGrid.SelectedItem is not SaleTransaction sale)
            {
                DetailsPanel.Visibility = Visibility.Collapsed;
                NoSelectionText.Visibility = Visibility.Visible;
                return;
            }

            DetailsPanel.Visibility = Visibility.Visible;
            NoSelectionText.Visibility = Visibility.Collapsed;

            DetailTransactionNumber.Text = sale.TransactionNumber;
            DetailDateTime.Text = sale.Timestamp.ToString("dddd, dd MMM yyyy  HH:mm");
            DetailPayment.Text = $"Payment method: {sale.PaymentMethod}";

            DetailItemsGrid.ItemsSource = sale.Items;

            DetailSubtotal.Text = $"Rs. {sale.Subtotal:N2}";
            DetailDiscountLabel.Text = $"Discount ({sale.DiscountPercent}%)";
            DetailDiscount.Text = $"- Rs. {sale.DiscountAmount:N2}";
            DetailTaxLabel.Text = $"Tax ({sale.TaxPercent}%)";
            DetailTax.Text = $"+ Rs. {sale.TaxAmount:N2}";
            DetailTotal.Text = $"Rs. {sale.Total:N2}";
        }
    }
}
