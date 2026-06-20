using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class SupportTicket
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User? User { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Open"; // Open, In Progress, Closed
    
    [Required]
    [MaxLength(50)]
    public string Priority { get; set; } = "Low"; // Low, Medium, High
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
