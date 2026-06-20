using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public class Color
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty; // Hex code, e.g. #FF0000
}
