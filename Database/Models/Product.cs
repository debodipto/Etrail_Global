using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class Product
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    public double Price { get; set; } = 0.0;
    
    public double DiscountPrice { get; set; } = 0.0;
    
    public int MinOrderQuantity { get; set; } = 1;
    
    public int Stock { get; set; } = 100;
    
    public int LeadTimeDays { get; set; } = 3;
    
    public string ImageUrl { get; set; } = string.Empty;

    public string MediaJson { get; set; } = "[]";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsFeatured { get; set; } = false;
    public bool IsBestSeller { get; set; } = false;
    public bool IsTodayDeal { get; set; } = false;
    
    public bool IsApproved { get; set; } = false;
    public string SpecificationsJson { get; set; } = "{}";

    public bool IsDigital { get; set; } = false;
    public string DigitalFileUrl { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string CustomLabel { get; set; } = string.Empty;
    public string AttributesJson { get; set; } = "[]";
    public string ColorsJson { get; set; } = "[]";

    [Required]
    public int SellerId { get; set; }
    
    [ForeignKey("SellerId")]
    public User? Seller { get; set; }
}
