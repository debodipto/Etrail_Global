using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User? User { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;
    
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // e.g. Pending, Processing, Shipped, Delivered
    
    [Required]
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = "Cash on Delivery";
    
    [Required]
    [MaxLength(500)]
    public string BillingAddress { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;
    
    // JSON-serialized string of purchased item details to keep it simpler
    // e.g. [{"Name": "Product Name", "Qty": 2, "Price": 5.50, "ImageUrl": "url"}]
    public string ItemDetailsJson { get; set; } = "[]";
}
