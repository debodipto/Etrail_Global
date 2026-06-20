using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class WalletRecharge
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Bkash";

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Success"; // Success, Pending, Failed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
