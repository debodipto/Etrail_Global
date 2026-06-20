using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public class UserSearch
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Query { get; set; } = string.Empty;

    public int SearchCount { get; set; } = 1;

    public DateTime LastSearchedAt { get; set; } = DateTime.UtcNow;
}
