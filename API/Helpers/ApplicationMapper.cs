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
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Amount < 0 ? "expense" : "income"))
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Wallet, opt => opt.Ignore());

            CreateMap<UpdateTransactionDTO, Transaction>()
                .ForMember(dest => dest.TransactionID, opt => opt.MapFrom(src => src.TransactionID))
                .ForMember(dest => dest.CategoryID, opt => opt.MapFrom(src => src.CategoryID))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.TransactionDate))
                .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Amount < 0 ? "expense" : "income"))
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

        }

    }
}

