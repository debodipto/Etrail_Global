using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public class CategoryCommissionRule
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public double CommissionPercentage { get; set; } = 0.0;
}
