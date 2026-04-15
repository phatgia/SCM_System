using Microsoft.EntityFrameworkCore;
using SCM_System.Models;

namespace SCM_System.Data
{
    public class SCMDbContext : DbContext
    {
        public SCMDbContext(DbContextOptions<SCMDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductLocation> ProductLocations { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
        public DbSet<ProductSerial> ProductSerials { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<SaleOrder> SaleOrders { get; set; }
        public DbSet<SaleOrderDetail> SaleOrderDetails { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<ReturnOrder> ReturnOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Inventory: Composite Primary Key =====
            modelBuilder.Entity<Inventory>()
                .HasKey(i => new { i.ProductID, i.LocationID });

            // ===== Restrict cascade deletes to avoid multiple cascade paths (SQL Server) =====

            // PurchaseOrder -> User (restrict)
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.User)
                .WithMany(u => u.PurchaseOrders)
                .HasForeignKey(po => po.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // SaleOrder -> User (restrict)
            modelBuilder.Entity<SaleOrder>()
                .HasOne(so => so.User)
                .WithMany(u => u.SaleOrders)
                .HasForeignKey(so => so.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Delivery -> User (restrict)
            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.User)
                .WithMany(u => u.Deliveries)
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Delivery -> SaleOrder (restrict)
            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.SaleOrder)
                .WithMany(so => so.Deliveries)
                .HasForeignKey(d => d.SOID)
                .OnDelete(DeleteBehavior.Restrict);

            // ReturnOrder -> SaleOrder (restrict)
            modelBuilder.Entity<ReturnOrder>()
                .HasOne(r => r.SaleOrder)
                .WithMany(so => so.ReturnOrders)
                .HasForeignKey(r => r.SOID)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductSerial -> ProductLocation (restrict)
            modelBuilder.Entity<ProductSerial>()
                .HasOne(ps => ps.ProductLocation)
                .WithMany(pl => pl.ProductSerials)
                .HasForeignKey(ps => ps.LocationID)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductSerial -> PurchaseOrder (restrict)
            modelBuilder.Entity<ProductSerial>()
                .HasOne(ps => ps.PurchaseOrder)
                .WithMany(po => po.ProductSerials)
                .HasForeignKey(ps => ps.POID)
                .OnDelete(DeleteBehavior.Restrict);

            // Inventory -> ProductLocation (restrict)
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.ProductLocation)
                .WithMany(pl => pl.Inventories)
                .HasForeignKey(i => i.LocationID)
                .OnDelete(DeleteBehavior.Restrict);

            // SaleOrderDetail -> SaleOrder (cascade is fine, but restrict to be safe)
            modelBuilder.Entity<SaleOrderDetail>()
                .HasOne(sod => sod.Product)
                .WithMany(p => p.SaleOrderDetails)
                .HasForeignKey(sod => sod.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            // PurchaseOrderDetail -> Product (restrict)
            modelBuilder.Entity<PurchaseOrderDetail>()
                .HasOne(pod => pod.Product)
                .WithMany(p => p.PurchaseOrderDetails)
                .HasForeignKey(pod => pod.ProductID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
