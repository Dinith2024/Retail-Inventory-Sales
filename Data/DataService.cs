using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using RetailInventorySales.Models;

namespace RetailInventorySales.Data
{
    /// <summary>
    /// Central point for all data access and business rules. Holds the
    /// in-memory collections that the UI binds to, and persists every
    /// change straight to the JSON files under the Data\Store folder.
    ///
    /// A single shared instance (Instance) is used throughout the app so
    /// every screen sees the same, up to date data.
    /// </summary>
    public class DataService
    {
        public static DataService Instance { get; } = new DataService();

        private readonly string _dataFolder;
        private readonly string _productsPath;
        private readonly string _movementsPath;
        private readonly string _salesPath;

        public ObservableCollection<Product> Products { get; }
        public ObservableCollection<StockMovement> StockMovements { get; }
        public ObservableCollection<SaleTransaction> Sales { get; }

        private DataService()
        {
            _dataFolder = Path.Combine(AppContext.BaseDirectory, "Data", "Store");
            _productsPath = Path.Combine(_dataFolder, "products.json");
            _movementsPath = Path.Combine(_dataFolder, "stock_movements.json");
            _salesPath = Path.Combine(_dataFolder, "sales.json");

            Products = new ObservableCollection<Product>(JsonFileStore<Product>.Load(_productsPath));
            StockMovements = new ObservableCollection<StockMovement>(JsonFileStore<StockMovement>.Load(_movementsPath));
            Sales = new ObservableCollection<SaleTransaction>(JsonFileStore<SaleTransaction>.Load(_salesPath));

            if (Products.Count == 0)
            {
                SeedDemoData();
            }
        }

        // ----------------------------------------------------------------
        // Persistence helpers
        // ----------------------------------------------------------------

        private void SaveProducts() => JsonFileStore<Product>.Save(_productsPath, Products.ToList());

        private void SaveMovements() => JsonFileStore<StockMovement>.Save(_movementsPath, StockMovements.ToList());

        private void SaveSales() => JsonFileStore<SaleTransaction>.Save(_salesPath, Sales.ToList());

        // ----------------------------------------------------------------
        // Product management
        // ----------------------------------------------------------------

        public int GetNextProductId() => Products.Count == 0 ? 1 : Products.Max(p => p.Id) + 1;

        /// <summary>
        /// Validates a SKU is present and not already used by another
        /// product. Returns an error message, or null if valid.
        /// </summary>
        public string? ValidateProduct(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.SKU))
            {
                return "SKU is required.";
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return "Product name is required.";
            }

            if (product.Price <= 0)
            {
                return "Price must be greater than zero.";
            }

            if (product.QuantityInStock < 0)
            {
                return "Initial stock cannot be negative.";
            }

            if (product.ReorderLevel < 0)
            {
                return "Reorder level cannot be negative.";
            }

            bool duplicateSku = Products.Any(p =>
                p.Id != product.Id &&
                string.Equals(p.SKU, product.SKU, StringComparison.OrdinalIgnoreCase));

            if (duplicateSku)
            {
                return $"SKU '{product.SKU}' is already used by another product.";
            }

            return null;
        }

        public void AddProduct(Product product)
        {
            product.Id = GetNextProductId();
            product.CreatedDate = DateTime.Now;
            Products.Add(product);
            SaveProducts();

            if (product.QuantityInStock > 0)
            {
                RecordMovement(product, StockMovementType.InitialStock, product.QuantityInStock, "Initial stock on product creation");
            }
        }

        public void UpdateProduct(Product updated)
        {
            var existing = Products.FirstOrDefault(p => p.Id == updated.Id);
            if (existing == null) return;

            existing.SKU = updated.SKU;
            existing.Name = updated.Name;
            existing.Category = updated.Category;
            existing.Price = updated.Price;
            existing.CostPrice = updated.CostPrice;
            existing.ReorderLevel = updated.ReorderLevel;
            // Stock quantity is intentionally NOT edited here - it must go
            // through AdjustStock so every change is recorded in the
            // stock movement audit trail.
            SaveProducts();
        }

        public string DeactivateProduct(Product product)
        {
            product.IsActive = false;
            SaveProducts();
            return $"'{product.Name}' was marked inactive and removed from the Sales screen.";
        }

        // ----------------------------------------------------------------
        // Stock management
        // ----------------------------------------------------------------

        public int GetNextMovementId() => StockMovements.Count == 0 ? 1 : StockMovements.Max(m => m.Id) + 1;

        private void RecordMovement(Product product, StockMovementType type, int quantityChange, string notes)
        {
            var movement = new StockMovement
            {
                Id = GetNextMovementId(),
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                MovementType = type,
                QuantityChange = quantityChange,
                ResultingStock = product.QuantityInStock,
                Notes = notes,
                Timestamp = DateTime.Now
            };
            StockMovements.Insert(0, movement);
            SaveMovements();
        }

        /// <summary>
        /// Applies a manual stock adjustment (restock, correction, return,
        /// etc). Positive quantity increases stock, negative decreases it.
        /// </summary>
        public string? AdjustStock(Product product, int quantityChange, StockMovementType type, string notes)
        {
            if (quantityChange == 0)
            {
                return "Enter a non-zero quantity.";
            }

            int newStock = product.QuantityInStock + quantityChange;
            if (newStock < 0)
            {
                return $"This adjustment would take stock below zero (current stock: {product.QuantityInStock}).";
            }

            product.QuantityInStock = newStock;
            SaveProducts();
            RecordMovement(product, type, quantityChange, notes);
            return null;
        }

        // ----------------------------------------------------------------
        // Sales
        // ----------------------------------------------------------------

        public int GetNextSaleId() => Sales.Count == 0 ? 1 : Sales.Max(s => s.Id) + 1;

        private string GenerateTransactionNumber(int id) => $"INV-{id:D6}";

        /// <summary>
        /// Completes a sale: validates stock is sufficient for every line,
        /// deducts stock, records a movement per line, and saves the
        /// transaction to history. Returns an error message, or null on
        /// success.
        /// </summary>
        public string? CompleteSale(SaleTransaction sale)
        {
            if (sale.Items.Count == 0)
            {
                return "Add at least one product to the cart before completing the sale.";
            }

            // Re-validate stock right before committing, in case it
            // changed since the item was added to the cart.
            foreach (var item in sale.Items)
            {
                var product = Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null)
                {
                    return $"Product '{item.ProductName}' no longer exists.";
                }
                if (item.Quantity <= 0)
                {
                    return $"Quantity for '{item.ProductName}' must be greater than zero.";
                }
                if (product.QuantityInStock < item.Quantity)
                {
                    return $"Not enough stock for '{item.ProductName}'. Available: {product.QuantityInStock}, requested: {item.Quantity}.";
                }
            }

            sale.Id = GetNextSaleId();
            sale.TransactionNumber = GenerateTransactionNumber(sale.Id);
            sale.Timestamp = DateTime.Now;

            foreach (var item in sale.Items)
            {
                var product = Products.First(p => p.Id == item.ProductId);
                product.QuantityInStock -= item.Quantity;
                RecordMovement(product, StockMovementType.Sale, -item.Quantity, $"Sold on {sale.TransactionNumber}");
            }

            SaveProducts();
            Sales.Insert(0, sale);
            SaveSales();
            return null;
        }

        // ----------------------------------------------------------------
        // Dashboard helpers
        // ----------------------------------------------------------------

        public int TotalActiveProducts => Products.Count(p => p.IsActive);

        public int TotalUnitsInStock => Products.Sum(p => p.QuantityInStock);

        public int LowStockCount => Products.Count(p => p.IsActive && p.IsLowStock);

        public decimal TotalRevenueAllTime => Sales.Sum(s => s.Total);

        public int TodaysSalesCount => Sales.Count(s => s.Timestamp.Date == DateTime.Today);

        public decimal TodaysRevenue => Sales.Where(s => s.Timestamp.Date == DateTime.Today).Sum(s => s.Total);

        // ----------------------------------------------------------------
        // Demo seed data (only used the very first time the app runs)
        // ----------------------------------------------------------------

        private void SeedDemoData()
        {
            var demoProducts = new[]
            {
                new Product { SKU = "BEV-001", Name = "Mineral Water 500ml", Category = "Beverages", Price = 80m, CostPrice = 55m, QuantityInStock = 120, ReorderLevel = 20 },
                new Product { SKU = "BEV-002", Name = "Ceylon Tea 200g", Category = "Beverages", Price = 450m, CostPrice = 320m, QuantityInStock = 40, ReorderLevel = 10 },
                new Product { SKU = "SNK-001", Name = "Potato Chips 100g", Category = "Snacks", Price = 150m, CostPrice = 100m, QuantityInStock = 60, ReorderLevel = 15 },
                new Product { SKU = "SNK-002", Name = "Chocolate Bar 50g", Category = "Snacks", Price = 120m, CostPrice = 80m, QuantityInStock = 8, ReorderLevel = 10 },
                new Product { SKU = "HSH-001", Name = "Dish Washing Liquid 500ml", Category = "Household", Price = 320m, CostPrice = 230m, QuantityInStock = 25, ReorderLevel = 5 },
                new Product { SKU = "HSH-002", Name = "Toilet Tissue 4-Pack", Category = "Household", Price = 480m, CostPrice = 350m, QuantityInStock = 3, ReorderLevel = 8 },
            };

            foreach (var product in demoProducts)
            {
                AddProduct(product);
            }
        }
    }
}