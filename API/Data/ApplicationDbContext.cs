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
        public DbSet<UserFriend> UserFriends { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<GroupMember> GroupMembers { get; set; } = null!;
        public DbSet<GroupMessage> GroupMessages { get; set; } = null!;
        public DbSet<GroupFund> GroupFunds { get; set; }        
        public DbSet<GroupTransaction> GroupTransactions { get; set; }
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<PostLike> PostLikes { get; set; } = null!;
        public DbSet<PostComment> PostComments { get; set; } = null!;

        public DbSet<PostCommentReply> PostCommentReplies { get; set; } = null!;

        // Safe extensions: New entities for message enhancements
        public DbSet<MessageReaction> MessageReactions { get; set; } = null!;
        public DbSet<MessageMention> MessageMentions { get; set; } = null!;
        public DbSet<GroupTransactionComment> GroupTransactionComments { get; set; } = null!;

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
                .IsRequired()
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
                .WithMany(u => u.MessagesSent)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.MessagesReceived)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFriend>()
                .HasKey(uf => new { uf.UserId, uf.FriendId });

            modelBuilder.Entity<UserFriend>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.FriendRequestsSent)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFriend>()
                .HasOne(uf => uf.Friend)
                .WithMany(u => u.FriendRequestsReceived)
                .HasForeignKey(uf => uf.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            // Group relationships
            modelBuilder.Entity<Group>()
                .HasOne(g => g.Creator)
                .WithMany(u => u.CreatedGroups)
                .HasForeignKey(g => g.CreatorId)
                .OnDelete(DeleteBehavior.NoAction);

            // GroupMember relationships
            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId);

            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(gm => gm.UserId);

            // GroupMessage relationships
            modelBuilder.Entity<GroupMessage>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Messages)
                .HasForeignKey(gm => gm.GroupId);

            modelBuilder.Entity<GroupMessage>()
                .HasOne(gm => gm.Sender)
                .WithMany(u => u.GroupMessagesSent)
                .HasForeignKey(gm => gm.SenderId);

            // GroupFund relationships
            modelBuilder.Entity<GroupFund>()
                .HasOne(gf => gf.Group)
                .WithMany(g => g.Funds)
                .HasForeignKey(gf => gf.GroupID)
                .OnDelete(DeleteBehavior.Cascade);

            // Group Transaction relationships
            modelBuilder.Entity<GroupTransaction>()
                .HasOne(gt => gt.GroupFund)
                .WithMany(gf => gf.GroupTransactions)
                .HasForeignKey(gt => gt.GroupFundID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupTransaction>()
                .HasOne(gt => gt.UserWallet)
                .WithMany(w => w.GroupTransactions)
                .HasForeignKey(gt => gt.UserWalletID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GroupTransaction>()
                .HasOne(gt => gt.UserCategory)
                .WithMany(c => c.GroupTransactions)
                .HasForeignKey(gt => gt.UserCategoryID)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Post relationships
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Post indexes for faster querying
            modelBuilder.Entity<Post>()
                .HasIndex(p => p.AuthorId)
                .HasDatabaseName("IX_Post_AuthorId");

            modelBuilder.Entity<Post>()
                .HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Post_CreatedAt");

            // PostLike relationships
            modelBuilder.Entity<PostLike>()
                .HasOne(pl => pl.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(pl => pl.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostLike>()
                .HasOne(pl => pl.User)
                .WithMany()
                .HasForeignKey(pl => pl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostLike>()
                .HasIndex(pl => new { pl.PostId, pl.UserId })
                .IsUnique()
                .HasDatabaseName("IX_PostLike_PostId_UserId");

            // PostComment relationships
            modelBuilder.Entity<PostComment>()
                .HasOne(pc => pc.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(pc => pc.PostId)
                .OnDelete(DeleteBehavior.Cascade);            
            
            modelBuilder.Entity<PostComment>()
                .HasOne(pc => pc.Author)
                .WithMany()
                .HasForeignKey(pc => pc.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Safe extensions: Configure new message enhancement entities
            // MessageReaction relationships and constraints
            modelBuilder.Entity<MessageReaction>()
                .HasOne(mr => mr.User)
                .WithMany()
                .HasForeignKey(mr => mr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique index: one reaction type per user per message
            modelBuilder.Entity<MessageReaction>()
                .HasIndex(mr => new { mr.MessageId, mr.UserId, mr.ReactionType })
                .IsUnique()
                .HasDatabaseName("IX_MessageReaction_MessageId_UserId_ReactionType");

            // Index for efficient reaction queries
            modelBuilder.Entity<MessageReaction>()
                .HasIndex(mr => mr.MessageId)
                .HasDatabaseName("IX_MessageReaction_MessageId");

            // MessageMention relationships and constraints
            modelBuilder.Entity<MessageMention>()
                .HasOne(mm => mm.MentionedUser)
                .WithMany()
                .HasForeignKey(mm => mm.MentionedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessageMention>()
                .HasOne(mm => mm.MentionedByUser)
                .WithMany()
                .HasForeignKey(mm => mm.MentionedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for efficient mention queries
            modelBuilder.Entity<MessageMention>()
                .HasIndex(mm => mm.MessageId)
                .HasDatabaseName("IX_MessageMention_MessageId");

            modelBuilder.Entity<MessageMention>()
                .HasIndex(mm => mm.MentionedUserId)
                .HasDatabaseName("IX_MessageMention_MentionedUserId");

            // PostCommentReply relationships
            modelBuilder.Entity<PostCommentReply>()
                .HasOne(pcr => pcr.Comment)
                .WithMany(pc => pc.Replies)
                .HasForeignKey(pcr => pcr.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostCommentReply>()
                .HasOne(pcr => pcr.Author)
                .WithMany()
                .HasForeignKey(pcr => pcr.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing relationship for nested replies
            modelBuilder.Entity<PostCommentReply>()
                .HasOne(pcr => pcr.ParentReply)
                .WithMany(pcr => pcr.Replies)
                .HasForeignKey(pcr => pcr.ParentReplyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add these relationship configurations in the OnModelCreating method
            modelBuilder.Entity<GroupTransactionComment>()
                .HasOne(gtc => gtc.GroupTransaction)
                .WithMany(gt => gt.Comments)
                .HasForeignKey(gtc => gtc.GroupTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupTransactionComment>()
                .HasOne(gtc => gtc.User)
                .WithMany()
                .HasForeignKey(gtc => gtc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add an index for faster comment retrieval
            modelBuilder.Entity<GroupTransactionComment>()
                .HasIndex(gtc => gtc.GroupTransactionId)
                .HasDatabaseName("IX_GroupTransactionComment_GroupTransactionId");
        }
    }
}
