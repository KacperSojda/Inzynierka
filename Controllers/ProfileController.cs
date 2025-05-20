using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Controllers
{
    public class ProfileController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        public ProfileController(UserManager<User> userManager, INZDbContext dbcontext)
        {
            this.userManager = userManager;
            context = dbcontext;
        }
        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);

            var userTags = await context.UserTags
            .Where(ut => ut.UserId == user.Id)
            .Include(ut => ut.Tag)
            .ToListAsync();

            var model = new UserViewModel
            {
                PrivateDescription = user.PrivateDescription,
                PublicDescription = user.PublicDescription,
                UserName = user.UserName,
                Avatar = user.Avatar,
                Tags = userTags.Select(ut => ut.Tag.Name).ToList()
            };
            return View(model);
        }

        public async Task<IActionResult> EditProfile()
        {
            var user = await userManager.GetUserAsync(User);

            var model = new UserViewModel
            {
                PrivateDescription = user.PrivateDescription,
                PublicDescription = user.PublicDescription,
                UserName = user.UserName,
                Avatar = user.Avatar,
                Tags = new List<String>()
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EditProfile(UserViewModel model)
        {
            model.Tags = new List<String>();
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(User);

                if (user == null) return NotFound();

                user.Avatar = model.Avatar;
                user.PublicDescription = model.PublicDescription;
                user.PrivateDescription = model.PrivateDescription;

                var result = await userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Profile");
                }
                else
                {
                    Console.WriteLine(user.UserName);
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> SelectTags()
        {
            var user = await userManager.GetUserAsync(User);

            var userTagIds = await context.UserTags
            .Where(ut => ut.UserId == user.Id)
            .Select(ut => ut.TagId)
            .ToListAsync();

            var tags = await context.Tags.ToListAsync();

            var model = new SelectTagsViewModel
            {
                Tags = tags.Select(t => new TagItem
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    IsSelected = userTagIds.Contains(t.Id)

                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SelectTags(SelectTagsViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            var selectedTagIds = model.Tags
            .Where(t => t.IsSelected)
            .Select(t => t.TagId)
            .ToList();

            var existingUserTags = await context.UserTags
                .Where(ut => ut.UserId == user.Id)
                .ToListAsync();

            context.UserTags.RemoveRange(existingUserTags);

            foreach (var tagId in selectedTagIds)
            {
                context.UserTags.Add(new UserTag
                {
                    UserId = user.Id,
                    TagId = tagId
                });
            }

            await context.SaveChangesAsync();
            return RedirectToAction("Index", "Profile");
        }


        /// Funkcje pomocnicze =====================================

        public IActionResult AddTag()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddTag(TagViewModel model)
        {
            Tag tag = new Tag()
            {
                Name = model.TagName
            };

            context.Tags.Add(tag);
            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Profile");
        }

        public async Task<IActionResult> ShowTags()
        {
            var tags = await context.Tags.ToListAsync();
            return View(tags);
        }
    }
}
