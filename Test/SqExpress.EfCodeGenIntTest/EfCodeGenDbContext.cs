using Microsoft.EntityFrameworkCore;

namespace SqExpress.EfCodeGenIntTest;

public sealed class EfCodeGenDbContext : DbContext
{
    public const string ConnectionString = "Data Source=(local);Initial Catalog=EFTest;Integrated Security=True;TrustServerCertificate=True";

    public DbSet<Customer> Customers => this.Set<Customer>();

    public DbSet<Category> Categories => this.Set<Category>();

    public DbSet<Product> Products => this.Set<Product>();

    public DbSet<Order> Orders => this.Set<Order>();

    public DbSet<OrderLine> OrderLines => this.Set<OrderLine>();

    public DbSet<AuditLog> AuditLogs => this.Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(ConnectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers", "sales");
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId).ValueGeneratedOnAdd();
            entity.Property(e => e.PublicId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Code).HasMaxLength(12).IsUnicode(false).IsFixedLength();
            entity.Property(e => e.Name).HasMaxLength(200).IsUnicode();
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.Score).HasDefaultValueSql("0");
            entity.Property(e => e.RiskLevel).HasDefaultValueSql("1");
            entity.Property(e => e.LegacyCode).HasDefaultValueSql("7");
            entity.Property(e => e.IsActive).HasDefaultValueSql("1");
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("sysutcdatetime()");
            entity.Property(e => e.LastSeenAt).HasColumnType("datetimeoffset");
            entity.Property(e => e.SessionTimeout).HasColumnType("time");
            entity.Property(e => e.BinaryCode).HasMaxLength(16);
            entity.Property(e => e.FixedBinaryCode).HasMaxLength(8).IsFixedLength();
            entity.Property(e => e.MetadataXml).HasColumnType("xml");
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.Name, e.CreatedUtc }).IsDescending(false, true);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", "catalog");
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryId).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(100).IsUnicode();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products", "catalog");
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.ProductId).ValueGeneratedOnAdd();
            entity.Property(e => e.Sku).HasMaxLength(32).IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(200).IsUnicode();
            entity.Property(e => e.Price).HasPrecision(19, 4);
            entity.Property(e => e.IsDiscontinued).HasDefaultValueSql("0");
            entity.HasOne(e => e.Category)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.HasIndex(e => new { e.CategoryId, e.Price });
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", "sales");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).ValueGeneratedOnAdd();
            entity.Property(e => e.OrderNumber).HasMaxLength(32).IsUnicode(false);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("sysutcdatetime()");
            entity.Property(e => e.Total).HasPrecision(19, 4);
            entity.HasOne(e => e.Customer)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.CustomerId);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => new { e.CustomerId, e.CreatedUtc }).IsDescending(false, true);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.ToTable("OrderLines", "sales");
            entity.HasKey(e => new { e.OrderId, e.LineNo });
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.UnitPrice).HasPrecision(19, 4);
            entity.HasOne(e => e.Order)
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.OrderId);
            entity.HasOne(e => e.Product)
                .WithMany(e => e.OrderLines)
                .HasForeignKey(e => e.ProductId);
            entity.HasIndex(e => new { e.ProductId, e.Quantity });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs", "audit");
            entity.HasKey(e => e.AuditLogId);
            entity.Property(e => e.AuditLogId).ValueGeneratedOnAdd();
            entity.Property(e => e.EntityName).HasMaxLength(128).IsUnicode(false);
            entity.Property(e => e.Message).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("sysutcdatetime()");
            entity.HasIndex(e => new { e.EntityName, e.CreatedUtc }).IsDescending(false, true);
        });
    }
}
