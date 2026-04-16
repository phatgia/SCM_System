using Bogus;
using Microsoft.EntityFrameworkCore;
using SCM_System.Models;
using System;
using System.Linq;

namespace SCM_System.Data
{
    public static class DbInitializer
    {
        public static void Initialize(SCMDbContext context)
        {
            // Apply any pending migrations automatically, or create DB if not exists
            context.Database.Migrate();

            // 1. Seed Roles
            if (!context.Roles.Any())
            {
                var roles = new Role[]
                {
                    new Role { RoleName = "Quản trị viên" },
                    new Role { RoleName = "Nhân viên bán hàng" },
                    new Role { RoleName = "Nhân viên mua hàng" },
                    new Role { RoleName = "Quản lý kho" },
                    new Role { RoleName = "Nhân viên vận chuyển" }
                };
                context.Roles.AddRange(roles);
                context.SaveChanges();
            }

            // 2. Seed Categories
            if (!context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category { CategoryName = "Electronics", Description = "Tivi, Tủ Lạnh, Điện thoại..." },
                    new Category { CategoryName = "Furniture", Description = "Bàn, Nhế, Tủ..." },
                    new Category { CategoryName = "Clothing", Description = "Quần áo thời trang" }
                };
                context.Categories.AddRange(categories);
                context.SaveChanges();
            }

            // 3. Seed Suppliers using Bogus
            if (!context.Suppliers.Any())
            {
                var supplierFaker = new Faker<Supplier>("vi")
                    .RuleFor(s => s.SupplierName, f => f.Company.CompanyName())
                    .RuleFor(s => s.ContactPerson, f => f.Person.FullName)
                    .RuleFor(s => s.Phone, f => f.Phone.PhoneNumber("09########"))
                    .RuleFor(s => s.Email, f => f.Internet.Email())
                    .RuleFor(s => s.Address, f => f.Address.FullAddress());

                var suppliers = supplierFaker.Generate(10); // Generate 10 fake suppliers
                context.Suppliers.AddRange(suppliers);
                context.SaveChanges();
            }
            
            // 4. Seed Products using Bogus
            if (!context.Products.Any())
            {
                // We need valid category ids
                var categoryIds = context.Categories.Select(c => c.CategoryID).ToList();

                var productFaker = new Faker<Product>("vi")
                    .RuleFor(p => p.ProductName, f => f.Commerce.ProductName())
                    .RuleFor(p => p.Unit, f => f.PickRandom(new[] { "Cái", "Chiếc", "Hộp", "Bộ" }))
                    .RuleFor(p => p.WarrantyMonths, f => f.Random.Int(6, 36))
                    .RuleFor(p => p.CategoryID, f => f.PickRandom(categoryIds));
                
                var products = productFaker.Generate(20);
                context.Products.AddRange(products);
                context.SaveChanges();
            }

            // 5. Seed Users
            if (!context.Users.Any())
            {
                var roleAdmin = context.Roles.FirstOrDefault(r => r.RoleName == "Quản trị viên")?.RoleID ?? 1;
                var roleSale = context.Roles.FirstOrDefault(r => r.RoleName == "Nhân viên bán hàng")?.RoleID ?? 2;
                var rolePurchase = context.Roles.FirstOrDefault(r => r.RoleName == "Nhân viên mua hàng")?.RoleID ?? 3;
                var roleWarehouse = context.Roles.FirstOrDefault(r => r.RoleName == "Quản lý kho")?.RoleID ?? 4;
                var roleDelivery = context.Roles.FirstOrDefault(r => r.RoleName == "Nhân viên vận chuyển")?.RoleID ?? 5;

                // Create fixed test accounts (Password is "123" for easy login testing)
                var testUsers = new User[]
                {
                    new User { RoleID = roleAdmin, FullName = "Nguyễn Admin", Username = "admin", Password = "123", Email = "admin@scm.com", PhoneNumber = "0987654321" },
                    new User { RoleID = roleSale, FullName = "Trần Bán Hàng", Username = "sale", Password = "123", Email = "sale@scm.com", PhoneNumber = "0987654322" },
                    new User { RoleID = rolePurchase, FullName = "Lê Mua Hàng", Username = "purchase", Password = "123", Email = "purchase@scm.com", PhoneNumber = "0987654323" },
                    new User { RoleID = roleWarehouse, FullName = "Phạm Thủ Kho", Username = "warehouse", Password = "123", Email = "warehouse@scm.com", PhoneNumber = "0987654324" },
                    new User { RoleID = roleDelivery, FullName = "Hoàng Vận Chuyển", Username = "delivery", Password = "123", Email = "delivery@scm.com", PhoneNumber = "0987654325" }
                };

                context.Users.AddRange(testUsers);
                context.SaveChanges();
                
                // Add some random users (using Bogus) for Sales and Delivery to bulk up data
                var userFaker = new Faker<User>("vi")
                    .RuleFor(u => u.RoleID, f => f.PickRandom(new[] { roleSale, roleDelivery }))
                    .RuleFor(u => u.FullName, f => f.Person.FullName)
                    .RuleFor(u => u.Username, (f, u) => f.Internet.UserName(u.FullName))
                    .RuleFor(u => u.Password, f => "123")
                    .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FullName))
                    .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("09########"));

                var randomUsers = userFaker.Generate(10);
                context.Users.AddRange(randomUsers);
                context.SaveChanges();
            }
        }
    }
}
