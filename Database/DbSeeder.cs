using System.Security.Cryptography;
using System.Text;
using EtrailGlobal.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EtrailGlobal.Database;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        // Check if database is already seeded
        if (context.Users.Any())
        {
            return; // Database has data
        }

        // 1. Create Users
        var admin = new User
        {
            Username = "Admin",
            Email = "admin@etrailglobal.com",
            PasswordHash = HashPassword("Admin123"),
            Role = UserRole.Admin,
            CompanyName = "EtrailGlobal Admin Ltd",
            BusinessType = "Platform Operator",
            ContactInfo = "admin@etrailglobal.com",
            IsVerified = true
        };

        var buyer = new User
        {
            Username = "BhadraCustomer",
            Email = "customer@etrailglobal.com",
            PasswordHash = HashPassword("Customer123"),
            Role = UserRole.Buyer,
            CompanyName = "",
            BusinessType = "",
            ContactInfo = "+880-1711-223344",
            IsVerified = true
        };

        var seller1 = new User
        {
            Username = "NexusElectronics",
            Email = "seller@etrailglobal.com",
            PasswordHash = HashPassword("Seller123"),
            Role = UserRole.Seller,
            CompanyName = "Nexus Tech & Electronics Ltd",
            BusinessType = "Manufacturer",
            ContactInfo = "sales@nexustech.example.com",
            WhatsAppNumber = "+880-1811-000001",
            AlternateEmail = "nexus.tech@gmail.com",
            TaxNumber = "TAX-NX-10928",
            CompanyAddress = "Plot 45, Sector 7, Uttara, Dhaka",
            YearEstablished = "2018",
            WebsiteUrl = "https://nexuselectronics.example.com",
            IsVerified = true
        };

        var seller2 = new User
        {
            Username = "EtrailTextiles",
            Email = "apparel@etrailglobal.com",
            PasswordHash = HashPassword("Textile123"),
            Role = UserRole.Seller,
            CompanyName = "Etrail Premium Textiles Inc",
            BusinessType = "Wholesale Exporter",
            ContactInfo = "orders@etrailtextiles.example.com",
            WhatsAppNumber = "+880-1911-000002",
            AlternateEmail = "etrail.textiles@gmail.com",
            TaxNumber = "TAX-ET-48593",
            CompanyAddress = "Baridhara DOHS, Dhaka",
            YearEstablished = "2012",
            WebsiteUrl = "https://etrailtextiles.example.com",
            IsVerified = true
        };

        var seller3 = new User
        {
            Username = "AgriPure",
            Email = "agri@etrailglobal.com",
            PasswordHash = HashPassword("Agri123"),
            Role = UserRole.Seller,
            CompanyName = "AgriPure Global Exports",
            BusinessType = "Grower & Exporter",
            ContactInfo = "trade@agripure.example.com",
            WhatsAppNumber = "+880-1711-000003",
            AlternateEmail = "agripure.bd@gmail.com",
            TaxNumber = "TAX-AP-88574",
            CompanyAddress = "Rupgonj, Narayangonj",
            YearEstablished = "2021",
            WebsiteUrl = "https://agripure.example.com",
            IsVerified = false // Unverified seller for demonstration!
        };

        context.Users.AddRange(admin, buyer, seller1, seller2, seller3);
        context.SaveChanges(); // Save to generate IDs

        // 2. Create Products
        var p1 = new Product
        {
            Name = "Dhaka Tech Industrial IoT Gateway - LTE-M / NB-IoT",
            Description = "A ruggedized industrial gateway optimized for local IoT applications. Ideal for remote logistics monitoring and factory automation in Dhaka and Chittagong zones.",
            Category = "Electronics",
            Price = 15500.00,
            DiscountPrice = 13800.00,
            MinOrderQuantity = 10,
            Stock = 120,
            LeadTimeDays = 5,
            ImageUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=600&q=80",
            SellerId = seller1.Id,
            IsFeatured = true,
            IsTodayDeal = true,
            IsApproved = true,
            SpecificationsJson = "{\"Brand\":\"Dhaka Tech\",\"Model\":\"GW-300\",\"RAM\":\"2GB\",\"Storage\":\"16GB eMMC\",\"Battery\":\"3000mAh backup\"}"
        };

        var p2 = new Product
        {
            Name = "Rugged Field Tablet (10-inch, IP67) - Bangla OS Enabled",
            Description = "MIL-STD-810G certified rugged tablet designed for industrial field audits. Waterproof, dustproof, and shockproof. Features high-brightness screen, 8GB RAM, and 128GB SSD.",
            Category = "Electronics",
            Price = 48000.00,
            DiscountPrice = 43000.00,
            MinOrderQuantity = 5,
            Stock = 45,
            LeadTimeDays = 7,
            ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?auto=format&fit=crop&w=600&q=80",
            SellerId = seller1.Id,
            IsBestSeller = true,
            IsApproved = true,
            SpecificationsJson = "{\"Brand\":\"BanglaTab\",\"Model\":\"Rugged X1\",\"RAM\":\"8GB\",\"Storage\":\"128GB SSD\",\"Battery\":\"8000mAh\"}"
        };

        var p3 = new Product
        {
            Name = "Export Quality Cotton T-Shirts (Bulk Wholesale)",
            Description = "100% GOTS-certified organic cotton t-shirts manufactured in Gazipur. Customizable company logos. Available in 12 colors and full size range.",
            Category = "Textiles & Apparel",
            Price = 480.00,
            DiscountPrice = 390.00,
            MinOrderQuantity = 100,
            Stock = 5000,
            LeadTimeDays = 15,
            ImageUrl = "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80",
            SellerId = seller2.Id,
            IsFeatured = true,
            IsBestSeller = true,
            IsApproved = true,
            SpecificationsJson = "{\"Material\":\"100% Organic Cotton\",\"Size Options\":\"S, M, L, XL, XXL\",\"Color Options\":\"Navy, Black, White, Grey\"}"
        };

        var p4 = new Product
        {
            Name = "Premium Golden Jute Fiber Sacks (Rolls & Bags)",
            Description = "High-strength natural jute sacks perfect for storing agricultural items like rice and tea. Biodegradable and environmentally friendly.",
            Category = "Textiles & Apparel",
            Price = 3400.00,
            DiscountPrice = 2900.00,
            MinOrderQuantity = 10,
            Stock = 2000,
            LeadTimeDays = 10,
            ImageUrl = "https://images.unsplash.com/photo-1543087903-1ac2ec7aa8c5?auto=format&fit=crop&w=600&q=80",
            SellerId = seller2.Id,
            IsTodayDeal = true,
            IsApproved = true,
            SpecificationsJson = "{\"Material\":\"Natural Jute Fiber\",\"Weight\":\"750g per bag\",\"Biodegradable\":\"Yes\"}"
        };

        var p5 = new Product
        {
            Name = "Premium Quality Miniket Rice (50kg Bag)",
            Description = "Aromatic double-polished premium quality Miniket rice. Aged and sortex clean. Standard 50kg wholesale packaging.",
            Category = "Agriculture & Food",
            Price = 3450.00,
            DiscountPrice = 3200.00,
            MinOrderQuantity = 20,
            Stock = 850,
            LeadTimeDays = 3,
            ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=600&q=80",
            SellerId = seller2.Id,
            IsBestSeller = true,
            IsApproved = true,
            SpecificationsJson = "{\"Type\":\"Miniket Rice\",\"Weight\":\"50kg\",\"Packaging\":\"PP Woven Bag\"}"
        };

        var p6 = new Product
        {
            Name = "CNC Fiber Laser Cutting Machine (2000W)",
            Description = "High-speed high-precision fiber laser cutting machine for metal plates. Equiped with Cypcut control system. Shipped directly to factory location.",
            Category = "Machinery & Tools",
            Price = 2950000.00,
            DiscountPrice = 2700000.00,
            MinOrderQuantity = 1,
            Stock = 12,
            LeadTimeDays = 30,
            ImageUrl = "https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?auto=format&fit=crop&w=600&q=80",
            SellerId = seller1.Id,
            IsFeatured = true,
            IsApproved = true,
            SpecificationsJson = "{\"Power\":\"2000W\",\"Bed Size\":\"3000x1500mm\",\"Laser Source\":\"Raycus\"}"
        };

        context.Products.AddRange(p1, p2, p3, p4, p5, p6);
        context.SaveChanges();

        // RFQs and Quotes are not seeded as RFQ center is disabled.
        
        // 5. Create Initial Messages
        var msg1 = new Message
        {
            SenderId = seller1.Id,
            ReceiverId = buyer.Id,
            Content = "Hello, thank you for your interest in our LTE-M / NB-IoT Industrial Gateway. We can arrange delivery to Dhaka.",
            Timestamp = DateTime.UtcNow.AddHours(-2),
            IsRead = false
        };

        var msg2 = new Message
        {
            SenderId = buyer.Id,
            ReceiverId = seller1.Id,
            Content = "Hi Nexus, thank you. What is the standard warranty period and standard lead time?",
            Timestamp = DateTime.UtcNow.AddHours(-1.5),
            IsRead = true
        };

        var msg3 = new Message
        {
            SenderId = seller1.Id,
            ReceiverId = buyer.Id,
            Content = "We provide a 2-year replacement warranty, and standard delivery takes 3 to 5 business days.",
            Timestamp = DateTime.UtcNow.AddHours(-1),
            IsRead = false
        };

        context.Messages.AddRange(msg1, msg2, msg3);
        context.SaveChanges();

        // 6. Create Banners
        var b1 = new Banner
        {
            Title = "Premium Local Customer & Wholesale Trade Portal",
            ImageUrl = "https://images.unsplash.com/photo-1578575437130-527eed3abbec?auto=format&fit=crop&w=1200&q=80",
            LinkUrl = "/product.html",
            Position = 1,
            IsActive = true
        };

        var b2 = new Banner
        {
            Title = "Connect with Verified Local Factories & Garments",
            ImageUrl = "https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?auto=format&fit=crop&w=1200&q=80",
            LinkUrl = "/product.html",
            Position = 1,
            IsActive = true
        };

        var b3 = new Banner
        {
            Title = "Compare Premium Products & Find Verified Sellers",
            ImageUrl = "https://images.unsplash.com/photo-1454165804606-c3d57bc86b40?auto=format&fit=crop&w=1200&q=80",
            LinkUrl = "/product.html",
            Position = 1,
            IsActive = true
        };

        var b4 = new Banner
        {
            Title = "Premium Quality Textiles & Jute Exporters",
            ImageUrl = "https://images.unsplash.com/photo-1543087903-1ac2ec7aa8c5?auto=format&fit=crop&w=1200&q=80",
            LinkUrl = "/product.html?category=Textiles%20%26%20Apparel",
            Position = 2,
            IsActive = true
        };

        context.Banners.AddRange(b1, b2, b3, b4);

        // 7. Create Hot Categories
        var hc1 = new HotCategory
        {
            Name = "Electronics",
            ImageUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=300&q=80",
            IsActive = true
        };

        var hc2 = new HotCategory
        {
            Name = "Textiles & Apparel",
            ImageUrl = "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=300&q=80",
            IsActive = true
        };

        var hc3 = new HotCategory
        {
            Name = "Agriculture & Food",
            ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=300&q=80",
            IsActive = true
        };

        var hc4 = new HotCategory
        {
            Name = "Machinery & Tools",
            ImageUrl = "https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?auto=format&fit=crop&w=300&q=80",
            IsActive = true
        };

        context.HotCategories.AddRange(hc1, hc2, hc3, hc4);
        context.SaveChanges();

        // 8. Create Default Site Settings
        var s1 = new SiteSetting
        {
            Key = "site_title",
            Value = "E-trail Global",
            Description = "The title/branding displayed on the top header and page footer."
        };

        var s2 = new SiteSetting
        {
            Key = "contact_phone",
            Value = "+880-9612-ETRAIL",
            Description = "Contact phone number shown on the support page and footer."
        };

        var s3 = new SiteSetting
        {
            Key = "contact_email",
            Value = "support@etrail.com.bd",
            Description = "Contact email address shown on the support page and footer."
        };

        var s4 = new SiteSetting
        {
            Key = "contact_address",
            Value = "Gulshan-2, Dhaka-1212, Bangladesh",
            Description = "Physical office address shown on the support page and footer."
        };

        var s5 = new SiteSetting
        {
            Key = "policy_terms",
            Value = "<h3>1. Customer Terms</h3><p>All sales and purchase queries on E-trail Global constitute raw inquiries. Agreements closed between customers and sellers are direct contracts under their respective domestic jurisdictions in Bangladesh.</p><h3>2. Audited Manufacturer Badging</h3><p>The \"Audited Manufacturer\" badge is awarded by platform administration based on supplier license documentation and factory reports in Bangladesh. It is not an insurance policy.</p><h3>3. Payment Terms</h3><p>E-trail Global recommends using standard secure payment instruments for high-volume transactions.</p>",
            Description = "Terms & Conditions policy page markup text."
        };

        var s6 = new SiteSetting
        {
            Key = "policy_return",
            Value = "<h3>1. Quality Audits</h3><p>Returns on orders are subject to the individual agreements established between the Customer and Seller during the purchase phase.</p><h3>2. Shipping Disputes</h3><p>Disputes relating to cargo damage, weight discrepancies, or transit issues are resolved under standard domestic carrier rules.</p>",
            Description = "Return & Settlement policy page markup text."
        };

        var s7 = new SiteSetting
        {
            Key = "policy_support",
            Value = "<h3>1. Customer Support Assistance</h3><p>Our helpdesk assists Customers in managing their profile and billing details, and helps Sellers upload pricing matrices and catalogs.</p><h3>2. Contact Information</h3><p>For escalation issues, email support@etrail.com.bd or call our business desk at +880-9612-ETRAIL (387245).</p>",
            Description = "Support policy page markup text."
        };

        var s8 = new SiteSetting
        {
            Key = "policy_privacy",
            Value = "<h3>1. Personal Data Confidentiality</h3><p>Negotiations, specifications, and messages exchanged between users are kept strictly confidential and stored on encrypted databases.</p><h3>2. Document Security</h3><p>Any billing details or verification documents uploaded are used solely for internal audits.</p>",
            Description = "Privacy & Data Security policy page markup text."
        };

        var s9 = new SiteSetting
        {
            Key = "currency_symbol",
            Value = "৳",
            Description = "The currency symbol used across product catalog prices and budgets (e.g. ৳)."
        };

        context.SiteSettings.AddRange(s1, s2, s3, s4, s5, s6, s7, s8, s9);
        context.SaveChanges();

        // 9. Update Buyer Addresses
        buyer.BillingAddress = "Holding 12, Road 4, Sector 3, Uttara, Dhaka-1230, Bangladesh";
        buyer.ShippingAddress = "Plot 45, Tejgaon Industrial Area, Tejgaon, Dhaka-1208, Bangladesh";
        context.Users.Update(buyer);

        // 10. Seed Customer Entities
        // Seed Cart items
        context.CartItems.AddRange(
            new CartItem { UserId = buyer.Id, ProductId = p5.Id, Quantity = 20 }, // Miniket Rice
            new CartItem { UserId = buyer.Id, ProductId = p4.Id, Quantity = 10 }, // Jute Fiber Sacks
            new CartItem { UserId = buyer.Id, ProductId = p2.Id, Quantity = 5 }    // Rugged Field Tablet
        );

        // Seed Wishlist items
        context.WishlistItems.AddRange(
            new WishlistItem { UserId = buyer.Id, ProductId = p2.Id }, // Field Tablet
            new WishlistItem { UserId = buyer.Id, ProductId = p3.Id }  // Cotton T-Shirts
        );

        // Seed Compare items
        context.CompareItems.AddRange(
            new CompareItem { UserId = buyer.Id, ProductId = p2.Id }, // Field Tablet
            new CompareItem { UserId = buyer.Id, ProductId = p6.Id }  // CNC Laser Machine
        );

        // Seed Downloads
        context.Downloads.AddRange(
            new Download 
            { 
                UserId = buyer.Id, 
                FileName = "Etrail Global B2B Buying Guide.pdf", 
                FileUrl = "/downloads/buying_guide.pdf", 
                FileSize = "1.2 MB", 
                DownloadDate = DateTime.UtcNow.AddDays(-5) 
            },
            new Download 
            { 
                UserId = buyer.Id, 
                FileName = "Invoice-ET-99481.pdf", 
                FileUrl = "/downloads/Invoice-ET-99481.pdf", 
                FileSize = "340 KB", 
                DownloadDate = DateTime.UtcNow.AddDays(-18) 
            }
        );

        // Seed Followed Sellers
        context.FollowedSellers.AddRange(
            new FollowedSeller { UserId = buyer.Id, SellerId = seller1.Id }, // Nexus Electronics
            new FollowedSeller { UserId = buyer.Id, SellerId = seller2.Id }  // Etrail Textiles
        );

        // Seed Support Tickets
        context.SupportTickets.AddRange(
            new SupportTicket 
            { 
                UserId = buyer.Id, 
                Subject = "Address Verification Inquiry", 
                Description = "Need help updating my delivery address in Dhaka. The system keeps saving it with a warning.", 
                Priority = "Medium", 
                Status = "Open", 
                CreatedAt = DateTime.UtcNow.AddDays(-2) 
            }
        );

        // Seed Orders
        context.Orders.AddRange(
            new Order 
            { 
                UserId = buyer.Id, 
                OrderNumber = "ET-99481", 
                OrderDate = DateTime.UtcNow.AddDays(-19), 
                TotalAmount = 48000.00m, 
                Status = "Delivered", 
                PaymentMethod = "Cash on Delivery", 
                BillingAddress = "Holding 12, Road 4, Sector 3, Uttara, Dhaka-1230, Bangladesh", 
                ShippingAddress = "Plot 45, Tejgaon Industrial Area, Tejgaon, Dhaka-1208, Bangladesh", 
                ItemDetailsJson = "[{\"Name\": \"Export Quality Cotton T-Shirts (Bulk Wholesale)\", \"Qty\": 100, \"Price\": 480.00, \"ImageUrl\": \"https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=600&q=80\"}]" 
            },
            new Order 
            { 
                UserId = buyer.Id, 
                OrderNumber = "ET-98124", 
                OrderDate = DateTime.UtcNow.AddDays(-10), 
                TotalAmount = 34500.00m, 
                Status = "Shipped", 
                PaymentMethod = "Bank Transfer", 
                BillingAddress = "Holding 12, Road 4, Sector 3, Uttara, Dhaka-1230, Bangladesh", 
                ShippingAddress = "Plot 45, Tejgaon Industrial Area, Tejgaon, Dhaka-1208, Bangladesh", 
                ItemDetailsJson = "[{\"Name\": \"Premium Quality Miniket Rice (50kg Bag)\", \"Qty\": 10, \"Price\": 3450.00, \"ImageUrl\": \"https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=600&q=80\"}]" 
            }
        );

        // Seed Seller Reviews
        context.SellerReviews.AddRange(
            new SellerReview { BuyerId = buyer.Id, SellerId = seller1.Id, Rating = 5, Comment = "Nexus Tech provides top notch quality IoT hardware. Very reliable supplier in Dhaka!", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new SellerReview { BuyerId = buyer.Id, SellerId = seller1.Id, Rating = 4, Comment = "Good support and fast delivery for rugged field tablets. Recommended.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new SellerReview { BuyerId = buyer.Id, SellerId = seller2.Id, Rating = 5, Comment = "Excellent export quality garments and jute products.", CreatedAt = DateTime.UtcNow.AddDays(-4) }
        );

        context.SaveChanges();

        // 11. Seed Brands
        var bBrand1 = new Brand { Name = "Walton", LogoUrl = "/uploads/walton_logo.png" };
        var bBrand2 = new Brand { Name = "Apex", LogoUrl = "/uploads/apex_logo.png" };
        var bBrand3 = new Brand { Name = "NexusTech", LogoUrl = "/uploads/nexus_logo.png" };
        var bBrand4 = new Brand { Name = "EtrailApparel", LogoUrl = "/uploads/etrail_logo.png" };
        context.Brands.AddRange(bBrand1, bBrand2, bBrand3, bBrand4);

        // 12. Seed Categories
        var cat1 = new Category { Name = "Electronics", DiscountPercentage = 5.0, BannerUrl = "/uploads/electronics_banner.png" };
        var cat2 = new Category { Name = "Textiles & Apparel", DiscountPercentage = 10.0, BannerUrl = "/uploads/textiles_banner.png" };
        var cat3 = new Category { Name = "Agriculture & Food", DiscountPercentage = 0.0, BannerUrl = "/uploads/agri_banner.png" };
        var cat4 = new Category { Name = "Machinery & Tools", DiscountPercentage = 2.0, BannerUrl = "/uploads/machinery_banner.png" };
        var cat5 = new Category { Name = "Digital Services", DiscountPercentage = 15.0, BannerUrl = "/uploads/digital_banner.png" };
        context.Categories.AddRange(cat1, cat2, cat3, cat4, cat5);

        // 13. Seed Colors
        var col1 = new Color { Name = "Navy Blue", Code = "#000080" };
        var col2 = new Color { Name = "Charcoal Black", Code = "#36454F" };
        var col3 = new Color { Name = "Crimson Red", Code = "#DC143C" };
        var col4 = new Color { Name = "Emerald Green", Code = "#50C878" };
        context.Colors.AddRange(col1, col2, col3, col4);

        // 14. Seed ProductAttributes
        var att1 = new ProductAttribute { Name = "Size", ValuesJson = "[\"S\", \"M\", \"L\", \"XL\"]" };
        var att2 = new ProductAttribute { Name = "RAM", ValuesJson = "[\"4GB\", \"8GB\", \"16GB\"]" };
        var att3 = new ProductAttribute { Name = "Storage", ValuesJson = "[\"128GB\", \"256GB\", \"512GB\"]" };
        context.ProductAttributes.AddRange(att1, att2, att3);

        // 15. Seed SmartBarSettings
        context.SmartBarSettings.Add(new SmartBarSetting 
        { 
            AnnouncementText = "🎉 Eid Special Discount: Get up to 15% discount on Electronics and Textiles!", 
            BgColor = "#146c43", 
            TextColor = "#ffffff", 
            Link = "/flash-deals.html", 
            IsActive = true 
        });

        // 16. Seed UserSearches
        context.UserSearches.AddRange(
            new UserSearch { Query = "IoT Gateway", SearchCount = 42, LastSearchedAt = DateTime.UtcNow.AddHours(-1) },
            new UserSearch { Query = "Cotton T-Shirts", SearchCount = 98, LastSearchedAt = DateTime.UtcNow.AddHours(-3) },
            new UserSearch { Query = "Miniket Rice", SearchCount = 57, LastSearchedAt = DateTime.UtcNow.AddHours(-4) },
            new UserSearch { Query = "Laser Cutting Machine", SearchCount = 15, LastSearchedAt = DateTime.UtcNow.AddHours(-8) }
        );

        // 17. Seed WalletRecharges
        context.WalletRecharges.AddRange(
            new WalletRecharge { UserId = buyer.Id, Amount = 5000.00m, PaymentMethod = "bKash", Status = "Success", CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new WalletRecharge { UserId = buyer.Id, Amount = 15000.00m, PaymentMethod = "Nagad", Status = "Success", CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new WalletRecharge { UserId = buyer.Id, Amount = 2000.00m, PaymentMethod = "Rocket", Status = "Pending", CreatedAt = DateTime.UtcNow.AddHours(-2) }
        );

        // Update buyer wallet balance
        buyer.WalletBalance = 20000.00m;
        context.Users.Update(buyer);

        // 18. Seed PayoutRequests
        context.PayoutRequests.AddRange(
            new PayoutRequest { SellerId = seller1.Id, Amount = 15000.00m, Status = "Approved", Note = "Monthly settlement payout.", CreatedAt = DateTime.UtcNow.AddDays(-12) },
            new PayoutRequest { SellerId = seller1.Id, Amount = 8500.00m, Status = "Pending", Note = "Request for urgent cashflow.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new PayoutRequest { SellerId = seller2.Id, Amount = 35000.00m, Status = "Pending", Note = "Standard payout request.", CreatedAt = DateTime.UtcNow.AddHours(-5) }
        );

        // 19. Seed Category & Seller commission rules
        context.CategoryCommissionRules.AddRange(
            new CategoryCommissionRule { CategoryName = "Electronics", CommissionPercentage = 8.0 },
            new CategoryCommissionRule { CategoryName = "Textiles & Apparel", CommissionPercentage = 5.0 }
        );

        seller1.CommissionRate = 7.5; // Custom seller-specific commission rate override (7.5%)
        context.Users.Update(seller1);

        // 20. Add In-house product and Digital product
        var inHouseProd = new Product
        {
            Name = "Etrail Premium Tea Leaves (In-house Spec)",
            Description = "Directly sourced premium tea leaves from Sylhet gardens. Packed and distributed by Etrail Global.",
            Category = "Agriculture & Food",
            Price = 350.00,
            DiscountPrice = 320.00,
            MinOrderQuantity = 5,
            Stock = 800,
            LeadTimeDays = 2,
            ImageUrl = "https://images.unsplash.com/photo-1597481499750-3e6b22637e12?auto=format&fit=crop&w=600&q=80",
            SellerId = admin.Id, // Admin is the seller (In-house)
            IsFeatured = true,
            IsApproved = true,
            Brand = "Etrail",
            CustomLabel = "In House Best",
            SpecificationsJson = "{\"Origin\":\"Sylhet\",\"Organic\":\"Yes\",\"Grade\":\"FTGFOP\"}"
        };

        var digitalProd = new Product
        {
            Name = "Etrail Global Logistics Guide & Matrix (Digital)",
            Description = "A complete downloadable guide detailing logistics pathways, carrier rates, and customs guidelines in Bangladesh.",
            Category = "Digital Services",
            Price = 2500.00,
            DiscountPrice = 1900.00,
            MinOrderQuantity = 1,
            Stock = 99999, // digital has unlimited stock
            LeadTimeDays = 0,
            ImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?auto=format&fit=crop&w=600&q=80",
            SellerId = admin.Id, // In-house digital product
            IsApproved = true,
            IsDigital = true,
            DigitalFileUrl = "/uploads/Etrail_Logistics_Guide_2026.pdf",
            Brand = "Etrail",
            CustomLabel = "Digital Ebook",
            SpecificationsJson = "{\"Pages\":\"148\",\"Format\":\"PDF\",\"Published\":\"2026\"}"
        };

        var sellerDigitalProd = new Product
        {
            Name = "Nexus IoT Gateway Firmware v4.0 (Seller Digital)",
            Description = "Upgrade package for Nexus IoT gateway devices containing advanced cellular fallback configuration and OTA capability.",
            Category = "Digital Services",
            Price = 7500.00,
            DiscountPrice = 6500.00,
            MinOrderQuantity = 1,
            Stock = 999,
            LeadTimeDays = 0,
            ImageUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=600&q=80",
            SellerId = seller1.Id,
            IsApproved = true,
            IsDigital = true,
            DigitalFileUrl = "/uploads/nexus_fw_v4.bin",
            Brand = "NexusTech",
            CustomLabel = "Firmware Update",
            SpecificationsJson = "{\"Version\":\"v4.0\",\"Target\":\"GW-300\",\"Size\":\"14MB\"}"
        };

        context.Products.AddRange(inHouseProd, digitalProd, sellerDigitalProd);

        // Update existing products with Brand & Colors
        p1.Brand = "NexusTech";
        p1.ColorsJson = "[\"Charcoal Black\"]";
        p1.AttributesJson = "[\"RAM\",\"Storage\"]";

        p2.Brand = "NexusTech";
        p2.ColorsJson = "[\"Charcoal Black\"]";
        p2.AttributesJson = "[\"RAM\",\"Storage\"]";

        p3.Brand = "EtrailApparel";
        p3.ColorsJson = "[\"Navy Blue\",\"Charcoal Black\",\"Crimson Red\"]";
        p3.AttributesJson = "[\"Size\"]";

        p4.Brand = "EtrailApparel";
        p4.ColorsJson = "[\"Emerald Green\"]";

        p5.Brand = "AgriPure";
        
        p6.Brand = "NexusTech";

        context.Products.UpdateRange(p1, p2, p3, p4, p5, p6);
        context.SaveChanges();

        // 21. Update and Seed Orders with detailed flags
        var ordersList = context.Orders.ToList();
        if (ordersList.Count >= 2)
        {
            // First order: Delivered, Paid, InHouse
            ordersList[0].IsPaid = true;
            ordersList[0].OrderType = "Seller"; // from NexusElectronics
            ordersList[0].CommissionEarned = 3600.00m; // 7.5% of 48000
            ordersList[0].PickupPoint = "";

            // Second order: Shipped, Bank Transfer, Seller, Pickup point
            ordersList[1].IsPaid = true;
            ordersList[1].OrderType = "Seller";
            ordersList[1].CommissionEarned = 1725.00m; // 5% of 34500
            ordersList[1].PickupPoint = "Uttara Sector 7 Pickup Point";

            context.Orders.UpdateRange(ordersList);

            // Add commission histories
            context.CommissionHistories.AddRange(
                new CommissionHistory { OrderId = ordersList[0].Id, SellerId = seller1.Id, Amount = 48000.00m, CommissionAmount = 3600.00m, CreatedAt = DateTime.UtcNow.AddDays(-19) },
                new CommissionHistory { OrderId = ordersList[1].Id, SellerId = seller2.Id, Amount = 34500.00m, CommissionAmount = 1725.00m, CreatedAt = DateTime.UtcNow.AddDays(-10) }
            );
        }

        // Add an unpaid, in-house order
        var unpaidOrder = new Order
        {
            UserId = buyer.Id,
            OrderNumber = "ET-91102",
            OrderDate = DateTime.UtcNow.AddDays(-1),
            TotalAmount = 640.00m,
            Status = "Pending",
            PaymentMethod = "Cash on Delivery",
            BillingAddress = "Holding 12, Road 4, Sector 3, Uttara, Dhaka-1230, Bangladesh",
            ShippingAddress = "Holding 12, Road 4, Sector 3, Uttara, Dhaka-1230, Bangladesh",
            ItemDetailsJson = "[{\"Name\": \"Etrail Premium Tea Leaves (In-house Spec)\", \"Qty\": 2, \"Price\": 320.00, \"ImageUrl\": \"https://images.unsplash.com/photo-1597481499750-3e6b22637e12?auto=format&fit=crop&w=600&q=80\"}]",
            IsPaid = false,
            OrderType = "InHouse",
            PickupPoint = "",
            CommissionEarned = 0.0m // InHouse has no commission
        };
        context.Orders.Add(unpaidOrder);

        context.SaveChanges();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}



