using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace INZYNIERKA.Controllers
{
    public class BrowserController : Controller
    {
        private readonly INZDbContext context;
        public BrowserController(INZDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> SearchUsersByTags()
        {
            var tags = await context.Tags.ToListAsync();

            var viewModel = new SearchByTagsViewModel
            {
                AvailableTags = tags.Select(t => new TagCheckboxItem
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    IsSelected = false
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SearchUsersByTags(SearchByTagsViewModel model)
        {
            var selectedTagIds = model.AvailableTags
                .Where(t => t.IsSelected)
                .Select(t => t.TagId)
                .ToList();

            var users = await context.Users
                .Include(u => u.UserTags)
                .ThenInclude(ut => ut.Tag)
                .Where(u => selectedTagIds.All(tagId =>
                    u.UserTags.Any(ut => ut.TagId == tagId)))
                .ToListAsync();

            model.MatchingUsers = users.Select(u => new UserViewModel
            {
                UserName = u.UserName,
                Avatar = u.Avatar,
                PublicDescription = u.PublicDescription,
                Tags = u.UserTags.Select(ut => ut.Tag.Name).ToList()
            }).ToList();

            TempData["SearchResults"] = JsonConvert.SerializeObject(model.MatchingUsers);

            return View("SearchResults", model);
        }

        [HttpPost]
        public IActionResult NextUser(int currentIndex)
        {
            var usersJson = TempData["SearchResults"]?.ToString();
            var users = JsonConvert.DeserializeObject<List<UserViewModel>>(usersJson);

            var model = new SearchByTagsViewModel
            {
                MatchingUsers = users,
                CurrentIndex = currentIndex + 1
            };

            TempData["SearchResults"] = JsonConvert.SerializeObject(users);
            return View("SearchResults", model);
        }
    }
}
