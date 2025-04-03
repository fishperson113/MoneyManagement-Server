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
using API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
namespace API.Test
{
    [TestFixture]
    public class AccountRepositoryTests
    {
        private ApplicationDbContext context;
        private UserManager<ApplicationUser> userManager;
        private SignInManager<ApplicationUser> signInManager;
        private RoleManager<IdentityRole> roleManager;
        private IConfiguration configuration;
        private ILogger<AccountRepository> logger;
        private IAccountRepository accountRepository;
        private Mock<IMapper> mapperMock;
        private Mock<ITokenService> tokenService;
        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            context = new ApplicationDbContext(options);

            userManager = GetUserManager(context);
            roleManager = GetRoleManager(context);
            signInManager = GetSignInManager(context);

            var inMemorySettings = new Dictionary<string, string?>
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

            tokenService = new Mock<ITokenService>();
            tokenService.Setup(ts => ts.GenerateTokensAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new AuthenticationResult
                {
                    Token = "test-access-token",
                    Success = true
                });
            tokenService.Setup(ts => ts.RefreshTokenAsync(It.IsAny<string>()))
               .ReturnsAsync((string token) => {
                   if (token == "invalidToken")
                   {
                       return new AuthenticationResult
                       {
                           Errors = new[] { "Invalid Token" },
                           Success = false
                       };
                   }

                   return new AuthenticationResult
                   {
                       Token = "new-test-access-token",
                       Success = true
                   };
               });

            accountRepository = new AccountRepository(
                userManager,
                signInManager,
                configuration,
                roleManager,
                logger,
                context,
                mapperMock.Object,
                tokenService.Object
            );
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
            // Setup - Create user and role
            var user = new ApplicationUser { Email = "test@example.com", UserName = "test@example.com" };
            await userManager.CreateAsync(user, "Password123!");

            // Ensure Customer role exists
            if (!await roleManager.RoleExistsAsync(AppRole.Customer))
            {
                await roleManager.CreateAsync(new IdentityRole(AppRole.Customer));
            }

            // Add user to role
            await userManager.AddToRoleAsync(user, AppRole.Customer);

            // Verify setup is correct
            var initialUsers = await userManager.Users.ToListAsync();
            var initialRoles = await roleManager.Roles.ToListAsync();

            Assert.Multiple(() =>
            {
                Assert.That(initialUsers.Count, Is.GreaterThan(0), "Should have at least one user before cleanup");
                Assert.That(initialRoles.Count, Is.GreaterThan(0), "Should have at least one role before cleanup");
               // Assert.IsTrue(await userManager.IsInRoleAsync(user, AppRole.Customer), "User should be in Customer role");
            });

            // Act
            var result = await accountRepository.ClearDatabaseAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.IsTrue(result, "ClearDatabaseAsync should return true on success");

                var remainingUsers = userManager.Users.ToList();
                var remainingRoles = roleManager.Roles.ToList();

                Assert.That(remainingUsers.Count, Is.EqualTo(0), $"All users should be removed, but found {remainingUsers.Count}");
                Assert.That(remainingRoles.Count, Is.EqualTo(0), $"All roles should be removed, but found {remainingRoles.Count}");
            });

        }
        [Test]
        public async Task RefreshTokenAsync_InvalidToken_ReturnsError()
        {
            // Arrange - Set up a test with an invalid token
            var refreshTokenDTO = new RefreshTokenDTO
            {
                ExpiredToken = "invalidToken"
            };

            // Act
            var result = await accountRepository.RefreshTokenAsync(refreshTokenDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, "Should not succeed with invalid token");
                Assert.That(result.Errors, Is.Not.Null.And.Not.Empty, "Should have error messages");
                Assert.That(result.Errors.First(), Is.EqualTo("Invalid Token"), "Error message should match");
            });

            // Verify - Ensure token service was called with the right parameter
            tokenService.Verify(ts => ts.RefreshTokenAsync("invalidToken"), Times.Once);

        }
        [Test]
        public async Task RefreshTokenAsync_ValidToken_ReturnsNewToken()
        {
            // Arrange - Set up a test with a valid token
            var validToken = "validToken";
            var refreshTokenDTO = new RefreshTokenDTO
            {
                ExpiredToken = validToken
            };

            // Act
            var result = await accountRepository.RefreshTokenAsync(refreshTokenDTO);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, "Should succeed with valid token");
                Assert.That(result.Token, Is.EqualTo("new-test-access-token"), "Access token should match");
                Assert.That(result.Errors, Is.Null.Or.Empty, "Should not have errors");
            });

            // Verify - Ensure token service was called with the right parameter
            tokenService.Verify(ts => ts.RefreshTokenAsync(validToken), Times.Once);


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
                // Update the assertion to handle the possibility of null values
                Assert.That(user?.FirstName, Is.EqualTo(signUpDto.FirstName), "FirstName should match");
                Assert.That(user?.LastName, Is.EqualTo(signUpDto.LastName), "LastName should match");
                Assert.That(user?.Email, Is.EqualTo(signUpDto.Email), "Email should match");

                if (user != null)
                {
                    var roles = userManager.GetRolesAsync(user).Result;
                    Assert.That(roles, Contains.Item(AppRole.Customer), "User should have Customer role");
                }
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
        public async Task SignInAsync_ValidCredentials_CreatesAccessAndRefreshTokens()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Email = "test@example.com",
                UserName = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            await userManager.CreateAsync(user, "Password123!");

            var signInDto = new SignInDTO
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var mockRefreshToken = new RefreshToken
            {
                Token = "test-refresh-token",
                JwtId = "test-jwt-id",
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                Invalidated = false
            };

            // Mock the token service to simulate generating both tokens
            tokenService.Reset();
            tokenService.Setup(ts => ts.GenerateTokensAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync((ApplicationUser u) =>
                {
                    // Simulate creating a refresh token by adding it to the database
                    if (context.RefreshTokens != null)
                    {
                        context.RefreshTokens.Add(mockRefreshToken);
                        context.SaveChanges();
                    }

                    return new AuthenticationResult
                    {
                        Token = "test-access-token",
                        Success = true
                    };
                });

            // Act
            var result = await accountRepository.SignInAsync(signInDto);

            // Assert
            Assert.Multiple(() =>
            {
                // Check authentication result
                Assert.That(result.Success, Is.True, "Authentication should succeed");
                Assert.That(result.Token, Is.Not.Null.Or.Empty, "Access token should not be null or empty");
          
                // Verify token service was called with the right user
                tokenService.Verify(ts => ts.GenerateTokensAsync(
                    It.Is<ApplicationUser>(u => u.Id == user.Id)),
                    Times.Once);

                // Check that a refresh token was actually added to the database
                var storedToken = context.RefreshTokens?.FirstOrDefault(rt => rt.UserId == user.Id);
                Assert.That(storedToken, Is.Not.Null, "Refresh token should be saved in the database");
                Assert.That(storedToken?.Token, Is.EqualTo(mockRefreshToken.Token), "Stored token should match the generated token");
                Assert.That(storedToken?.UserId, Is.EqualTo(user.Id), "Token should be associated with the correct user");
                Assert.That(storedToken?.Invalidated, Is.False, "Token should not be invalidated");
            });
        }

        [Test]
        public async Task RefreshTokenAsync_ValidToken_UpdatesDatabaseAndReturnsNewTokens()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Email = "test@example.com",
                UserName = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            await userManager.CreateAsync(user, "Password123!");

            // Create an initial JWT ID
            var initialJwtId = Guid.NewGuid().ToString();

            // Create an initial refresh token in the database
            var initialRefreshToken = new RefreshToken
            {
                Token = "initial-refresh-token",
                JwtId = initialJwtId,
                UserId = user.Id,
                CreationDate = DateTime.UtcNow.AddHours(-1),
                ExpiryDate = DateTime.UtcNow.AddDays(6),
                Invalidated = false
            };

            // Add the initial refresh token to the database
            await context.RefreshTokens!.AddAsync(initialRefreshToken);
            await context.SaveChangesAsync();

            // Mock the token retrieval from expired token
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, initialJwtId)
            }));

            var mockNewRefreshToken = new RefreshToken
            {
                Token = "new-refresh-token",
                JwtId = "new-jwt-id",
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                Invalidated = false
            };

            // Set up token service mock
            tokenService.Reset();
            tokenService.Setup(ts => ts.GetPrincipalFromExpiredToken("valid-expired-token"))
                .Returns(claimsPrincipal);

            tokenService.Setup(ts => ts.RefreshTokenAsync("valid-expired-token"))
                .ReturnsAsync(() =>
                {
                    // Simulate creating a new refresh token by adding it to the database
                    // and marking the old one as used
                    if (context.RefreshTokens != null)
                    {
                        // Mark original token as invalidated
                        var token = context.RefreshTokens.Find(initialRefreshToken.Id);
                        if (token != null)
                        {
                            token.Invalidated = true;
                            context.RefreshTokens.Update(token);
                        }

                        // Add new refresh token
                        context.RefreshTokens.Add(mockNewRefreshToken);
                        context.SaveChanges();
                    }

                    return new AuthenticationResult
                    {
                        Token = "new-access-token",
                        Success = true
                    };
                });

            var refreshTokenDTO = new RefreshTokenDTO
            {
                ExpiredToken = "valid-expired-token"
            };

            // Act
            var result = await accountRepository.RefreshTokenAsync(refreshTokenDTO);

            // Assert
            Assert.Multiple(() =>
            {
                // Check authentication result
                Assert.That(result.Success, Is.True, "Token refresh should succeed");
                Assert.That(result.Token, Is.EqualTo("new-access-token"), "New access token should be returned");
             
                // Verify token service was called
                tokenService.Verify(ts => ts.RefreshTokenAsync("valid-expired-token"), Times.Once);

                // Check that the initial token was invalidated
                var oldToken = context.RefreshTokens?.Find(initialRefreshToken.Id);
                Assert.That(oldToken?.Invalidated, Is.True, "Old token should be invalidated");

                // Check that a new refresh token was added to the database
                var newToken = context.RefreshTokens?.FirstOrDefault(rt => rt.Token == mockNewRefreshToken.Token);
                Assert.That(newToken, Is.Not.Null, "New refresh token should be in the database");
                Assert.That(newToken?.UserId, Is.EqualTo(user.Id), "New token should be associated with the same user");
                Assert.That(newToken?.Invalidated, Is.False, "New token should not be invalidated");
            });
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
                new ServiceCollection().BuildServiceProvider(), // Replace null with a valid IServiceProvider
                NullLogger<UserManager<ApplicationUser>>.Instance
            );
        }
        [Test]
        public async Task SignInAsync_ValidCredentials_ReturnsSuccessResult()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Email = "test@example.com",
                UserName = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            await userManager.CreateAsync(user, "Password123!");

            var signInDto = new SignInDTO
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var mockRefreshToken = new RefreshToken
            {
                Token = "test-refresh-token",
                JwtId = "test-jwt-id",
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                Invalidated = false
            };

            // Mock the token service to simulate generating both tokens
            tokenService.Reset();
            tokenService.Setup(ts => ts.GenerateTokensAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync((ApplicationUser u) =>
                {
                    // Simulate creating a refresh token by adding it to the database
                    if (context.RefreshTokens != null)
                    {
                        context.RefreshTokens.Add(mockRefreshToken);
                        context.SaveChanges();
                    }

                    return new AuthenticationResult
                    {
                        Token = "test-access-token",
                        Success = true
                    };
                });

            // Act
            var result = await accountRepository.SignInAsync(signInDto);

            // Assert
            Assert.Multiple(() =>
            {
                // Check authentication result
                Assert.That(result.Success, Is.True, "Authentication should succeed");
                Assert.That(result.Token, Is.Not.Null.Or.Empty, "Access token should not be null or empty");

                // Verify token service was called with the right user
                tokenService.Verify(ts => ts.GenerateTokensAsync(
                    It.Is<ApplicationUser>(u => u.Id == user.Id)),
                    Times.Once);

                // Check that a refresh token was actually added to the database
                var storedToken = context.RefreshTokens?.FirstOrDefault(rt => rt.UserId == user.Id);
                Assert.That(storedToken, Is.Not.Null, "Refresh token should be saved in the database");
                Assert.That(storedToken?.Token, Is.EqualTo(mockRefreshToken.Token), "Stored token should match the generated token");
                Assert.That(storedToken?.UserId, Is.EqualTo(user.Id), "Token should be associated with the correct user");
                Assert.That(storedToken?.Invalidated, Is.False, "Token should not be invalidated");
            });
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
        private SignInManager<ApplicationUser> GetSignInManager(ApplicationDbContext context)
        {

            var userManager = GetUserManager(context);

            // Need to create mocks for the dependencies
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

            userPrincipalFactoryMock
                .Setup(upf => upf.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync((ApplicationUser user) => {
                    // Create claims for the user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.Id)
                    };

                    // Create the identity and principal
                    var identity = new ClaimsIdentity(claims, "Test");
                    var principal = new ClaimsPrincipal(identity);

                    return principal;
                });

            // Create the SignInManager with all required dependencies
            return new SignInManager<ApplicationUser>(
                userManager,
                contextAccessorMock.Object,
                userPrincipalFactoryMock.Object,
                new OptionsWrapper<IdentityOptions>(new IdentityOptions()),
                NullLogger<SignInManager<ApplicationUser>>.Instance,
                new Mock<IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<ApplicationUser>>().Object
            );
        }
    }
}
