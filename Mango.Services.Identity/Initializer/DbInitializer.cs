using System.Security.Claims;
using IdentityModel;
using Mango.Services.Identity.DbContexts;
using Mango.Services.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace Mango.Services.Identity.Initializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public void Initialize()
        {
            //Create admin/customer Role if it does not exists
            if (_roleManager.FindByNameAsync(SD.Admin).Result == null)
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Customer)).GetAwaiter().GetResult();
            }
            else
            {
                return;
            }
            //create admin user
            ApplicationUser adminUser = new ApplicationUser()
            {
                UserName = "admin",
                FirstName = "Virendra",
                LastName = "Kumar",
                Email = "admin@gmail.com",
                PhoneNumber = "1234567890"
            };
            _userManager.CreateAsync(adminUser, "Admin123*").GetAwaiter().GetResult();
            //assign role to admin user
            _userManager.AddToRoleAsync(adminUser, SD.Admin).GetAwaiter().GetResult();
            //add claims to the user
            var adminClaims = _userManager.AddClaimsAsync(adminUser, new Claim[]
            {
                new Claim(JwtClaimTypes.Name,adminUser.FirstName + " " + adminUser.LastName),
                new Claim(JwtClaimTypes.GivenName,adminUser.FirstName),
                new Claim(JwtClaimTypes.FamilyName,adminUser.LastName),
                new Claim(JwtClaimTypes.Role,SD.Admin),
            }).Result;
            //Create Customer User
            ApplicationUser customerUser = new ApplicationUser()
            {
                UserName = "customer",
                FirstName = "Tara",
                LastName = "Sharma",
                Email = "customer@gmail.com",
                PhoneNumber = "1234567891"
            };
            _userManager.CreateAsync(customerUser, "Admin123*").GetAwaiter().GetResult();
            //assign role to admin user
            _userManager.AddToRoleAsync(customerUser, SD.Customer).GetAwaiter().GetResult();
            //add claims to the user
            var customerClaims = _userManager.AddClaimsAsync(customerUser, new Claim[]
            {
                new Claim(JwtClaimTypes.Name,customerUser.FirstName + " " + customerUser.LastName),
                new Claim(JwtClaimTypes.GivenName,customerUser.FirstName),
                new Claim(JwtClaimTypes.FamilyName,customerUser.LastName),
                new Claim(JwtClaimTypes.Role,SD.Customer),
            }).Result;


        }
    }
}
