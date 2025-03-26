using API.Data;
using API.Models.Entities;
using API.Repositories;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using API.Helpers;
using AutoMapper;
using Moq;
using Microsoft.Extensions.Options;
namespace API.Test
{
    [TestFixture]
    public class AccountRepositoryTests
    {
        private ApplicationDbContext context;
        private UserManager<ApplicationUser> userManager;
        private RoleManager<IdentityRole> roleManager;
        private IConfiguration configuration;
        private ILogger<AccountRepository> logger;
        private IAccountRepository accountRepository;
        private Mock<IMapper> mapperMock;
        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new ApplicationDbContext(options);

            userManager = GetUserManager(context);
            roleManager = GetRoleManager(context);

            var inMemorySettings = new Dictionary<string, string>
            {
                {"AppSettings:BaseUrl", "http://localhost:5000"},
                {"JWT:Secret", "8jK9pL2mN7vQ5rX8tY0uW3zA6cD9fG2hJ5kM8nP1qS4tV7wY0zB3eH6iL9oR2uT5vX8yA1dF4gJ7mN0pQ3sU6wZ9"},
                {"JWT:ValidIssuer", "http://localhost:5000"},
                {"JWT:ValidAudience", "http://localhost:5000"}
            };
            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            logger = NullLogger<AccountRepository>.Instance;
            mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<ApplicationUser>(It.IsAny<SignUpDTO>()))
                .Returns((SignUpDTO dto) => new ApplicationUser
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    UserName = dto.Email
                });

            accountRepository = new AccountRepository(
                userManager,
                null!,
                configuration,
                roleManager,
                logger,
                context,
                mapperMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            userManager?.Dispose();
            roleManager?.Dispose();
            context?.Dispose();
        }

        [Test]
        public async Task ClearDatabaseAsync_RemovesAllUsersAndRoles()
        {
            var user = new ApplicationUser { Email = "test@example.com", UserName = "test@example.com" };
            await userManager.CreateAsync(user, "Password123!");
            await roleManager.CreateAsync(new IdentityRole("Customer"));

            Assert.IsNotEmpty(await userManager.Users.ToListAsync());
            Assert.IsNotEmpty(await roleManager.Roles.ToListAsync());

            await accountRepository.ClearDatabaseAsync();

            Assert.IsEmpty(await userManager.Users.ToListAsync());
            Assert.IsEmpty(await roleManager.Roles.ToListAsync());
        }
        [Test]
        public async Task RefreshTokenAsync_InvalidToken_ReturnsError()
        {
            var refreshTokenDTO = new RefreshTokenDTO
            {
                Token = "invalidToken",
                RefreshToken = "someRefreshToken"
            };

            var result = await accountRepository.RefreshTokenAsync(refreshTokenDTO);

            Assert.IsFalse(result.Errors == null || !result.Errors.Any());
            Assert.AreEqual("Invalid Token", result.Errors.First());
        }
        [Test]
        public async Task RefreshTokenAsync_ValidToken_ReturnsNewToken()
        {
            var user = new ApplicationUser { Email = "test@example.com", UserName = "test@example.com" };
            await userManager.CreateAsync(user, "Password123!");

            // Create an expired token manually
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret not found in configuration"));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("id", user.Id)
            };
            var now = DateTime.UtcNow;
            var pastTime = now.AddHours(-1);  
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = pastTime,  
                IssuedAt = pastTime,   
                Expires = pastTime.AddMinutes(30), 
                Audience = configuration["JWT:ValidAudience"],
                Issuer = configuration["JWT:ValidIssuer"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha512Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);
            var tokenId = token.Id;

            // Create refresh token
            var refreshTokenString = Guid.NewGuid().ToString();
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                JwtId = tokenId,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                CreationDate = DateTime.UtcNow.AddDays(-1)
            };

            if (context.RefreshTokens != null)
            {
                await context.RefreshTokens.AddAsync(refreshToken);
                await context.SaveChangesAsync();
            }
            else
            {
                Assert.Fail("RefreshTokens DbSet is null");
            }

            var refreshTokenDTO = new RefreshTokenDTO
            {
                Token = jwtToken,
                RefreshToken = refreshTokenString
            };

            var result = await accountRepository.RefreshTokenAsync(refreshTokenDTO);

            if (result.Errors != null && result.Errors.Any())
            {
                Console.WriteLine("Error: " + string.Join(", ", result.Errors));
            }

            Assert.Multiple(() =>
            {
                Assert.That(result.Errors == null || !result.Errors.Any(), Is.True, "Refresh token operation returned errors");
                Assert.That(result.Token, Is.Not.Null, "Token should not be null");
                Assert.That(result.RefreshToken, Is.Not.Null, "Refresh token should not be null");
            });

        }
        [Test]
        public async Task SignUpAsync_ValidData_ReturnsSuccessResult()
        {
            // Arrange
            var signUpDto = new SignUpDTO
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            var result = await accountRepository.SignUpAsync(signUpDto);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True, "User registration should succeed");

                var user = userManager.FindByEmailAsync(signUpDto.Email).Result;
                Assert.That(user, Is.Not.Null, "User should exist in the database");
                Assert.That(user.FirstName, Is.EqualTo(signUpDto.FirstName), "FirstName should match");
                Assert.That(user.LastName, Is.EqualTo(signUpDto.LastName), "LastName should match");
                Assert.That(user.Email, Is.EqualTo(signUpDto.Email), "Email should match");

                var roles = userManager.GetRolesAsync(user).Result;
                Assert.That(roles, Contains.Item(AppRole.Customer), "User should have Customer role");
            });
        }

        [Test]
        public async Task SignUpAsync_InvalidPassword_ReturnsFailed()
        {
            // Arrange
            var signUpDto = new SignUpDTO
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "weak", // Too weak password
                ConfirmPassword = "weak"
            };

            // Act
            var result = await accountRepository.SignUpAsync(signUpDto);

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False, "Registration should fail with weak password");
                Assert.That(result.Errors, Is.Not.Empty, "Should contain error messages");

                Assert.That(result.Errors.Any(e =>
                    e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)),
                    Is.True, "Should contain password-related error");

                var user = userManager.FindByEmailAsync(signUpDto.Email).Result;
                Assert.That(user, Is.Null, "User should not exist in the database");
            });
        }

        [Test]
        public async Task SignUpAsync_DuplicateEmail_ReturnsFailed()
        {
            // Arrange - Create a user first
            var existingUser = new ApplicationUser
            {
                Email = "existing@example.com",
                UserName = "existing@example.com",
                FirstName = "Existing",
                LastName = "User"
            };
            await userManager.CreateAsync(existingUser, "Password123!");

            // Arrange - Try to register with the same email
            var signUpDto = new SignUpDTO
            {
                FirstName = "Another",
                LastName = "User",
                Email = "existing@example.com", // Same email as existing user
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            var result = await accountRepository.SignUpAsync(signUpDto);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False, "Registration should fail with duplicate email");
                Assert.That(result.Errors, Is.Not.Empty, "Should contain error messages");
                Assert.That(result.Errors.Any(e =>
                    e.Code == "DuplicateUserName" ||
                    e.Code == "DuplicateEmail"),
                    Is.True, "Should contain duplicate user/email error");
            });
        }

        [Test]
        public async Task SignUpAsync_MapperConfiguredCorrectly()
        {
            // This test verifies that the AutoMapper is correctly mapping SignUpDTO to ApplicationUser

            // Arrange
            var signUpDto = new SignUpDTO
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<ApplicationUser>(It.IsAny<SignUpDTO>()))
                .Returns((SignUpDTO dto) => new ApplicationUser
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    UserName = dto.Email
                });

            var accountRepositoryWithMock = new AccountRepository(
                userManager,
                null!,
                configuration,
                roleManager,
                logger,
                context,
                mapperMock.Object);

            // Act
            var result = await accountRepositoryWithMock.SignUpAsync(signUpDto);

            // Assert
            mapperMock.Verify(m => m.Map<ApplicationUser>(It.Is<SignUpDTO>(dto =>
                dto.Email == signUpDto.Email &&
                dto.FirstName == signUpDto.FirstName &&
                dto.LastName == signUpDto.LastName)),
                Times.Once,
                "Mapper should be called once with the correct SignUpDTO");

            Assert.That(result.Succeeded, Is.True, "User registration should succeed");
        }

        private UserManager<ApplicationUser> GetUserManager(ApplicationDbContext context)
        {
            var store = new UserStore<ApplicationUser>(context);
            var options = new IdentityOptions();

            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            var userValidators = new List<IUserValidator<ApplicationUser>>
            {
                new UserValidator<ApplicationUser>()
            };

                    var passwordValidators = new List<IPasswordValidator<ApplicationUser>>
            {
                new PasswordValidator<ApplicationUser>()
            };

            return new UserManager<ApplicationUser>(
                store,
                new OptionsWrapper<IdentityOptions>(options),
                new PasswordHasher<ApplicationUser>(),
                userValidators,
                passwordValidators,
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                NullLogger<UserManager<ApplicationUser>>.Instance
            );
        }

        private RoleManager<IdentityRole> GetRoleManager(ApplicationDbContext context)
        {
            var store = new RoleStore<IdentityRole>(context);
            return new RoleManager<IdentityRole>(
                store,
                new List<IRoleValidator<IdentityRole>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                NullLogger<RoleManager<IdentityRole>>.Instance
            );
        }
    }
}
