using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class Download
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User? User { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string FileUrl { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string FileSize { get; set; } = string.Empty;
    
    public DateTime DownloadDate { get; set; } = DateTime.UtcNow;
}
