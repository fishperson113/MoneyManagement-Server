using Microsoft.EntityFrameworkCore;
using API.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Wallet> Wallets { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes for Wallet
            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserID)
                .HasDatabaseName("IX_Wallet_UserID");

            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.WalletName)
                .HasDatabaseName("IX_Wallet_WalletName");

            // Indexes for Category
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .HasDatabaseName("IX_Category_Name");

            //modelBuilder.Entity<Category>()
            //    .HasIndex(c => c.Type)
            //    .HasDatabaseName("IX_Category_Type");

            // Indexes for Transaction
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.WalletID)
                .HasDatabaseName("IX_Transaction_WalletID");

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.CategoryID)
                .HasDatabaseName("IX_Transaction_CategoryID");

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.TransactionDate)
                .HasDatabaseName("IX_Transaction_TransactionDate");

            // Relationships and other configurations
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithMany(u => u.Wallets)
                .HasForeignKey(w => w.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasColumnType("decimal(20,4)");

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Wallet)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.WalletID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(20,4)");

            modelBuilder.Entity<RefreshToken>()
               .HasOne(rt => rt.User)
               .WithMany(u => u.RefreshTokens)
               .HasForeignKey(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
