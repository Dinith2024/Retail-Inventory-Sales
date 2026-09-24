using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RetailInventorySales.Data;
using RetailInventorySales.Models;

namespace RetailInventorySales.Views
{
    public partial class StockView : UserControl
    {
        private readonly DataService _data = DataService.Instance;
        private bool _suppressSelectionSync;

        public StockView()
        {
            InitializeComponent();
            RefreshData();
        }

        public void RefreshData()
        {
            var previouslySelectedId = (ProductCombo.SelectedItem as Product)?.Id;

            var activeProducts = _data.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
            StockGrid.ItemsSource = activeProducts;
            ProductCombo.ItemsSource = activeProducts;

            if (previouslySelectedId.HasValue)
            {
                var match = activeProducts.FirstOrDefault(p => p.Id == previouslySelectedId.Value);
                _suppressSelectionSync = true;
                ProductCombo.SelectedItem = match;
                _suppressSelectionSync = false;
            }

            MovementsGrid.ItemsSource = _data.StockMovements.Take(100).ToList();
            UpdateCurrentStockText();
        }

        private void UpdateCurrentStockText()
        {
            if (ProductCombo.SelectedItem is Product product)
            {
                CurrentStockText.Text = $"Current stock: {product.QuantityInStock}   (reorder at {product.ReorderLevel})";
            }
            else
            {
                CurrentStockText.Text = "Current stock: -";
            }
        }

        private void StockGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StockGrid.SelectedItem is Product product)
            {
                _suppressSelectionSync = true;
                ProductCombo.SelectedItem = product;
                _suppressSelectionSync = false;
                UpdateCurrentStockText();
            }
        }

        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCurrentStockText();

            if (_suppressSelectionSync) return;

            if (ProductCombo.SelectedItem is Product product)
            {
                StockGrid.SelectedItem = product;
            }
        }

        private void ShowValidation(string message)
        {
            ValidationText.Text = message;
            ValidationText.Visibility = Visibility.Visible;
        }

        private void HideValidation() => ValidationText.Visibility = Visibility.Collapsed;

        private void ApplyAdjustment_Click(object sender, RoutedEventArgs e)
        {
            HideValidation();

            if (ProductCombo.SelectedItem is not Product product)
            {
                ShowValidation("Select a product first.");
                return;
            }

            if (!int.TryParse(QuantityBox.Text, out int quantityChange))
            {
                ShowValidation("Enter a valid whole number (positive to add stock, negative to remove it).");
                return;
            }

            string movementTag = (MovementTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ManualAdjustment";
            var movementType = movementTag switch
            {
                "Restock" => StockMovementType.Restock,
                "Return" => StockMovementType.Return,
                _ => StockMovementType.ManualAdjustment
            };

            string notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? "(no notes provided)" : NotesBox.Text.Trim();

            string? error = _data.AdjustStock(product, quantityChange, movementType, notes);
            if (error != null)
            {
                ShowValidation(error);
                return;
            }

            MessageBox.Show(
                $"Stock for '{product.Name}' updated. New stock level: {product.QuantityInStock}.",
                "Stock Adjusted",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            QuantityBox.Text = string.Empty;
            NotesBox.Text = string.Empty;
            RefreshData();
        }
    }
}
