using API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using API.Repositories;
using System.Text;
using Microsoft.OpenApi.Models;
using API.Models.Entities;
using API.Services; 
using System.Security.Cryptography.X509Certificates;
using API.Helpers;
using Microsoft.Extensions.DependencyInjection;
using API.Config;
using API.Hub;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"appsettings.json", optional: true).AddEnvironmentVariables();


// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new API.Helpers.JsonDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new API.Helpers.JsonNullableDateTimeConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "MoneyManagement API", Version = "v1" });

    option.EnableAnnotations();
    option.OperationFilter<SwaggerFileOperationFilter>();
    option.SchemaFilter<SwaggerSchemaExampleFilter>();

    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    option.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        //temporary disable email confirmation
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityApiEndpoints<ApplicationUser>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddAutoMapper(typeof(Program), typeof(ApplicationMapper));

builder.Services.ConfigureFirebase(builder.Configuration);
builder.Services.ConfigureFirebaseStorage(builder.Configuration);

builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUserProfileMediator, UserProfileMediator>();
builder.Services.AddScoped<SeedService>();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<FriendRepository>();
builder.Services.AddScoped<GroupRepository>();
builder.Services.AddScoped<IGroupFundRepository, GroupFundRepository>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JWT"));
builder.Services.AddSignalR();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"] ?? throw new ArgumentNullException("JWT:ValidAudience"),
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"] ?? throw new ArgumentNullException("JWT:ValidIssuer"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"] ?? throw new ArgumentNullException("JWT:Secret")))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://143.198.208.227") // 👈 Đặt đúng origin client
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Cho phép gửi cookie/token
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("LocalDevelopment"))
{
    builder.WebHost.UseUrls("http://*:5000");
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var seedService = services.GetRequiredService<SeedService>();

    dbContext.Database.Migrate();

    await seedService.SeedAdminUserAndRole();
    await seedService.SeedTestUserWithData();
}



app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/register", StringComparison.OrdinalIgnoreCase) 
    || context.Request.Path.StartsWithSegments("/login", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Endpoint đăng ký đã bị vô hiệu hóa.");
        return;
    }
    await next();
});
app.MapGet("/ping", () => "pong");
app.MapHub<ChatHub>("/hubs/chat");
app.UseStaticFiles();
app.UseRouting();
app.UseCors();

app.MapIdentityApi<ApplicationUser>().RequireAuthorization();

//app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

