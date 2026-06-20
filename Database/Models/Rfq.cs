using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public enum RfqStatus
{
    Open,
    Closed
}

public class Rfq
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int BuyerId { get; set; }
    
    [ForeignKey("BuyerId")]
    public User? Buyer { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    [MaxLength(50)]
    public string Unit { get; set; } = "units"; // e.g., tons, pcs, meters
    
    public decimal BudgetPrice { get; set; }
    
    [MaxLength(100)]
    public string ShippingTerms { get; set; } = string.Empty; // FOB, CIF, EXW
    
    public DateTime ExpiryDate { get; set; }
    
    public RfqStatus Status { get; set; } = RfqStatus.Open;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
