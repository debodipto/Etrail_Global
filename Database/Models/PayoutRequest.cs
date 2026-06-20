using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class PayoutRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SellerId { get; set; }

    [ForeignKey("SellerId")]
    public User? Seller { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
