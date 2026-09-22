namespace RetailInventorySales.Models
{
    /// <summary>
    /// One line of a sale transaction. Product details are snapshotted at
    /// the time of sale so that historical transactions remain accurate
    /// even if a product's price or name changes later.
    /// </summary>
    public class SaleLineItem
    {
        public int ProductId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal => UnitPrice * Quantity;
    }
}