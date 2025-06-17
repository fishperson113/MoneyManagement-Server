# MoneyManagement Server - Project Structure & Integration Guide

## 📋 Project Overview

The MoneyManagement Server is a comprehensive ASP.NET Core 8 financial management application with social features, following a layered architecture pattern with clear separation of concerns.

### 🏗️ Core Architecture

```
API/
├── Controllers/          # API endpoints (presentation layer)
├── Services/            # Business logic services
├── Repositories/        # Data access layer
├── Models/
│   ├── Entities/        # Database entities
│   └── DTOs/           # Data transfer objects
├── Data/               # Database context and configurations
├── Helpers/            # Utilities, mappers, extensions
├── Hub/                # SignalR hubs for real-time features
├── Migrations/         # Entity Framework migrations
└── Config/             # Configuration classes
```

## 🔄 Application Flow

### 1. Request Flow
```
HTTP Request → Controller → Service → Repository → Database
HTTP Response ← Controller ← Service ← Repository ← Database
```

### 2. Key Components

#### Controllers (Presentation Layer)
- Handle HTTP requests/responses
- Validate input using DTOs
- Return appropriate HTTP status codes
- Use `[Authorize]` attribute for authentication

#### Services (Business Logic Layer)
- Implement complex business rules
- Coordinate between multiple repositories
- Handle cross-cutting concerns
- Examples: `ReportService`, `StatisticService`, `GeminiService`

#### Repositories (Data Access Layer)
- Implement CRUD operations
- Handle Entity Framework queries
- Use AutoMapper for entity/DTO mapping
- Follow repository pattern with interfaces

#### Models
- **Entities**: Database models with EF Core configurations
- **DTOs**: Data transfer objects for API communication

## 🗄️ Database Schema

### Core Financial Entities
```
ApplicationUser (Identity)
├── Wallets (1:N)
│   └── Transactions (1:N)
│       └── Category (N:1)
└── Categories (1:N)
```

### Social Features
```
ApplicationUser
├── FriendRequestsSent (1:N)
├── FriendRequestsReceived (1:N)
├── Posts (1:N)
│   ├── Comments (1:N)
│   └── Likes (1:N)
├── Groups (1:N) - as Creator
└── GroupMemberships (1:N) - as Member
```

### Group Financial Management
```
Group
├── Members (GroupMember)
├── Funds (GroupFund)
│   └── GroupTransactions (1:N)
└── Messages (GroupMessage)
```

## 🔧 Dependency Injection Configuration

### Service Registration (Program.cs)
```csharp
// Repositories
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Services
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IStatisticService, StatisticService>();
builder.Services.AddScoped<GeminiService>();

// Infrastructure
builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddAutoMapper(typeof(ApplicationMapper));
builder.Services.AddSignalR();
```

## 🔐 Authentication & Authorization

### JWT Token-Based Authentication
- Uses ASP.NET Core Identity with custom `ApplicationUser`
- JWT tokens with refresh token support
- Role-based authorization (`Customer`, `Admin`)

### SignalR Authentication
- Supports JWT token via query string for WebSocket connections
- Real-time chat and notifications

## 🎯 How to Add New Features Without Breaking Legacy Code

### ⚠️ CRITICAL RULES - DO NOT MODIFY

1. **Never modify existing entity properties or relationships**
2. **Never change existing controller endpoints or method signatures**
3. **Never alter existing DTO structures**
4. **Never modify existing database indexes or constraints**
5. **Always create new migrations for database changes**

### ✅ Safe Integration Patterns

#### 1. Adding New Entities
```csharp
// 1. Create new entity in Models/Entities/
public class NewFeature
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    // Add navigation properties carefully
}

// 2. Add to ApplicationDbContext
public DbSet<NewFeature> NewFeatures { get; set; } = null!;

// 3. Configure relationships in OnModelCreating
modelBuilder.Entity<NewFeature>()
    .HasOne(nf => nf.User)
    .WithMany() // Don't add to ApplicationUser unless necessary
    .HasForeignKey(nf => nf.UserId);
```

#### 2. Adding New Controllers
```csharp
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NewFeatureController : ControllerBase
{
    private readonly INewFeatureRepository _repository;
    private readonly ILogger<NewFeatureController> _logger;

    public NewFeatureController(
        INewFeatureRepository repository,
        ILogger<NewFeatureController> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    // Implement CRUD operations
}
```

#### 3. Adding New Services
```csharp
public interface INewFeatureService
{
    Task<ResultDTO> ProcessNewFeatureAsync(InputDTO input);
}

public class NewFeatureService : INewFeatureService
{
    private readonly INewFeatureRepository _repository;
    private readonly ILogger<NewFeatureService> _logger;
    
    // Register in Program.cs:
    // builder.Services.AddScoped<INewFeatureService, NewFeatureService>();
}
```

#### 4. Extending Existing Entities (Carefully)
```csharp
// Instead of modifying ApplicationUser directly:
public class UserPreferences
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public string? NotificationSettings { get; set; }
    // Add new properties here
}

// Then create a one-to-one relationship
modelBuilder.Entity<UserPreferences>()
    .HasOne(up => up.User)
    .WithOne() // Don't modify ApplicationUser
    .HasForeignKey<UserPreferences>(up => up.UserId);
```

### 🔄 AutoMapper Integration
```csharp
// Add to ApplicationMapper.cs
CreateMap<NewFeature, NewFeatureDTO>()
    .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName));

CreateMap<CreateNewFeatureDTO, NewFeature>()
    .ForMember(dest => dest.Id, opt => opt.Ignore())
    .ForMember(dest => dest.User, opt => opt.Ignore());
```

### 📊 Adding New Endpoints to Existing Controllers

#### ❌ DON'T: Modify existing methods
```csharp
// DON'T modify this existing method
[HttpGet]
public async Task<ActionResult<IEnumerable<WalletDTO>>> GetAllWallets()
{
    // Existing implementation - DON'T CHANGE
}
```

#### ✅ DO: Add new endpoints
```csharp
// ADD new endpoints with different routes
[HttpGet("detailed")]
public async Task<ActionResult<IEnumerable<DetailedWalletDTO>>> GetDetailedWallets()
{
    // New implementation
}

[HttpGet("{id}/summary")]
public async Task<ActionResult<WalletSummaryDTO>> GetWalletSummary(Guid id)
{
    // New implementation
}
```

### 🗃️ Database Migration Strategy
```powershell
# 1. Always create new migration for changes
cd API
dotnet ef migrations add AddNewFeature

# 2. Review generated migration before applying
# 3. Test migration on development database first
dotnet ef database update

# 4. Ensure rollback capability if needed
dotnet ef migrations remove  # If issues found before commit
```

### 🔍 Testing New Features
```csharp
// Create tests in API.Test project
[TestFixture]
public class NewFeatureRepositoryTests
{
    [SetUp]
    public void Setup()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        // Setup test dependencies
    }
    
    [Test]
    public async Task CreateNewFeature_ShouldReturnSuccess()
    {
        // Arrange, Act, Assert
    }
}
```

## 🚨 Legacy Code Protection

### Areas with Multiple Developers Working
1. **Controllers**: Add new endpoints only, don't modify existing
2. **Entities**: Create new entities instead of modifying existing ones
3. **DTOs**: Create new DTOs for new features
4. **Repositories**: Implement new interfaces, don't change existing methods
5. **Database Schema**: Only additive changes through migrations

### Communication Protocol
1. **Before adding features**: Check with team about potential conflicts
2. **Code reviews**: Mandatory for any changes near core entities
3. **Database changes**: Coordinate migrations with team
4. **Testing**: Ensure existing tests still pass

## 📚 Development Workflow

### 1. Feature Planning
- Identify affected areas
- Plan new entities/DTOs needed
- Design API endpoints
- Consider database changes

### 2. Implementation Order
1. Create new entities and DTOs
2. Add database migration
3. Implement repository layer
4. Add service layer (if needed)
5. Create controller endpoints
6. Add AutoMapper configurations
7. Write unit tests
8. Update API documentation

### 3. Quality Assurance
- Run existing test suite
- Test new functionality
- Verify no breaking changes
- Check performance impact

## 🔧 Common Patterns

### Repository Pattern
```csharp
public class NewFeatureRepository : INewFeatureRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Standard constructor pattern
    // Implement GetCurrentUserId() for user context
    // Use _mapper for entity/DTO conversion
    // Log operations for debugging
}
```

### Error Handling
```csharp
try
{
    // Business logic
    return Ok(result);
}
catch (KeyNotFoundException ex)
{
    _logger.LogWarning(ex, "Resource not found");
    return NotFound();
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogWarning(ex, "Unauthorized access");
    return Unauthorized();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, "An error occurred");
}
```

## 🔄 SignalR Real-Time Communication Workflow

### 📡 SignalR Architecture Overview

The application uses SignalR for real-time bidirectional communication between the server and clients. Unlike traditional HTTP requests, SignalR maintains persistent connections for instant messaging, live updates, and real-time notifications.

```
Client (JavaScript) ←→ SignalR Hub ←→ Services/Repositories ←→ Database
     ↕                    ↕                    ↕
Other Clients        IChatClient         Event Broadcasting
```

### 🏗️ Core SignalR Components

#### 1. ChatHub (`API/Hub/ChatHub.cs`)
The main SignalR hub that handles all real-time communications:

```csharp
public class ChatHub : Hub<IChatClient>
{
    // Connection tracking
    private static readonly ConcurrentDictionary<string, HashSet<string>> OnlineUsers = new();
    
    // Dependencies
    private readonly MessageRepository _messageRepository;
    private readonly FriendRepository _friendRepository;
    private readonly GroupRepository _groupRepository;
}
```

#### 2. IChatClient Interface (`API/Models/Entities/IChatClient.cs`)
Defines client-side methods that the server can invoke:

```csharp
public interface IChatClient
{
    Task ReceiveMessage(MessageDTO message);
    Task UserOnline(string userId);
    Task UserOffline(string userId);
    Task MessageRead(string messageId, string userId);
    Task NewUnreadMessages(string fromUserId);
    Task FriendRequestReceived(FriendRequestDTO request);
    Task FriendRequestAccepted(string userId);
    Task UserAvatarUpdated(string userId, string newAvatarUrl);
    Task ReceiveGroupMessage(GroupMessageDTO message);
    Task GroupMessagesRead(Guid groupId, string userId);
    Task UserAddedToGroup(GroupDTO group);
    Task UserRemovedFromGroup(Guid groupId, string userId);
    Task NewUnreadGroupMessages(Guid groupId);
    Task UserRoleChanged(Guid groupId, string userId, GroupRole newRole);
    Task GroupDeleted(Guid groupId);
}
```

### 🔌 Connection Lifecycle

#### Connection Establishment
```csharp
// Client connects with JWT authentication
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", {
        accessTokenFactory: () => getJWTToken()
    })
    .withAutomaticReconnect()
    .build();

await connection.start();
```

#### OnConnectedAsync Flow
```csharp
public override async Task OnConnectedAsync()
{
    var userId = Context.UserIdentifier;
    
    // 1. Track user as online
    OnlineUsers[userId].Add(Context.ConnectionId);
    
    // 2. Auto-join user groups
    var userGroups = await _groupRepository.GetUserGroupsAsync(userId);
    foreach (var group in userGroups)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{group.GroupId}");
    }
    
    // 3. Notify friends user is online
    var friends = await _friendRepository.GetUserFriendsAsync(userId);
    foreach (var friend in friends)
    {
        await Clients.User(friend.UserId).UserOnline(userId);
    }
    
    // 4. Check for unread messages
    await CheckUnreadMessages(userId);
}
```

#### OnDisconnectedAsync Flow
```csharp
public override async Task OnDisconnectedAsync(Exception exception)
{
    var userId = Context.UserIdentifier;
    
    // 1. Remove connection ID
    OnlineUsers[userId].Remove(Context.ConnectionId);
    
    // 2. If no more connections, mark user offline
    if (OnlineUsers[userId].Count == 0)
    {
        OnlineUsers.TryRemove(userId, out _);
        
        // 3. Notify friends user is offline
        var friends = await _friendRepository.GetUserFriendsAsync(userId);
        foreach (var friend in friends)
        {
            await Clients.User(friend.UserId).UserOffline(userId);
        }
    }
}
```

### 💬 Real-Time Messaging Workflows

#### 1. Private Messaging
```csharp
// Server-side: Send message to specific user
public async Task SendMessageToUser(string receiverId, SendMessageDto messageDto)
{
    var senderId = Context.UserIdentifier;
    
    // 1. Save message to database
    var message = await _messageRepository.SendMessageAsync(senderId, messageDto);
    
    // 2. Send real-time notification if receiver is online
    if (OnlineUsers.ContainsKey(receiverId))
    {
        await Clients.User(receiverId).ReceiveMessage(message);
    }
    
    // 3. Echo back to sender for consistency
    await Clients.Caller.ReceiveMessage(message);
}
```

```javascript
// Client-side: Send and receive messages
await connection.invoke("SendMessageToUser", receiverId, messageData);

connection.on("ReceiveMessage", (message) => {
    displayMessage(message);
});
```

#### 2. Group Messaging
```csharp
// Server-side: Send message to group
public async Task SendMessageToGroup(SendGroupMessageDTO messageDto)
{
    var senderId = Context.UserIdentifier;
    
    // 1. Save group message
    var message = await _groupRepository.SendGroupMessageAsync(senderId, messageDto);
    
    // 2. Get all group members
    var groupMembers = await _groupRepository.GetGroupMembersAsync(senderId, messageDto.GroupId);
    
    // 3. Send to all online members
    foreach (var member in groupMembers)
    {
        if (OnlineUsers.ContainsKey(member.UserId))
        {
            await Clients.User(member.UserId).ReceiveGroupMessage(message);
        }
    }
}
```

```javascript
// Client-side: Join group and send messages
await connection.invoke("JoinGroupChat", groupId);
await connection.invoke("SendMessageToGroup", { groupId, content });

connection.on("ReceiveGroupMessage", (message) => {
    if (currentActiveGroupId === message.groupId) {
        displayGroupMessage(message);
    }
});
```

### 👥 Friend System Integration

#### Friend Request Workflow
```csharp
// When friend request is sent (via REST API)
// The repository triggers SignalR notification:

public async Task SendFriendRequest(string friendId)
{
    // ... save request to database ...
    
    // Real-time notification
    if (OnlineUsers.ContainsKey(friendId))
    {
        await Clients.User(friendId).FriendRequestReceived(requestDTO);
    }
}
```

#### Online Status Broadcasting
```csharp
// Only friends see each other's online status
var friends = await _friendRepository.GetUserFriendsAsync(userId);
foreach (var friend in friends)
{
    await Clients.User(friend.UserId).UserOnline(userId);
}
```

### 🔔 Notification System

#### Unread Message Notifications
```csharp
// On connection, check for unread messages
var unreadChats = await _messageRepository.GetUserChatsAsync(userId);
foreach (var chat in unreadChats)
{
    if (chat.Messages.Any(m => m.ReceiverId == userId))
    {
        await Clients.User(userId).NewUnreadMessages(chat.OtherUserId);
    }
}
```

#### Read Receipt System
```csharp
public async Task MarkMessageAsRead(string messageId, string senderId)
{
    var userId = Context.UserIdentifier;
    
    // Update database
    await _messageRepository.MarkMessagesAsReadAsync(userId, senderId);
    
    // Notify sender
    await Clients.User(senderId).MessageRead(messageId, userId);
}
```

### 🔧 SignalR Configuration

#### Program.cs Setup
```csharp
builder.Services.AddSignalR();

// JWT Authentication for SignalR
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && 
                    path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Register hub
app.MapHub<ChatHub>("/hubs/chat");
```

#### CORS Configuration for SignalR
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://your-frontend-domain")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});
```

### 🚨 Adding New SignalR Features Safely

#### ⚠️ DON'T: Modify existing SignalR methods
```csharp
// DON'T modify existing hub methods
public async Task SendMessageToUser(string receiverId, SendMessageDto messageDto)
{
    // DON'T change method signature or core logic
}
```

#### ✅ DO: Add new hub methods
```csharp
// ADD new methods for new features
public async Task SendNewFeatureNotification(string userId, NewFeatureDTO data)
{
    await Clients.User(userId).ReceiveNewFeatureNotification(data);
}

public async Task JoinNewFeatureGroup(string groupName)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"feature_{groupName}");
}
```

#### ✅ DO: Extend IChatClient interface
```csharp
public interface IChatClient
{
    // ...existing methods...
    
    // ADD new client methods
    Task ReceiveNewFeatureNotification(NewFeatureDTO data);
    Task NewFeatureStatusUpdated(string featureId, string status);
}
```

### 📊 SignalR Groups Management

#### User Groups
```csharp
// Each user automatically joins their personal group
await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
```

#### Chat Groups
```csharp
// Users join SignalR groups for each chat group they're in
await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
```

#### Feature-Specific Groups
```csharp
// Create groups for specific features
await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications_{userId}");
await Groups.AddToGroupAsync(Context.ConnectionId, $"reports_{departmentId}");
```

### 🔄 Integration with REST APIs

SignalR works alongside REST APIs, not as a replacement:

#### When to Use SignalR
- ✅ Real-time messaging
- ✅ Live notifications
- ✅ Online status updates
- ✅ Live data updates
- ✅ Broadcasting events

#### When to Use REST APIs
- ✅ CRUD operations
- ✅ File uploads
- ✅ Authentication
- ✅ Data querying
- ✅ Bulk operations

#### Hybrid Approach Example
```csharp
// REST API creates the data
[HttpPost]
public async Task<IActionResult> CreateTransaction(CreateTransactionDTO dto)
{
    var transaction = await _repository.CreateTransactionAsync(dto);
    
    // SignalR broadcasts the update
    await _hubContext.Clients.User(GetCurrentUserId())
        .TransactionCreated(transaction);
    
    return Ok(transaction);
}
```

### 🧪 Testing SignalR Features

#### Integration Testing
```csharp
[Test]
public async Task SendMessage_ShouldNotifyReceiver()
{
    // Setup test hub context
    var hubContext = new Mock<IHubContext<ChatHub, IChatClient>>();
    
    // Test SignalR method
    await chatHub.SendMessageToUser(receiverId, messageDto);
    
    // Verify notification was sent
    hubContext.Verify(x => x.Clients.User(receiverId)
        .ReceiveMessage(It.IsAny<MessageDTO>()), Times.Once);
}
```

#### Client-Side Testing
Use the provided test client (`wwwroot/chat-test.html`) for manual testing:
- Connect with JWT token
- Test messaging features
- Verify real-time updates
- Check group functionality

### 🔧 Performance Considerations

#### Connection Scaling
```csharp
// For production, use Redis backplane
services.AddSignalR()
    .AddStackExchangeRedis("connection-string");
```

#### Memory Management
```csharp
// Cleanup disconnected users
private static readonly Timer CleanupTimer = new Timer(
    CleanupOfflineUsers, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
```

#### Message Queuing
- Online users: Real-time delivery via SignalR
- Offline users: Messages stored in database
- On reconnection: Deliver queued messages

This structure ensures scalability while protecting existing functionality across multiple development teams.
