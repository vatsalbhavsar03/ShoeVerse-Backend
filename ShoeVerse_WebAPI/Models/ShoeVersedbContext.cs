using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ShoeVerse_WebAPI.Models;

public partial class ShoeVersedbContext : DbContext
{
    public ShoeVersedbContext()
    {
    }

    public ShoeVersedbContext(DbContextOptions<ShoeVersedbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductColor> ProductColors { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductSize> ProductSizes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=Vatsal\\SQLEXPRESS;Initial Catalog=ShoeVersedb;Integrated Security=True;Encrypt=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("PK__Brand__AAB3216F7BE431AA");

            entity.ToTable("Brand");

            entity.Property(e => e.BrandId).HasColumnName("Brand_ID");
            entity.Property(e => e.BrandName)
                .HasMaxLength(100)
                .HasColumnName("Brand_Name");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");

            entity.HasOne(d => d.Category).WithMany(p => p.Brands)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Brand__Category___571DF1D5");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__6DB38D4E5C2E4987");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("Category_Name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Product__9834FB9AAE0CC555");

            entity.ToTable("Product");

            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.AverageRating)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(3, 2)");
            entity.Property(e => e.BrandId).HasColumnName("Brand_ID");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Material).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Stock).HasDefaultValue(0);
            entity.Property(e => e.TotalReviews).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .HasConstraintName("FK__Product__Brand_I__5BE2A6F2");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Product__Categor__5AEE82B9");
        });

        modelBuilder.Entity<ProductColor>(entity =>
        {
            entity.HasKey(e => e.ColorId).HasName("PK__ProductC__795F1D74B47C16A7");

            entity.ToTable("ProductColor");

            entity.Property(e => e.ColorId).HasColumnName("Color_ID");
            entity.Property(e => e.ColorName).HasMaxLength(50);
            entity.Property(e => e.HexCode).HasMaxLength(10);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.Stock).HasDefaultValue(0);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductColors)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductCo__Produ__6477ECF3");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__3CAB4D39711B4181");

            entity.ToTable("ProductImage");

            entity.Property(e => e.ImageId).HasColumnName("Image_ID");
            entity.Property(e => e.ColorId).HasColumnName("Color_ID");
            entity.Property(e => e.IsMainImage).HasDefaultValue(false);
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");

            entity.HasOne(d => d.Color).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ColorId)
                .HasConstraintName("FK__ProductIm__Color__6A30C649");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductIm__Produ__693CA210");
        });

        modelBuilder.Entity<ProductSize>(entity =>
        {
            entity.HasKey(e => e.SizeId);

            entity.ToTable("ProductSize");

            entity.Property(e => e.SizeId).HasColumnName("Size_ID");
            entity.Property(e => e.ColorId).HasColumnName("Color_ID");
            entity.Property(e => e.SizeName).HasMaxLength(50);

            entity.HasOne(d => d.Color).WithMany(p => p.ProductSizes)
                .HasForeignKey(d => d.ColorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductSize_ProductColor");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__D80AB49BE7C74DC6");

            entity.ToTable("Role");

            entity.Property(e => e.RoleId).HasColumnName("Role_ID");
            entity.Property(e => e.Rname).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__206D9190921A7F29");

            entity.ToTable("User");

            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.ProfileImage)
                .HasMaxLength(255)
                .HasColumnName("Profile_Image");
            entity.Property(e => e.RoleId).HasColumnName("Role_ID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__Role_ID__3C69FB99");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
