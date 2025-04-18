﻿using API.Models.DTOs;
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
               .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.TransactionDate.Date))
               .ForMember(dest => dest.Time, opt => opt.MapFrom(src => src.TransactionDate.ToString("HH:mm:ss")))
               .ForMember(dest => dest.DayOfWeek, opt => opt.MapFrom(src => src.TransactionDate.DayOfWeek.ToString()))
               .ForMember(dest => dest.Month, opt => opt.MapFrom(src => src.TransactionDate.ToString("MMMM")))
               .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
               .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
               .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.Name))
               .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
               .ForMember(dest => dest.WalletID, opt => opt.MapFrom(src => src.WalletID))
               .ForMember(dest => dest.WalletName, opt => opt.MapFrom(src => src.Wallet.WalletName));

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

        }

    }
}

