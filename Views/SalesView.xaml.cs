using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RetailInventorySales.Data;
using RetailInventorySales.Models;

namespace RetailInventorySales.Views
{
    public partial class SalesView : UserControl
    {
        private readonly DataService _data = DataService.Instance;
        private readonly ObservableCollection<SaleLineItem> _cart = new();

        public SalesView()
        {
            InitializeComponent();
            CartGrid.ItemsSource = _cart;
            RefreshData();
        }

        public void RefreshData()
        {
            var previouslySelectedId = (ProductCombo.SelectedItem as Product)?.Id;

            ProductCombo.ItemsSource = _data.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToList();

            if (previouslySelectedId.HasValue)
            {
                ProductCombo.SelectedItem = (ProductCombo.ItemsSource as System.Collections.Generic.List<Product>)?
                    .FirstOrDefault(p => p.Id == previouslySelectedId.Value);
            }

            UpdateStockHint();
        }

        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateStockHint();

        private void UpdateStockHint()
        {
            if (ProductCombo.SelectedItem is Product product)
            {
                StockHintText.Text = $"Price: Rs. {product.Price:N2}   |   Available stock: {product.QuantityInStock}";
            }
            else
            {
                StockHintText.Text = string.Empty;
            }
        }

        private void ShowAddError(string message)
        {
            AddErrorText.Text = message;
            AddErrorText.Visibility = Visibility.Visible;
        }

        private void HideAddError() => AddErrorText.Visibility = Visibility.Collapsed;

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            HideAddError();

            if (ProductCombo.SelectedItem is not Product product)
            {
                ShowAddError("Select a product first.");
                return;
            }

            if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
            {
                ShowAddError("Enter a valid quantity greater than zero.");
                return;
            }

            var existingLine = _cart.FirstOrDefault(i => i.ProductId == product.Id);
            int alreadyInCart = existingLine?.Quantity ?? 0;
            int totalRequested = alreadyInCart + quantity;

            if (totalRequested > product.QuantityInStock)
            {
                ShowAddError($"Only {product.QuantityInStock} unit(s) of '{product.Name}' are in stock ({alreadyInCart} already in cart).");
                return;
            }

            if (existingLine != null)
            {
                existingLine.Quantity = totalRequested;
                // Refresh the grid row since SaleLineItem does not raise
                // property-changed notifications.
                CartGrid.Items.Refresh();
            }
            else
            {
                _cart.Add(new SaleLineItem
                {
                    ProductId = product.Id,
                    SKU = product.SKU,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = quantity
                });
            }

            QuantityBox.Text = "1";
            RecalculateTotals();
        }

        private void RemoveFromCart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SaleLineItem item)
            {
                _cart.Remove(item);
                RecalculateTotals();
            }
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            _cart.Clear();
            RecalculateTotals();
        }

        private void AdjustmentBox_TextChanged(object sender, TextChangedEventArgs e) => RecalculateTotals();

        private decimal ParsePercent(string text)
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) && value >= 0)
            {
                return value;
            }
            return 0;
        }

        private void RecalculateTotals()
        {
            if (DiscountBox == null || TaxBox == null || SubtotalText == null ||
                DiscountAmountText == null || TaxAmountText == null || TotalText == null)
            {
                return;
            }

            decimal subtotal = _cart.Sum(i => i.LineTotal);
            decimal discountPercent = ParsePercent(DiscountBox.Text);
            decimal taxPercent = ParsePercent(TaxBox.Text);

            decimal discountAmount = System.Math.Round(subtotal * (discountPercent / 100m), 2);
            decimal taxable = subtotal - discountAmount;
            decimal taxAmount = System.Math.Round(taxable * (taxPercent / 100m), 2);
            decimal total = taxable + taxAmount;

            SubtotalText.Text = $"Rs. {subtotal:N2}";
            DiscountAmountText.Text = $"- Rs. {discountAmount:N2}";
            TaxAmountText.Text = $"+ Rs. {taxAmount:N2}";
            TotalText.Text = $"Rs. {total:N2}";
        }

        private void ShowSaleError(string message)
        {
            SaleErrorText.Text = message;
            SaleErrorText.Visibility = Visibility.Visible;
        }

        private void HideSaleError() => SaleErrorText.Visibility = Visibility.Collapsed;

        private void CompleteSale_Click(object sender, RoutedEventArgs e)
        {
            HideSaleError();

            if (_cart.Count == 0)
            {
                ShowSaleError("The cart is empty. Add at least one product before completing the sale.");
                return;
            }

            string paymentMethod = (PaymentCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cash";

            var sale = new SaleTransaction
            {
                Items = _cart.ToList(),
                DiscountPercent = ParsePercent(DiscountBox.Text),
                TaxPercent = ParsePercent(TaxBox.Text),
                PaymentMethod = paymentMethod
            };

            string? error = _data.CompleteSale(sale);
            if (error != null)
            {
                ShowSaleError(error);
                RefreshData();
                return;
            }

            MessageBox.Show(
                $"Sale {sale.TransactionNumber} completed.\n\nItems: {sale.ItemCount}\nTotal: Rs. {sale.Total:N2}\nPayment: {sale.PaymentMethod}",
                "Sale Completed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _cart.Clear();
            DiscountBox.Text = "0";
            TaxBox.Text = "0";
            RecalculateTotals();
            RefreshData();
        }
    }
}
