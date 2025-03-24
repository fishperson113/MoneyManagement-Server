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
                {"AppSettings:BaseUrl", "http://localhost:5000"}
            };
            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            logger = NullLogger<AccountRepository>.Instance;

            accountRepository = new AccountRepository(userManager, null, configuration, roleManager, logger);
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

        private UserManager<ApplicationUser> GetUserManager(ApplicationDbContext context)
        {
            var store = new UserStore<ApplicationUser>(context);
            return new UserManager<ApplicationUser>(
                store,
                null,
                new PasswordHasher<ApplicationUser>(),
                new List<IUserValidator<ApplicationUser>>(),
                new List<IPasswordValidator<ApplicationUser>>(),
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
