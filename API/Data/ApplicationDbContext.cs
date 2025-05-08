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
        public DbSet<Message> Messages { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.WalletName)
                .HasDatabaseName("IX_Wallet_WalletName");

            // Indexes for Category
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .HasDatabaseName("IX_Category_Name");

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.UserId)
                .HasDatabaseName("IX_Category_UserId");

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

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Type)
                .HasDatabaseName("IX_Transaction_Type");
            modelBuilder.Entity<Wallet>()
               .HasIndex(w => w.UserId)
               .HasDatabaseName("IX_Wallet_UserId");

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Wallets)
                .WithOne(w => w.User)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ApplicationUser>()
               .HasMany(u => u.Categories)
               .WithOne(c => c.User)
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Transaction>()
               .Property(t => t.Type)
               .HasMaxLength(20)
               .IsRequired()
               .HasDefaultValue("expense");

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

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
