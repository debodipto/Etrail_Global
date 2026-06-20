using Microsoft.EntityFrameworkCore;
using EtrailGlobal.Database.Models;

namespace EtrailGlobal.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Rfq> Rfqs { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<HotCategory> HotCategories { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<CompareItem> CompareItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Download> Downloads { get; set; }
    public DbSet<FollowedSeller> FollowedSellers { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<SellerReview> SellerReviews { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ProductAttribute> ProductAttributes { get; set; }
    public DbSet<Color> Colors { get; set; }
    public DbSet<PayoutRequest> PayoutRequests { get; set; }
    public DbSet<CategoryCommissionRule> CategoryCommissionRules { get; set; }
    public DbSet<SmartBarSetting> SmartBarSettings { get; set; }
    public DbSet<UserSearch> UserSearches { get; set; }
    public DbSet<WalletRecharge> WalletRecharges { get; set; }
    public DbSet<CommissionHistory> CommissionHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Define relations to avoid multiple cascade paths for messages
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Delete cascade rules for Quote and RFQ
        modelBuilder.Entity<Quote>()
            .HasOne(q => q.Rfq)
            .WithMany()
            .HasForeignKey(q => q.RfqId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Quote>()
            .HasOne(q => q.Seller)
            .WithMany()
            .HasForeignKey(q => q.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Delete cascade rules for Product and Seller
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Seller)
            .WithMany()
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Delete cascade rules for RFQ and Buyer
        modelBuilder.Entity<Rfq>()
            .HasOne(r => r.Buyer)
            .WithMany()
            .HasForeignKey(r => r.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cascade delete configurations for new customer entities
        modelBuilder.Entity<CartItem>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WishlistItem>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompareItem>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Download>()
            .HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupportTicket>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FollowedSeller>()
            .HasOne(fs => fs.User)
            .WithMany()
            .HasForeignKey(fs => fs.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FollowedSeller>()
            .HasOne(fs => fs.Seller)
            .WithMany()
            .HasForeignKey(fs => fs.SellerId)
            .OnDelete(DeleteBehavior.Restrict); // restrict to prevent multiple cascades path

        modelBuilder.Entity<SellerReview>()
            .HasOne(sr => sr.Buyer)
            .WithMany()
            .HasForeignKey(sr => sr.BuyerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SellerReview>()
            .HasOne(sr => sr.Seller)
            .WithMany()
            .HasForeignKey(sr => sr.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductReview>()
            .HasOne(pr => pr.Buyer)
            .WithMany()
            .HasForeignKey(pr => pr.BuyerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductReview>()
            .HasOne(pr => pr.Product)
            .WithMany()
            .HasForeignKey(pr => pr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PayoutRequest>()
            .HasOne(pr => pr.Seller)
            .WithMany()
            .HasForeignKey(pr => pr.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WalletRecharge>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommissionHistory>()
            .HasOne(ch => ch.Order)
            .WithMany()
            .HasForeignKey(ch => ch.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommissionHistory>()
            .HasOne(ch => ch.Seller)
            .WithMany()
            .HasForeignKey(ch => ch.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
