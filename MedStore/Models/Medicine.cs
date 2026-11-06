using System.ComponentModel.DataAnnotations;

namespace MedStore.Models;
public sealed class Medicine
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [Required] public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    [StringLength(120)] public string? Manufacturer { get; set; }
    [Required, StringLength(80)] public string BatchNumber { get; set; } = "";
    public int? SupplierId { get; set; }
    [Range(0, 1000000)] public decimal PurchasePrice { get; set; }
    [Range(0, 1000000)] public decimal SellingPrice { get; set; }
    [Range(0, 1000000)] public int Quantity { get; set; }
    [DataType(DataType.Date)] public DateTime ExpiryDate { get; set; }
    [StringLength(1000)] public string? Description { get; set; }
}

public sealed class MedicineListViewModel
{
    public List<Medicine> Items { get; set; } = [];
    public List<(int Id,string Name)> Categories { get; set; } = [];
    public string? Search { get; set; }
    public string? Manufacturer { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? ExpiryBefore { get; set; }
    public string Sort { get; set; } = "name";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}
