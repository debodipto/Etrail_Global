using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtrailGlobal.Database.Models;

public class ProductReview
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int BuyerId { get; set; }
    
    [ForeignKey("BuyerId")]
    public User? Buyer { get; set; }
    
    [Required]
    public int ProductId { get; set; }
    
    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
    
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
