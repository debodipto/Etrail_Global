using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class Message
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int SenderId { get; set; }
    
    [ForeignKey("SenderId")]
    public User? Sender { get; set; }
    
    [Required]
    public int ReceiverId { get; set; }
    
    [ForeignKey("ReceiverId")]
    public User? Receiver { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public bool IsRead { get; set; } = false;
}
