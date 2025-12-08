using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QLKhachSan.Models;

public partial class Hotel01Context : DbContext
{
    public Hotel01Context()
    {
    }

    public Hotel01Context(DbContextOptions<Hotel01Context> options)
        : base(options)
    {
    }

    public virtual DbSet<BlogPost> BlogPosts { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Hotel> Hotels { get; set; }

    public virtual DbSet<HotelBranch> HotelBranches { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomMaintenance> RoomMaintenances { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserLog> UserLogs { get; set; }

    /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("data source=NITRO-5-TIGER\\MSSQLSEVER; initial catalog=Hotel01; integrated security=True; \nTrustServerCertificate=True;");
*/
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PK__BlogPost__AA126018267532A3");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Author).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK__BlogPosts__Autho__6754599E");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Bookings__73951AEDE78A4D7A");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ServicesUsed).HasMaxLength(300);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Room).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__Bookings__RoomId__5441852A");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Bookings__UserId__534D60F1");
        });

        modelBuilder.Entity<Hotel>(entity =>
        {
            entity.HasKey(e => e.HotelId).HasName("PK__Hotels__46023BDFE9B0706C");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HotelName).HasMaxLength(150);
            entity.Property(e => e.Hotline).HasMaxLength(20);
            entity.Property(e => e.Rating).HasColumnType("decimal(2, 1)");
        });

        modelBuilder.Entity<HotelBranch>(entity =>
        {
            entity.HasKey(e => e.BranchId).HasName("PK__HotelBra__A1682FC544FD54DD");

            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.BranchName).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.HasOne(d => d.Hotel).WithMany(p => p.HotelBranches)
                .HasForeignKey(d => d.HotelId)
                .HasConstraintName("FK__HotelBran__Hotel__44FF419A");

            entity.HasOne(d => d.Manager).WithMany(p => p.HotelBranches)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK__HotelBran__Manag__45F365D3");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.MenuId).HasName("PK__Menus__C99ED2308893ABD2");

            entity.Property(e => e.Icon).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MenuName).HasMaxLength(100);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.Url).HasMaxLength(200);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38A818C1D9");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.TransactionCode).HasMaxLength(100);

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__Payments__Bookin__59063A47");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42FCF44BD7E16");

            entity.HasIndex(e => e.Code, "UQ__Promotio__A25C5AA7D1239785").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79CE60C5CC93");

            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Reply).HasMaxLength(500);

            entity.HasOne(d => d.Room).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__Reviews__RoomId__5FB337D6");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Reviews__UserId__5EBF139D");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Rooms__32863939BC9C9928");

            entity.Property(e => e.Amenities).HasMaxLength(300);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RoomNumber).HasMaxLength(50);
            entity.Property(e => e.RoomType).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Available");

            entity.HasOne(d => d.Branch).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK__Rooms__BranchId__48CFD27E");
        });

        modelBuilder.Entity<RoomMaintenance>(entity =>
        {
            entity.HasKey(e => e.MaintenanceId).HasName("PK__RoomMain__E60542D590B2E1C7");

            entity.ToTable("RoomMaintenance");

            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.MaintenanceDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Room).WithMany(p => p.RoomMaintenances)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__RoomMaint__RoomI__4D94879B");

            entity.HasOne(d => d.Staff).WithMany(p => p.RoomMaintenances)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__RoomMaint__Staff__4F7CD00D");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Services__C51BB00A6889F1D4");

            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ServiceName).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C28289500");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E40703D124").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534CF2D03D2").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.Avatar).HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<UserLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__UserLogs__5E5486482200E926");

            entity.Property(e => e.Action).HasMaxLength(200);
            entity.Property(e => e.ActionTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Device).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.UserLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserLogs__UserId__3D5E1FD2");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
