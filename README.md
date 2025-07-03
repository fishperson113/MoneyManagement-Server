# MoneyManagement Server

This is a comprehensive ASP.NET Core financial management application with social features, built with .NET 8 and SQL Server, deployed and run entirely with Docker.

## 🚀 Prerequisites

- Docker Desktop installed and running on your system

## 🛠️ How to Run

1. **Open your terminal (PowerShell, CMD, or Bash)**

2. **Navigate to the root folder of this project (where docker-compose.yml is located):**
   ```powershell
   cd API
   ```

3. **Set up environment variables:**
   Create a `.env` file in the root directory with the following variables:
   ```env
   GEMINI_KEY=your_gemini_api_key_here
   FIREBASE_CREDENTIAL_PATH=/app/secrets/firebase.json
   FIREBASE_PROJECT_ID=your_firebase_project_id
   FIREBASE_STORAGE_BUCKET=your_firebase_storage_bucket
   ```

4. **Run the application using Docker Compose:**
   ```powershell
   docker compose up
   ```

   This will:
   - Build the ASP.NET Core API using .NET 8 SDK
   - Start a SQL Server 2022 container
   - Run the backend on http://localhost:5000

## 📂 Project Overview

### Architecture
- **api** (Dockerfile): .NET 8 + ASP.NET Core + Entity Framework Core
- **sqlserver**: SQL Server 2022, running in Docker
- **Configuration**: All database connection settings are configured in `appsettings.json` files

### Core Features
- **Financial Management**: Wallets, transactions, categories, and budgeting
- **Social Features**: Friend system, group chats, news feed, and posts
- **Reporting**: PDF report generation with QuestPDF
- **Real-time Communication**: SignalR-powered chat system
- **AI Integration**: Gemini AI for intelligent insights
- **File Storage**: Firebase Storage integration
- **Authentication**: JWT-based authentication with ASP.NET Core Identity

### Tech Stack
- **.NET 8** with C# 13
- **ASP.NET Core** with Entity Framework Core
- **SQL Server 2022** for data persistence
- **AutoMapper** for object-to-object mapping
- **SignalR** for real-time communication
- **Firebase** for cloud storage and authentication
- **QuestPDF** for report generation
- **Moq** for unit testing
- **Swagger/OpenAPI** for API documentation

## 🗃️ Database Info

The SQL Server service is preconfigured with:
- **Server**: localhost:1433
- **Database**: UsersDb
- **Username**: sa
- **Password**: YourStrong!Passw0rd
- **Trust Server Certificate**: true

No local installation of SQL Server is required — Docker handles it for you.

## 🏗️ Project Structure

```
API/
├── Controllers/          # REST API endpoints
│   ├── AccountsController.cs
│   ├── TransactionsController.cs
│   ├── WalletsController.cs
│   ├── CategoriesController.cs
│   ├── GroupsController.cs
│   ├── MessagesController.cs
│   └── ...
├── Models/
│   ├── Entities/         # Database entities
│   └── DTOs/             # Data Transfer Objects
├── Repositories/         # Data access layer
├── Services/             # Business logic services
├── Data/                 # Entity Framework DbContext
├── Helpers/              # Utility classes and mappers
├── Hub/                  # SignalR hubs for real-time features
├── Migrations/           # Entity Framework migrations
└── Config/               # Configuration classes

API.Test/                 # Unit tests
├── CategoryRepositoryTests.cs
├── TransactionRepositoryTests.cs
└── WalletRepositoryTests.cs
```

## 🔑 API Endpoints

### Core Financial Features
- **Wallets**: `/api/wallets` - Manage user wallets
- **Transactions**: `/api/transactions` - Track income and expenses
- **Categories**: `/api/categories` - Organize transactions
- **Reports**: `/api/reports` - Generate financial reports

### Social Features
- **Friends**: `/api/friends` - Friend management system
- **Groups**: `/api/groups` - Group chat functionality
- **Messages**: `/api/messages` - Direct messaging
- **News Feed**: `/api/newsfeed` - Social posts and updates

### Analytics & Reports
- **Statistics**: `/api/statistics` - Financial analytics
- **Calendar**: `/api/calendar` - Daily/weekly/monthly summaries

## ✅ Useful URLs

- **Backend API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/ping

## 🧪 Testing

The project includes comprehensive unit tests using:
- **xUnit** for test framework
- **Moq** for mocking dependencies
- **Entity Framework In-Memory Database** for testing

Run tests using:
```powershell
dotnet test
```

## 🔧 Development Features

- **Hot Reload**: Enabled for development
- **Swagger Documentation**: Interactive API documentation
- **Logging**: Structured logging with different log levels
- **CORS**: Configured for cross-origin requests
- **Authentication**: JWT Bearer token authentication
- **Real-time Updates**: SignalR for instant notifications

## 🔒 Security Features

- **JWT Authentication**: Secure token-based authentication
- **Role-based Authorization**: Admin and user roles
- **Input Validation**: Comprehensive request validation
- **HTTPS**: SSL/TLS encryption support
- **Firebase Security**: Secure file upload and storage

## 📦 Dependencies

Key NuGet packages include:
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.AspNetCore.Authentication.JwtBearer
- AutoMapper
- FirebaseAdmin
- QuestPDF
- Microsoft.AspNetCore.SignalR
- Swashbuckle.AspNetCore

## 🚀 Deployment

The application is containerized and ready for deployment:
- **Docker Multi-stage Build**: Optimized for production
- **Environment Configuration**: Separate settings for Development/Production
- **Health Checks**: Built-in monitoring endpoints
- **Scalability**: Designed for horizontal scaling

## 📄 API Documentation

Once the application is running, visit http://localhost:5000/swagger to explore the complete API documentation with interactive testing capabilities.
