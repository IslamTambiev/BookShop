using BookShopUI.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Update;

namespace BookShopUI.Data
{
    public class DbSeeder
    {
        public static async Task SeedDefaultData(IServiceProvider service)
        {
            var userMgr = service.GetService<UserManager<IdentityUser>>();
            var roleMgr = service.GetService<RoleManager<IdentityRole>>();
            // adding some roles to db
            await roleMgr.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
            await roleMgr.CreateAsync(new IdentityRole(Roles.User.ToString()));

            // create admin user
            var admin = new IdentityUser
            {
                UserName = "admin@gmail.com",
                Email = "admin@gmail.com",
                EmailConfirmed = true
            };

            var userInDb = await userMgr.FindByNameAsync(admin.Email);
            if (userInDb == null)
            {
                await userMgr.CreateAsync(admin, "admin@123");
                await userMgr.AddToRoleAsync(admin, Roles.Admin.ToString());
            }
        }
    }
}
