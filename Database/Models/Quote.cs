using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public enum QuoteStatus
{
    Pending,
    Accepted,
    Rejected
}

public class Quote
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int RfqId { get; set; }
    
    [ForeignKey("RfqId")]
    public Rfq? Rfq { get; set; }
    
    [Required]
    public int SellerId { get; set; }
    
    [ForeignKey("SellerId")]
    public User? Seller { get; set; }
    
    [Required]
    public decimal ProposedPrice { get; set; }
    
    public int DeliveryTimeDays { get; set; }
    
    public string Notes { get; set; } = string.Empty;
    
    public QuoteStatus Status { get; set; } = QuoteStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
