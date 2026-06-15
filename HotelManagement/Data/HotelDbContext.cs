using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Data
{
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<RoomType> RoomTypes { get; set; }

        public DbSet<Room> Rooms { get; set; }

        public DbSet<RoomImage> RoomImages { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<Service> Services { get; set; }

        public DbSet<BookingService> BookingServices { get; set; }

        public DbSet<Invoice> Invoices { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUser(modelBuilder);
            ConfigureRoomType(modelBuilder);
            ConfigureRoom(modelBuilder);
            ConfigureRoomImage(modelBuilder);
            ConfigureBooking(modelBuilder);
            ConfigureService(modelBuilder);
            ConfigureBookingService(modelBuilder);
            ConfigureInvoice(modelBuilder);
            ConfigurePayment(modelBuilder);
            ConfigureActivityLog(modelBuilder);
        }

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();

                entity.Property(u => u.FullName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(u => u.PhoneNumber)
                    .HasMaxLength(20);

                entity.Property(u => u.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(u => u.Role)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(u => u.IdentityNumber)
                    .HasMaxLength(50);

                entity.Property(u => u.Address)
                    .HasMaxLength(255);

                entity.Property(u => u.Status)
                    .IsRequired()
                    .HasMaxLength(30);
            });
        }

        private static void ConfigureRoomType(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RoomType>(entity =>
            {
                entity.Property(rt => rt.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(rt => rt.Price)
                    .HasColumnType("decimal(18,2)");

                entity.Property(rt => rt.BedType)
                    .HasMaxLength(100);

                entity.Property(rt => rt.ThumbnailUrl)
                    .HasMaxLength(500);

                entity.Property(rt => rt.Status)
                    .IsRequired()
                    .HasMaxLength(30);
            });
        }

        private static void ConfigureRoom(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasIndex(r => r.RoomNumber).IsUnique();
                entity.HasIndex(r => new { r.RoomTypeId, r.Status });

                entity.Property(r => r.RoomNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(r => r.Note)
                    .HasMaxLength(500);

                entity.HasOne(r => r.RoomType)
                    .WithMany(rt => rt.Rooms)
                    .HasForeignKey(r => r.RoomTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBooking(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasIndex(b => b.BookingCode).IsUnique();
                entity.HasIndex(b => b.CustomerId);
                entity.HasIndex(b => b.Status);
                entity.HasIndex(b => new { b.RoomId, b.CheckInDate, b.CheckOutDate, b.Status });

                entity.Property(b => b.BookingCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(b => b.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(b => b.TotalRoomAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(b => b.TotalServiceAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(b => b.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(b => b.SpecialRequest)
                    .HasMaxLength(1000);

                entity.Property(b => b.CancelReason)
                    .HasMaxLength(500);

                entity.HasOne(b => b.Customer)
                    .WithMany(u => u.CustomerBookings)
                    .HasForeignKey(b => b.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.CreatedByUser)
                    .WithMany(u => u.CreatedBookings)
                    .HasForeignKey(b => b.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Room)
                    .WithMany(r => r.Bookings)
                    .HasForeignKey(b => b.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureRoomImage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RoomImage>(entity =>
            {
                entity.HasIndex(ri => ri.RoomId);

                entity.Property(ri => ri.ImageUrl)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(ri => ri.Caption)
                    .HasMaxLength(255);

                entity.HasOne(ri => ri.Room)
                    .WithMany(r => r.RoomImages)
                    .HasForeignKey(ri => ri.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureService(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Service>(entity =>
            {
                entity.Property(s => s.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(s => s.Category)
                    .HasMaxLength(100);

                entity.Property(s => s.Unit)
                    .HasMaxLength(50);

                entity.Property(s => s.Price)
                    .HasColumnType("decimal(18,2)");

                entity.Property(s => s.Status)
                    .IsRequired()
                    .HasMaxLength(30);
            });
        }

        private static void ConfigureBookingService(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BookingService>(entity =>
            {
                entity.Property(bs => bs.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(bs => bs.TotalPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(bs => bs.Note)
                    .HasMaxLength(500);

                entity.HasOne(bs => bs.Booking)
                    .WithMany(b => b.BookingServices)
                    .HasForeignKey(bs => bs.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bs => bs.Service)
                    .WithMany(s => s.BookingServices)
                    .HasForeignKey(bs => bs.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(bs => bs.CreatedByUser)
                    .WithMany(u => u.CreatedBookingServices)
                    .HasForeignKey(bs => bs.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureInvoice(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(i => i.InvoiceCode).IsUnique();
                entity.HasIndex(i => i.BookingId).IsUnique();
                entity.HasIndex(i => i.Status);

                entity.Property(i => i.InvoiceCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(i => i.RoomAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.ServiceAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.DiscountAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.TaxAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.PaidAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.RemainingAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(i => i.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(i => i.Note)
                    .HasMaxLength(500);

                entity.HasOne(i => i.Booking)
                    .WithOne(b => b.Invoice)
                    .HasForeignKey<Invoice>(i => i.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.IssuedByUser)
                    .WithMany(u => u.IssuedInvoices)
                    .HasForeignKey(i => i.IssuedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigurePayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasIndex(p => p.PaymentCode).IsUnique();
                entity.HasIndex(p => p.InvoiceId);

                entity.Property(p => p.PaymentCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.PaymentMethod)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.Amount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(p => p.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(p => p.Note)
                    .HasMaxLength(500);

                entity.HasOne(p => p.Invoice)
                    .WithMany(i => i.Payments)
                    .HasForeignKey(p => p.InvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.CreatedByUser)
                    .WithMany(u => u.CreatedPayments)
                    .HasForeignKey(p => p.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureActivityLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.CreatedAt);

                entity.Property(a => a.Action)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(a => a.EntityName)
                    .HasMaxLength(100);

                entity.Property(a => a.Description)
                    .HasMaxLength(1000);

                entity.HasOne(a => a.User)
                    .WithMany(u => u.ActivityLogs)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
