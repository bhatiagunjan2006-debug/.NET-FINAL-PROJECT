using System.ComponentModel.DataAnnotations;

namespace MedStore.Models;

public sealed class BusinessRecord
{
    public int Id { get; set; }
    [Required, StringLength(120)]
    public string Name { get; set; } = "";
    [StringLength(32)]
    public string? Contact { get; set; }
    [StringLength(80)]
    public string? Username { get; set; }
    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }
    [StringLength(400)]
    public string? Address { get; set; }
    [StringLength(120)]
    public string? Company { get; set; }
    [StringLength(80)]
    public string? Designation { get; set; }
    [Range(0,100000000)]
    public decimal Amount { get; set; }
    [Range(0,100000000)]
    public decimal Allowances { get; set; }
    [Range(0,100000000)]
    public decimal Deductions { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public string Role { get; set; } = "Billing Staff";
    [Range(0,5)] public int Rating { get; set; }
}

public sealed class ModuleField(string key, string label, string type = "text", bool required = false)
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    public string Type { get; } = type;
    public bool Required { get; } = required;
}

public sealed class ModulePageViewModel
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ModuleField> Fields { get; set; } = [];
    public List<BusinessRecord> Rows { get; set; } = [];
    public bool CanEdit { get; set; }
    public bool IsLeave { get; set; }
}

public sealed class ModuleFormViewModel
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public List<ModuleField> Fields { get; set; } = [];
    public BusinessRecord Record { get; set; } = new();
}

public sealed class LeaveCalendarViewModel
{
    public DateTime Month { get; set; }
    public List<DateTime> Days { get; set; } = [];
    public List<BusinessRecord> Leaves { get; set; } = [];
}

public sealed class StockAdjustmentInput
{
    [Range(1, int.MaxValue)] public int MedicineId { get; set; }
    [Range(-100000, 100000)] public int Change { get; set; }
    [Required, StringLength(200)] public string Note { get; set; } = "";
}

public sealed class TransactionLineInput
{
    [Range(1, int.MaxValue)] public int MedicineId { get; set; }
    [Range(1, 100000)] public int Quantity { get; set; } = 1;
}

public sealed class TransactionInput
{
    public string Kind { get; set; } = "sale";
    public int? PartnerId { get; set; }
    [Range(0, 100000000)] public decimal Discount { get; set; }
    public List<TransactionLineInput> Lines { get; set; } = [new()];
}

public sealed class TransactionLine
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

public sealed class StoreTransaction
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Now;
    public string PartnerName { get; set; } = "Walk-in customer";
    public string? PartnerContact { get; set; }
    public string? PartnerAddress { get; set; }
    public string Status { get; set; } = "Completed";
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public List<TransactionLine> Lines { get; set; } = [];
}

public sealed class TransactionPageViewModel
{
    public string Kind { get; set; } = "sale";
    public List<StoreTransaction> Transactions { get; set; } = [];
    public List<Medicine> Medicines { get; set; } = [];
    public List<BusinessRecord> Partners { get; set; } = [];
}

public sealed class InvoiceViewModel
{
    public StoreTransaction Transaction { get; set; } = new();
    public string StoreName { get; set; } = "MediStock Medical Store";
    public string StoreAddress { get; set; } = "";
    public string StoreContact { get; set; } = "";
    public string StoreEmail { get; set; } = "";
    public bool IsPurchase { get; set; }
}

public sealed class MovementRecord
{
    public DateTime Date { get; set; } = DateTime.Now;
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = "";
    public string Type { get; set; } = "Adjustment";
    public int Change { get; set; }
    public string Note { get; set; } = "";
}
