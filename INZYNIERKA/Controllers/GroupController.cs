using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Controllers
{
    public class GroupController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        public GroupController(UserManager<User> userManager, INZDbContext dbcontext)
        {
            this.userManager = userManager;
            this.context = dbcontext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ShowUserGroups()
        {
            var user = await userManager.GetUserAsync(User);

            var model = await context.UserGroups
                .Where(ug => ug.UserId == user.Id)
                .Select(ug => new GroupItem
                {
                    GroupId = ug.ChatGroup.Id,
                    Name = ug.ChatGroup.Name
                }).ToListAsync();

            return View(new GroupViewModel { Groups = model });
        }
    }
}
