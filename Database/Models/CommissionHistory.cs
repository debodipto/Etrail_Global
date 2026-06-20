using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class CommissionHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [Required]
    public int SellerId { get; set; }

    [ForeignKey("SellerId")]
    public User? Seller { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
