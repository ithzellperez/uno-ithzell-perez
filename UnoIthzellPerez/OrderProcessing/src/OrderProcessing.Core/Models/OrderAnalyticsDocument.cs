namespace OrderProcessing.Core.Models;

public class OrderAnalyticsDocument
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
    public List<TopProductEntry> TopProducts { get; set; } = [];
}

public class TopProductEntry
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
