using Microsoft.EntityFrameworkCore;
using WineryOrderSystemAPI.Models;

namespace WineryOrderSystemAPI.Data;

public partial class WineryReadDbContext : DbContext
{
    public WineryReadDbContext()
    {
    }

    public WineryReadDbContext(DbContextOptions<WineryReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public virtual DbSet<DimCustomer> DimCustomers { get; set; }

    public virtual DbSet<DimTime> DimTimes { get; set; }

    public virtual DbSet<DimWine> DimWines { get; set; }

    public virtual DbSet<FactSale> FactSales { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=LAPTOP-E3QFUDTV\\SQLEXPRESS;Database=WineryDb_Replica;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DimCustomer>(entity =>
        {
            entity.HasKey(e => e.CustomerKey).HasName("PK__Dim_Cust__95011E64D296E159");

            entity.ToTable("Dim_Customer");

            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DimTime>(entity =>
        {
            entity.HasKey(e => e.TimeKey).HasName("PK__Dim_Time__5877C2FE56A7B9BE");

            entity.ToTable("Dim_Time");

            entity.Property(e => e.Date).HasColumnType("datetime");
        });

        modelBuilder.Entity<DimWine>(entity =>
        {
            entity.HasKey(e => e.WineKey).HasName("PK__Dim_Wine__9488906DF67D0635");

            entity.ToTable("Dim_Wine");

            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.WineName)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<FactSale>(entity =>
        {
            entity.HasKey(e => e.SalesId).HasName("PK__Fact_Sal__C952FB32F9776BF8");

            entity.ToTable("Fact_Sales");

            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CustomerKeyNavigation).WithMany(p => p.FactSales)
                .HasForeignKey(d => d.CustomerKey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Fact_Sale__Custo__5070F446");

            entity.HasOne(d => d.TimeKeyNavigation).WithMany(p => p.FactSales)
                .HasForeignKey(d => d.TimeKey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Fact_Sale__TimeK__5165187F");

            entity.HasOne(d => d.WineKeyNavigation).WithMany(p => p.FactSales)
                .HasForeignKey(d => d.WineKey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Fact_Sale__WineK__4F7CD00D");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
