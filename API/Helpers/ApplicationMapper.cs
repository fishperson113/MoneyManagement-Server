using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;

namespace API.Helpers
{
    public class ApplicationMapper:Profile
    {
        public ApplicationMapper()
        {
            CreateMap<SignUpDTO, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Wallets, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore());


            // Category Mappings
            CreateMap<CategoryDTO, Category>()
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
               .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
               .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID))
               .ForMember(dest => dest.UserId, opt => opt.Ignore())
               .ForMember(dest => dest.User, opt => opt.Ignore())
               .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<Category, CategoryDTO>()
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
               .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
               .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID));

            CreateMap<CreateCategoryDTO, Category>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryID, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<UpdateCategoryDTO, Category>()
                .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<DeleteCategoryByIdDTO, Category>()
                .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID))
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<GetCategoryByIdDTO, Category>()
                .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID))
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            // Transaction Mappings
            CreateMap<TransactionDTO, Transaction>()
                .ForMember(dest => dest.TransactionID, opt => opt.MapFrom(src => src.TransactionID))
                .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.TransactionDate))
                .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Wallet, opt => opt.Ignore());

            CreateMap<Transaction, TransactionDTO>()
                .ForMember(dest => dest.TransactionID, opt => opt.MapFrom(src => src.TransactionID))
                .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.TransactionDate))
                .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));

            CreateMap<CreateTransactionDTO, Transaction>()
                .ForMember(dest => dest.TransactionID, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.TransactionDate))
                .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Wallet, opt => opt.Ignore());

            CreateMap<UpdateTransactionDTO, Transaction>()
                .ForMember(dest => dest.TransactionID, opt => opt.MapFrom(src => src.TransactionID))
                .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.TransactionDate))
                .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Wallet, opt => opt.Ignore());


            // Wallet Mappings
            CreateMap<WalletDTO, Wallet>()
                .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
                .ForMember(dest => dest.WalletName, opt => opt.MapFrom(src => src.WalletName))
                .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Balance))
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Add this
                .ForMember(dest => dest.User, opt => opt.Ignore())   // Add this
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<Wallet, WalletDTO>()
               .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
               .ForMember(dest => dest.WalletName, opt => opt.MapFrom(src => src.WalletName))
               .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Balance));

            CreateMap<CreateWalletDTO, Wallet>()
                .ForMember(dest => dest.WalletID, opt => opt.Ignore())
                .ForMember(dest => dest.WalletName, opt => opt.MapFrom(src => src.WalletName))
                .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Balance))
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // We'll set this in controller/repository
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<UpdateWalletDTO, Wallet>()
                .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
                .ForMember(dest => dest.WalletName, opt => opt.MapFrom(src => src.WalletName))
                .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Balance))
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Don't change owner when updating
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore());

            CreateMap<Transaction, TransactionDetailDTO>()
               .ForMember(dest => dest.TransactionID, opt => opt.MapFrom(src => src.TransactionID))
               .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.TransactionDate))
               .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.TransactionDate.Date))
               .ForMember(dest => dest.Time, opt => opt.MapFrom(src => src.TransactionDate.ToString("HH:mm:ss")))
               .ForMember(dest => dest.DayOfWeek, opt => opt.MapFrom(src => src.TransactionDate.DayOfWeek.ToString()))
               .ForMember(dest => dest.Month, opt => opt.MapFrom(src => src.TransactionDate.ToString("MMMM")))
               .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
               .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
               .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.Name))
               .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.Category.CategoryID))
               .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
               .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
               .ForMember(dest => dest.WalletName, opt => opt.MapFrom(src => src.Wallet.WalletName));

            // Message Mappings
            CreateMap<Message, MessageDTO>()
                .ForMember(dest => dest.MessageID, opt => opt.MapFrom(src => src.MessageID))
                .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.SenderId))
                .ForMember(dest => dest.ReceiverId, opt => opt.MapFrom(src => src.ReceiverId))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.SentAt))
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => $"{src.Sender.FirstName} {src.Sender.LastName}"))
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => $"{src.Receiver.FirstName} {src.Receiver.LastName}"));

            CreateMap<SendMessageDto, Message>()
                .ForMember(dest => dest.MessageID, opt => opt.Ignore())
                .ForMember(dest => dest.SenderId, opt => opt.Ignore())
                .ForMember(dest => dest.ReceiverId, opt => opt.MapFrom(src => src.ReceiverId))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.SentAt, opt => opt.Ignore())
                .ForMember(dest => dest.Sender, opt => opt.Ignore())
                .ForMember(dest => dest.Receiver, opt => opt.Ignore());

            CreateMap<UserFriend, FriendDTO>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom((src, _, _, context) =>
                    context.Items.ContainsKey("CurrentUserId") &&
                    (string)context.Items["CurrentUserId"] == src.UserId ? src.FriendId : src.UserId))
                .ForMember(dest => dest.Username, opt => opt.MapFrom((src, _, _, context) =>
                    context.Items.ContainsKey("CurrentUserId") &&
                    (string)context.Items["CurrentUserId"] == src.UserId ? src.Friend.UserName : src.User.UserName))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom((src, _, _, context) => {
                    if (context.Items.ContainsKey("CurrentUserId") && (string)context.Items["CurrentUserId"] == src.UserId)
                        return $"{src.Friend.FirstName} {src.Friend.LastName}";
                    return $"{src.User.FirstName} {src.User.LastName}";
                }))
                .ForMember(dest => dest.IsOnline, opt => opt.Ignore()) // Set by SignalR/service
                .ForMember(dest => dest.LastActive, opt => opt.Ignore()) // Set by service if needed
                .ForMember(dest => dest.IsPendingRequest, opt => opt.MapFrom(src => !src.IsAccepted));

            CreateMap<UserFriend, FriendRequestDTO>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.UserName))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"))
                .ForMember(dest => dest.RequestedAt, opt => opt.MapFrom(src => src.RequestedAt));

            CreateMap<AddFriendDTO, UserFriend>()
                .ForMember(dest => dest.FriendId, opt => opt.MapFrom(src => src.FriendId))
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.IsAccepted, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.RequestedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.AcceptedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Friend, opt => opt.Ignore());
            CreateMap<Group, GroupDTO>()
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.CreatorId, opt => opt.MapFrom(src => src.CreatorId))
                .ForMember(dest => dest.CreatorName, opt => opt.Ignore()) // Set manually after mapping
                .ForMember(dest => dest.MemberCount, opt => opt.Ignore()) // Calculate after mapping
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            CreateMap<CreateGroupDTO, Group>()
                .ForMember(dest => dest.GroupId, opt => opt.Ignore())
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatorId, opt => opt.Ignore())
                .ForMember(dest => dest.Creator, opt => opt.Ignore())
                .ForMember(dest => dest.Members, opt => opt.Ignore())
                .ForMember(dest => dest.Messages, opt => opt.Ignore());

            // GroupMember mappings
            CreateMap<GroupMember, GroupMemberDTO>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.DisplayName, opt => opt.Ignore()) // Set manually after mapping
                .ForMember(dest => dest.AvatarUrl, opt => opt.Ignore())   // Set manually after mapping
                .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.JoinedAt));

            // GroupMessage mappings
            CreateMap<GroupMessage, GroupMessageDTO>()
                .ForMember(dest => dest.MessageId, opt => opt.MapFrom(src => src.MessageId))
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.SenderId))
                .ForMember(dest => dest.SenderName, opt => opt.Ignore()) // Set manually after mapping
                .ForMember(dest => dest.SenderAvatarUrl, opt => opt.Ignore()) // Set manually after mapping
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.SentAt));

            CreateMap<SendGroupMessageDTO, GroupMessage>()
                .ForMember(dest => dest.MessageId, opt => opt.Ignore())
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(dest => dest.SenderId, opt => opt.Ignore())    // Set manually after mapping
                .ForMember(dest => dest.Sender, opt => opt.Ignore())      // Loaded separately
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.SentAt, opt => opt.Ignore())      // Set to current time
                .ForMember(dest => dest.Group, opt => opt.Ignore());      // Loaded separately

            // Group Fund mappings
            CreateMap<CreateGroupFundDTO, GroupFund>()
                .ForMember(dest => dest.GroupFundID, opt => opt.Ignore())
                .ForMember(dest => dest.GroupID, opt => opt.MapFrom(src => src.GroupID))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.SavingGoal, opt => opt.MapFrom(src => src.SavingGoal))
                .ForMember(dest => dest.TotalFundsIn, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.TotalFundsOut, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.Balance, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.GroupTransactions, opt => opt.Ignore());

            CreateMap<GroupFund, GroupFundDTO>()
                .ForMember(dest => dest.GroupFundID, opt => opt.MapFrom(src => src.GroupFundID))
                .ForMember(dest => dest.GroupID, opt => opt.MapFrom(src => src.GroupID))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.SavingGoal, opt => opt.MapFrom(src => src.SavingGoal))
                .ForMember(dest => dest.TotalFundsIn, opt => opt.MapFrom(src => src.TotalFundsIn))
                .ForMember(dest => dest.TotalFundsOut, opt => opt.MapFrom(src => src.TotalFundsOut))
                .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Balance))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            CreateMap<UpdateGroupFundDTO, GroupFund>()
                .ForMember(dest => dest.GroupFundID, opt => opt.MapFrom(src => src.GroupFundID))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.SavingGoal, opt => opt.MapFrom(src => src.SavingGoal))
                .ForMember(dest => dest.GroupID, opt => opt.Ignore())
                .ForMember(dest => dest.TotalFundsIn, opt => opt.Ignore())
                .ForMember(dest => dest.TotalFundsOut, opt => opt.Ignore())
                .ForMember(dest => dest.Balance, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.GroupTransactions, opt => opt.Ignore());

            // Group Transaction mappings
            CreateMap<CreateGroupTransactionDTO, GroupTransaction>()
                .ForMember(dest => dest.GroupTransactionID, opt => opt.Ignore())
                .ForMember(dest => dest.GroupFundID, opt => opt.MapFrom(src => src.GroupFundID))
                .ForMember(dest => dest.UserWalletID, opt => opt.MapFrom(src => src.UserWalletID))
                .ForMember(dest => dest.UserCategoryID, opt => opt.MapFrom(src => src.UserCategoryID))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.TransactionDate))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.GroupFund, opt => opt.Ignore())
                .ForMember(dest => dest.UserWallet, opt => opt.Ignore())
                .ForMember(dest => dest.UserCategory, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore());

            CreateMap<GroupTransaction, GroupTransactionDTO>()
                .ForMember(dest => dest.GroupTransactionID, opt => opt.MapFrom(src => src.GroupTransactionID))
                .ForMember(dest => dest.GroupFundID, opt => opt.MapFrom(src => src.GroupFundID))
                .ForMember(dest => dest.UserWalletID, opt => opt.MapFrom(src => src.UserWalletID))
                .ForMember(dest => dest.UserCategoryID, opt => opt.MapFrom(src => src.UserCategoryID))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.TransactionDate))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.UserWalletName, opt => opt.MapFrom(src => src.UserWallet != null ? src.UserWallet.WalletName: null))
                .ForMember(dest => dest.UserCategoryName, opt => opt.MapFrom(src => src.UserCategory != null ? src.UserCategory.Name : null))
                .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => src.CreatedByUserId))
                .ForMember(dest => dest.CreatedByUserName, opt => opt.Ignore()) // Cần gán thủ công nếu muốn
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

            CreateMap<UpdateGroupTransactionDTO, GroupTransaction>()
                .ForMember(dest => dest.GroupTransactionID, opt => opt.MapFrom(src => src.GroupTransactionID))
                .ForMember(dest => dest.UserWalletID, opt => opt.MapFrom(src => src.UserWalletID))
                .ForMember(dest => dest.UserCategoryID, opt => opt.MapFrom(src => src.UserCategoryID))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.TransactionDate))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.GroupFundID, opt => opt.Ignore())
                .ForMember(dest => dest.GroupFund, opt => opt.Ignore())
                .ForMember(dest => dest.UserWallet, opt => opt.Ignore())
                .ForMember(dest => dest.UserCategory, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // Simple self-mappings for DTOs that are created directly in the repository
            CreateMap<CategoryBreakdownDTO, CategoryBreakdownDTO>();
       
        // Simple self-mappings for DTOs that are created directly in the repository
            CreateMap<CategoryBreakdownDTO, CategoryBreakdownDTO>();
            CreateMap<CashFlowSummaryDTO, CashFlowSummaryDTO>();
            CreateMap<DailySummaryDTO, DailySummaryDTO>();
            CreateMap<AggregateStatisticsDTO, AggregateStatisticsDTO>();
            CreateMap<UpcomingBillDTO, UpcomingBillDTO>();
            CreateMap<ReportInfoDTO, ReportInfoDTO>();
            CreateMap<ReportInfoDTO, ReportInfoDTO>();

            // WeeklySummaryDTO Mapping
            CreateMap<WeeklySummaryDTO, WeeklySummaryDTO>();

            // MonthlySummaryDTO Mapping
            CreateMap<MonthlySummaryDTO, MonthlySummaryDTO>();

            // YearlySummaryDTO Mapping
            CreateMap<YearlySummaryDTO, YearlySummaryDTO>();

            CreateMap<FriendDTO, FriendDTO>();
            CreateMap<FriendRequestDTO, FriendRequestDTO>();
            CreateMap<AvatarDTO, AvatarDTO>();

            // Add these mappings to your existing CreateMappings method
            CreateMap<Post, PostDTO>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src =>
                    $"{src.Author.FirstName} {src.Author.LastName}".Trim()))
                .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.MapFrom(src => src.Author.AvatarUrl))
                .ForMember(dest => dest.LikesCount, opt => opt.MapFrom(src => src.Likes.Count))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src => src.Comments.Count));

            CreateMap<PostComment, PostCommentDTO>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src =>
                    $"{src.Author.FirstName} {src.Author.LastName}".Trim()))
                .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.MapFrom(src => src.Author.AvatarUrl));            
            CreateMap<PostLike, PostLikeDTO>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                    $"{src.User.FirstName} {src.User.LastName}".Trim()));

            // Safe extensions: Message enhancement mappings
            // MessageReaction mappings
            CreateMap<MessageReaction, MessageReactionDTO>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
                .ForMember(dest => dest.UserAvatarUrl, opt => opt.MapFrom(src => src.User.AvatarUrl));

            CreateMap<CreateMessageReactionDTO, MessageReaction>()
                .ForMember(dest => dest.ReactionId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // MessageMention mappings
            CreateMap<MessageMention, MessageMentionDTO>()
                .ForMember(dest => dest.MentionedUserName, opt => opt.MapFrom(src => src.MentionedUser.UserName))
                .ForMember(dest => dest.MentionedUserAvatarUrl, opt => opt.MapFrom(src => src.MentionedUser.AvatarUrl))
                .ForMember(dest => dest.MentionedByUserName, opt => opt.MapFrom(src => src.MentionedByUser.UserName));

            // Add PostCommentReply mapping
            CreateMap<PostCommentReply, PostCommentReplyDTO>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src =>
                    $"{src.Author.FirstName} {src.Author.LastName}".Trim()))
                .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.MapFrom(src => src.Author.AvatarUrl));

            // Add CreateCommentReplyDTO to PostCommentReply mapping
            CreateMap<CreateCommentReplyDTO, PostCommentReply>()
                .ForMember(dest => dest.ReplyId, opt => opt.Ignore())
                .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
                .ForMember(dest => dest.Author, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Replies, opt => opt.Ignore())
                .ForMember(dest => dest.Comment, opt => opt.Ignore());

            // Add these mappings to your ApplicationMapper constructor
            // GroupTransactionComment mappings
            CreateMap<GroupTransactionComment, GroupTransactionCommentDTO>()
                .ForMember(dest => dest.CommentId, opt => opt.MapFrom(src => src.CommentId))
                .ForMember(dest => dest.GroupTransactionId, opt => opt.MapFrom(src => src.GroupTransactionId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}".Trim()))
                .ForMember(dest => dest.UserAvatarUrl, opt => opt.MapFrom(src => src.User.AvatarUrl))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            CreateMap<CreateGroupTransactionCommentDTO, GroupTransactionComment>()
                .ForMember(dest => dest.CommentId, opt => opt.Ignore())
                .ForMember(dest => dest.GroupTransactionId, opt => opt.MapFrom(src => src.GroupTransactionId))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.GroupTransaction, opt => opt.Ignore());

            CreateMap<UpdateGroupTransactionCommentDTO, GroupTransactionComment>()
                .ForMember(dest => dest.CommentId, opt => opt.MapFrom(src => src.CommentId))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.GroupTransactionId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.GroupTransaction, opt => opt.Ignore());
        }
    }
}

