using System;

namespace RetailInventorySales.Models
{
    /// <summary>
    /// Represents a single sellable product held in inventory.
    /// </summary>
    public class Product
    {
        public int Id { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal CostPrice { get; set; }

        /// <summary>
        /// Current quantity on hand. Updated by sales and stock adjustments.
        /// </summary>
        public int QuantityInStock { get; set; }

        /// <summary>
        /// When stock falls at or below this level the product is flagged
        /// as "low stock" on the dashboard.
        /// </summary>
        public int ReorderLevel { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Soft-delete flag. Inactive products are hidden from the Sales
        /// screen but kept so historical transactions still resolve.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public bool IsLowStock => QuantityInStock <= ReorderLevel;

        public string StatusText => IsActive ? "Active" : "Inactive";

        public string StockStatus => IsLowStock ? "Low Stock" : "OK";

        public string DisplayLabel => $"{SKU} - {Name}";
    }
}