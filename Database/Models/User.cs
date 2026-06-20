using System.ComponentModel.DataAnnotations;

namespace EtrailGlobal.Database.Models;

public enum UserRole
{
    Buyer,
    Seller,
    Admin
}

public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public UserRole Role { get; set; } = UserRole.Buyer;
    
    [MaxLength(150)]
    public string CompanyName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string BusinessType { get; set; } = string.Empty; // e.g., Manufacturer, Wholesaler, Partner
    
    [MaxLength(200)]
    public string ContactInfo { get; set; } = string.Empty;

    [MaxLength(100)]
    public string WhatsAppNumber { get; set; } = string.Empty;

    [MaxLength(150)]
    public string AlternateEmail { get; set; } = string.Empty;

    [MaxLength(100)]
    public string TaxNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string CompanyAddress { get; set; } = string.Empty;

    [MaxLength(50)]
    public string YearEstablished { get; set; } = string.Empty;

    [MaxLength(200)]
    public string WebsiteUrl { get; set; } = string.Empty;
    
    public bool IsVerified { get; set; } = false;
    
    [MaxLength(500)]
    public string BillingAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
