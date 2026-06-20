using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class FollowedSeller
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User? User { get; set; }
    
    [Required]
    public int SellerId { get; set; }
    
    [ForeignKey("SellerId")]
    public User? Seller { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
