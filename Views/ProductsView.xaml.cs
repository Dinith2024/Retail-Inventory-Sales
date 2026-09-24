using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RetailInventorySales.Data;
using RetailInventorySales.Models;

namespace RetailInventorySales.Views
{
    public partial class ProductsView : UserControl
    {
        private readonly DataService _data = DataService.Instance;
        private Product? _selectedProduct;

        public ProductsView()
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
            bool showInactive = ShowInactiveCheck.IsChecked == true;

            var query = _data.Products.AsEnumerable();

            if (!showInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.SKU.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            ProductsGrid.ItemsSource = query.OrderBy(p => p.Name).ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ShowInactiveCheck_Changed(object sender, RoutedEventArgs e) => ApplyFilter();

        private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedProduct = ProductsGrid.SelectedItem as Product;

            if (_selectedProduct == null)
            {
                ResetForm();
                return;
            }

            FormTitleText.Text = "Edit Product";
            SkuBox.Text = _selectedProduct.SKU;
            NameBox.Text = _selectedProduct.Name;
            CategoryBox.Text = _selectedProduct.Category;
            PriceBox.Text = _selectedProduct.Price.ToString(CultureInfo.InvariantCulture);
            CostPriceBox.Text = _selectedProduct.CostPrice.ToString(CultureInfo.InvariantCulture);
            StockBox.Text = _selectedProduct.QuantityInStock.ToString(CultureInfo.InvariantCulture);
            ReorderBox.Text = _selectedProduct.ReorderLevel.ToString(CultureInfo.InvariantCulture);

            StockBox.IsEnabled = false;
            StockLabelText.Text = "Current Stock (read-only)";
            StockNoteText.Visibility = Visibility.Visible;

            SaveButton.Content = "Save Changes";
            CancelButton.Visibility = Visibility.Visible;
            DeactivateButton.IsEnabled = _selectedProduct.IsActive;
            HideValidation();
        }

        private void ResetForm()
        {
            _selectedProduct = null;
            FormTitleText.Text = "Add Product";
            SkuBox.Text = string.Empty;
            NameBox.Text = string.Empty;
            CategoryBox.Text = string.Empty;
            PriceBox.Text = string.Empty;
            CostPriceBox.Text = string.Empty;
            StockBox.Text = string.Empty;
            ReorderBox.Text = string.Empty;

            StockBox.IsEnabled = true;
            StockLabelText.Text = "Initial Stock";
            StockNoteText.Visibility = Visibility.Collapsed;

            SaveButton.Content = "Add Product";
            CancelButton.Visibility = Visibility.Collapsed;
            DeactivateButton.IsEnabled = false;
            ProductsGrid.SelectedItem = null;
            HideValidation();
        }

        private void ShowValidation(string message)
        {
            ValidationText.Text = message;
            ValidationText.Visibility = Visibility.Visible;
        }

        private void HideValidation()
        {
            ValidationText.Visibility = Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price))
            {
                ShowValidation("Enter a valid selling price (e.g. 150.00).");
                return;
            }

            decimal costPrice = 0;
            if (!string.IsNullOrWhiteSpace(CostPriceBox.Text) &&
                !decimal.TryParse(CostPriceBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out costPrice))
            {
                ShowValidation("Enter a valid cost price, or leave it blank.");
                return;
            }

            if (!int.TryParse(ReorderBox.Text, out int reorderLevel))
            {
                ShowValidation("Enter a valid whole number for reorder level (e.g. 10).");
                return;
            }

            int stock = _selectedProduct?.QuantityInStock ?? 0;
            if (_selectedProduct == null)
            {
                if (!int.TryParse(StockBox.Text, out stock))
                {
                    ShowValidation("Enter a valid whole number for initial stock (e.g. 20).");
                    return;
                }
            }

            var product = new Product
            {
                Id = _selectedProduct?.Id ?? 0,
                SKU = SkuBox.Text.Trim(),
                Name = NameBox.Text.Trim(),
                Category = CategoryBox.Text.Trim(),
                Price = price,
                CostPrice = costPrice,
                QuantityInStock = stock,
                ReorderLevel = reorderLevel,
                IsActive = _selectedProduct?.IsActive ?? true
            };

            string? error = _data.ValidateProduct(product);
            if (error != null)
            {
                ShowValidation(error);
                return;
            }

            if (_selectedProduct == null)
            {
                _data.AddProduct(product);
                MessageBox.Show($"'{product.Name}' was added to the product catalog.", "Product Added", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _data.UpdateProduct(product);
                MessageBox.Show($"'{product.Name}' was updated.", "Product Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ResetForm();
            ApplyFilter();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => ResetForm();

        private void DeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null) return;

            var result = MessageBox.Show(
                $"Deactivate '{_selectedProduct.Name}'? It will no longer be available on the Sales screen.",
                "Confirm Deactivate",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            string message = _data.DeactivateProduct(_selectedProduct);
            MessageBox.Show(message, "Product Deactivated", MessageBoxButton.OK, MessageBoxImage.Information);
            ResetForm();
            ApplyFilter();
        }
    }
}
