namespace MedStore.Models;
public sealed class DashboardViewModel
{
    public int Medicines { get; set; }
    public int Categories { get; set; }
    public int Suppliers { get; set; }
    public int Customers { get; set; }
    public int LowStock { get; set; }
    public int Expired { get; set; }
    public int Employees { get; set; }
    public decimal TodaySales { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal InventoryValue { get; set; }
    public List<Medicine> Alerts { get; set; } = [];
    public List<DailySales> SalesLast7Days { get; set; } = [];
    public List<StoreTransaction> RecentSales { get; set; } = [];
}

public sealed record DailySales(DateTime Date,decimal Amount);
