using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ValorantRentalServer.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ValorantKey> ValorantKeys { get; set; } = null!;
        public DbSet<RiotAccount> RiotAccounts { get; set; } = null!;
        public DbSet<ValorantSession> ValorantSessions { get; set; } = null!;
        public DbSet<Violation> Violations { get; set; } = null!;
        public DbSet<RiotClientPath> RiotClientPaths { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User configuration
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // ValorantKey configuration
            modelBuilder.Entity<ValorantKey>()
                .HasIndex(k => k.KeyCode)
                .IsUnique();

            // ValorantSession configuration - Add navigation property
            modelBuilder.Entity<ValorantSession>()
                .HasOne<ValorantKey>()
                .WithMany()
                .HasForeignKey(s => s.KeyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ValorantSession>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ValorantSession>()
                .HasOne<RiotAccount>()
                .WithMany()
                .HasForeignKey(s => s.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class ValorantKey
    {
        [Key]
        public int KeyId { get; set; }
        public string KeyCode { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public int Duration { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsSold { get; set; }
        public int? SoldToUserId { get; set; }
        public DateTime? ActivatedDate { get; set; }
        public string Status { get; set; } = "Available";
    }

    public class RiotAccount
    {
        [Key]
        public int AccountId { get; set; }
        public string RiotUsername { get; set; } = string.Empty;
        public string RiotPassword { get; set; } = string.Empty;
        public string GameName { get; set; } = "Valorant";
        public string Region { get; set; } = "VN";
        public int? CurrentUserId { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public bool IsAvailable { get; set; }
        public int TotalUsed { get; set; }
        public string AccountStatus { get; set; } = "Good";
        public string? Notes { get; set; }
    }

    public class ValorantSession
    {
        [Key]
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int KeyId { get; set; }
        public int AccountId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Duration { get; set; }
        public string Status { get; set; } = "Active";
        public string? RiotClientPath { get; set; }
        public string? MachineId { get; set; }
        
        // Navigation properties
        public ValorantKey? Key { get; set; }
        public User? User { get; set; }
        public RiotAccount? Account { get; set; }
    }

    public class Violation
    {
        [Key]
        public int ViolationId { get; set; }
        public int UserId { get; set; }
        public int SessionId { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime DetectedTime { get; set; }
        public string Action { get; set; } = "Warning";
    }

    public class RiotClientPath
    {
        [Key]
        public int PathId { get; set; }
        public int UserId { get; set; }
        public string ClientPath { get; set; } = string.Empty;
        public DateTime DetectedDate { get; set; }
        public bool IsValid { get; set; } = true;
    }

    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public int KeyId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionCode { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedDate { get; set; }
    }
}