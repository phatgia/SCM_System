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

            // 6. Seed Customers
            if (!context.Customers.Any())
            {
                var customerFaker = new Faker<Customer>("vi")
                    .RuleFor(c => c.Name, f => f.Person.FullName)
                    .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber("08########"))
                    .RuleFor(c => c.Email, f => f.Internet.Email())
                    .RuleFor(c => c.ShippingAddress, f => f.Address.FullAddress());

                context.Customers.AddRange(customerFaker.Generate(15));
                context.SaveChanges();
            }

            // 7. Seed SaleOrders & Details (History for last 6 months)
            if (!context.SaleOrders.Any())
            {
                var customerIds = context.Customers.Select(c => c.CustomerID).ToList();
                var userIds = context.Users.Select(u => u.UserID).ToList();
                var productIds = context.Products.Select(p => p.ProductID).ToList();

                var statuses = new[] { "Hoàn thành", "Đã giao", "Đang xử lý", "Đã hủy" };

                for (int i = 0; i < 100; i++)
                {
                    var faker = new Faker();
                    var orderDate = faker.Date.Between(DateTime.Now.AddMonths(-6), DateTime.Now);
                    var status = faker.PickRandom(statuses);

                    var order = new SaleOrder
                    {
                        CustomerID = faker.PickRandom(customerIds),
                        UserID = faker.PickRandom(userIds),
                        OrderDate = orderDate,
                        Status = status,
                        TotalAmount = 0 // Will update after details
                    };

                    context.SaleOrders.Add(order);
                    context.SaveChanges();

                    // Details
                    decimal total = 0;
                    int itemsCount = faker.Random.Int(1, 3);
                    for (int j = 0; j < itemsCount; j++)
                    {
                        var price = faker.Random.Decimal(1000000, 20000000);
                        var qty = faker.Random.Int(1, 2);
                        var detail = new SaleOrderDetail
                        {
                            SOID = order.SOID,
                            ProductID = faker.PickRandom(productIds),
                            Quantity = qty,
                            UnitPrice = price
                        };
                        context.SaleOrderDetails.Add(detail);
                        total += (price * qty);
                    }
                    order.TotalAmount = total;
                    context.SaveChanges();

                    // If order is "Hoàn thành" or "Đã giao", create a Delivery
                    if (status == "Hoàn thành" || status == "Đã giao")
                    {
                        context.Deliveries.Add(new Delivery
                        {
                            SOID = order.SOID,
                            UserID = faker.PickRandom(userIds),
                            Status = "Thành công",
                            DeliveryTime = orderDate.AddDays(2)
                        });
                    }
                }
                context.SaveChanges();

                // 8. Seed some ReturnOrders
                var completedOrderIds = context.SaleOrders.Where(o => o.Status == "Hoàn thành").Select(o => o.SOID).Take(5).ToList();
                foreach (var soid in completedOrderIds)
                {
                    context.ReturnOrders.Add(new ReturnOrder
                    {
                        SOID = soid,
                        UserID = context.Users.First().UserID,
                        Reason = "Khách đổi ý",
                        Settlement = "Hoàn tiền",
                        Status = "Hoàn thành"
                    });
                }
                context.SaveChanges();
            }

            // 9. Seed PurchaseOrders & Details (History for last 6 months)
            if (!context.PurchaseOrders.Any())
            {
                var supplierIds = context.Suppliers.Select(s => s.SupplierID).ToList();
                var userIds = context.Users.Select(u => u.UserID).ToList();
                var productIds = context.Products.Select(p => p.ProductID).ToList();

                for (int i = 0; i < 50; i++)
                {
                    var faker = new Faker();
                    var orderDate = faker.Date.Between(DateTime.Now.AddMonths(-6), DateTime.Now);
                    
                    var order = new PurchaseOrder
                    {
                        SupplierID = faker.PickRandom(supplierIds),
                        UserID = faker.PickRandom(userIds),
                        OrderDate = orderDate,
                        ExpectedDeliveryDate = orderDate.AddDays(7),
                        Status = "Hoàn thành",
                        TotalAmount = 0
                    };

                    context.PurchaseOrders.Add(order);
                    context.SaveChanges();

                    decimal total = 0;
                    int itemsCount = faker.Random.Int(2, 5);
                    for (int j = 0; j < itemsCount; j++)
                    {
                        var price = faker.Random.Decimal(500000, 15000000);
                        var qty = faker.Random.Int(10, 50);
                        var detail = new PurchaseOrderDetail
                        {
                            POID = order.POID,
                            ProductID = faker.PickRandom(productIds),
                            Quantity = qty,
                            UnitPrice = price
                        };
                        context.PurchaseOrderDetails.Add(detail);
                        total += (price * qty);
                    }
                    order.TotalAmount = total;
                    context.SaveChanges();
                }
            }
        }
    }
}
