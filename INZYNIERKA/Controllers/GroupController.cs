using System.Text.RegularExpressions;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.Services;
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
        private readonly GeminiService geminiService;
        public GroupController(UserManager<User> userManager, INZDbContext dbcontext, GeminiService geminiService)
        {
            this.userManager = userManager;
            this.context = dbcontext;
            this.geminiService = geminiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ShowAvailableGroups()
        {
            var user = await userManager.GetUserAsync(User);

            var userGroupIds = await context.UserGroups
                .Where(ug => ug.UserId == user.Id)
                .Select(ug => ug.ChatGroupId)
                .ToListAsync();

            var model = await context.Groups
                .Include(g => g.GroupTags)
                    .ThenInclude(gt => gt.Tag)
                .Where(g => !userGroupIds.Contains(g.Id))
                .Select(g => new GroupItem
                {
                    GroupId = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    Tags = g.GroupTags
                        .Select(gt => gt.Tag.Name)
                        .ToList()
                })
                .ToListAsync();

            return View(new GroupViewModel { Groups = model });
        }


        public async Task<IActionResult> ShowUserGroups()
        {
            var user = await userManager.GetUserAsync(User);

            var userGroups = await context.UserGroups
                .Include(ug => ug.ChatGroup)
                    .ThenInclude(g => g.GroupTags)
                        .ThenInclude(gt => gt.Tag)
                .Where(ug => ug.UserId == user.Id)
                .ToListAsync();

            var adminGroups = userGroups
                .Where(ug => ug.Type == MemberType.Administrator)
                .Select(ug => new GroupItem
                {
                    GroupId = ug.ChatGroup.Id,
                    Name = ug.ChatGroup.Name,
                    Description = ug.ChatGroup.Description,
                    Tags = ug.ChatGroup.GroupTags
                        .Select(gt => gt.Tag.Name)
                        .ToList()
                })
                .ToList();

            var memberGroups = userGroups
                .Where(ug => ug.Type == MemberType.Member)
                .Select(ug => new GroupItem
                {
                    GroupId = ug.ChatGroup.Id,
                    Name = ug.ChatGroup.Name,
                    Tags = ug.ChatGroup.GroupTags
                        .Select(gt => gt.Tag.Name)
                        .ToList()
                })
                .ToList();

            return View(new GroupViewModel
            {
                AdminGroups = adminGroups,
                Groups = memberGroups
            });
        }

        public IActionResult CreateGroup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(string name)
        {
            var user = await userManager.GetUserAsync(User);

            var group = new Models.Group
            {
                Name = name,
                Description = "",
                Members = new List<UserGroup>
                {
                    new UserGroup
                    {
                        UserId = user.Id,
                        Type = MemberType.Administrator
                    }
                }
            };

            context.Groups.Add(group);
            await context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            var user = await userManager.GetUserAsync(User);

            var alreadyMember = await context.UserGroups
                .AnyAsync(ug => ug.UserId == user.Id && ug.ChatGroupId == groupId);

            if (!alreadyMember)
            {
                var userGroup = new UserGroup
                {
                    UserId = user.Id,
                    ChatGroupId = groupId,
                    Type = MemberType.Member
                };

                context.UserGroups.Add(userGroup);
                await context.SaveChangesAsync();
            }

            return RedirectToAction("ShowUserGroups");
        }

        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var user = await userManager.GetUserAsync(User);

            var membership = await context.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == user.Id && ug.ChatGroupId == groupId);

            if (membership != null)
            {
                context.UserGroups.Remove(membership);
                await context.SaveChangesAsync();
            }

            return RedirectToAction("ShowUserGroups");
        }

        public async Task<IActionResult> ShowGroupMembers(int groupId)
        {
            var user = await userManager.GetUserAsync(User);

            var group = await context.Groups
                .Include(g => g.Members)
                .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound();

            var admins = group.Members
                .Where(m => m.Type == MemberType.Administrator)
                .Select(m => new GroupMember
                {
                    UserId = m.User.Id,
                    Name = m.User.UserName
                }).ToList();

            var members = group.Members
                .Where(m => m.Type == MemberType.Member)
                .Select(m => new GroupMember
                {
                    UserId = m.User.Id,
                    Name = m.User.UserName
                }).ToList();

            var model = new GroupMembersViewModel
            {
                GroupId = group.Id,
                Name = group.Name,
                CurrentUserId = user.Id,
                Admins = admins,
                Members = members
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GiveAdmin(int groupId, string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            var isAdmin = await context.UserGroups
                .AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUserId && ug.Type == MemberType.Administrator);

            if (!isAdmin)
                return Forbid();

            var userGroup = await context.UserGroups
                .FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == userId);

            if (userGroup == null)
                return NotFound();

            userGroup.Type = MemberType.Administrator;
            await context.SaveChangesAsync();

            return RedirectToAction("ShowGroupMembers", new {groupId});
        }

        [HttpPost]
        public async Task<IActionResult> DemoteAdmin(int groupId, string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            var isAdmin = await context.UserGroups
                .AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUserId && ug.Type == MemberType.Administrator);

            if (!isAdmin)
                return Forbid();

            if (userId == currentUserId)
                return Forbid();

            var userGroup = await context.UserGroups
                .FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == userId);

            if (userGroup == null)
                return NotFound();

            userGroup.Type = MemberType.Member;
            await context.SaveChangesAsync();

            return RedirectToAction("ShowGroupMembers", new {groupId});
        }

        [HttpPost]
        public async Task<IActionResult> KickUser(int groupId, string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            var isAdmin = await context.UserGroups
                .AnyAsync(ug => ug.ChatGroupId == groupId && ug.UserId == currentUserId && ug.Type == MemberType.Administrator);

            if (!isAdmin)
                return Forbid();

            if (userId == currentUserId)
                return Forbid();

            var userGroup = await context.UserGroups
                .FirstOrDefaultAsync(ug => ug.ChatGroupId == groupId && ug.UserId == userId);

            if (userGroup == null)
                return NotFound();

            context.UserGroups.Remove(userGroup);
            await context.SaveChangesAsync();

            return RedirectToAction("ShowGroupMembers", new { groupId });
        }

        public async Task<IActionResult> EditGroup(int GroupID)
        {
            var currentUserId = userManager.GetUserId(User);

            var isAdmin = await context.UserGroups
                .AnyAsync(ug => ug.ChatGroupId == GroupID && ug.UserId == currentUserId && ug.Type == MemberType.Administrator);

            if (!isAdmin)
                return Forbid();

            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == GroupID);
            if (group == null)
                return NotFound();

            return View(group);
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup(Models.Group model)
        {
            var currentUserId = userManager.GetUserId(User);

            var isAdmin = await context.UserGroups
                .AnyAsync(ug => ug.ChatGroupId == model.Id && ug.UserId == currentUserId && ug.Type == MemberType.Administrator);

            if (!isAdmin)
                return Forbid();

            var group = await context.Groups.FindAsync(model.Id);
            if (group == null)
                return NotFound();

            group.Name = model.Name;
            group.Description = model.Description;

            await context.SaveChangesAsync();

            return RedirectToAction("ShowUserGroups");
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(int groupId, string userId)
        {
            var currentUser = await userManager.GetUserAsync(User);

            var isAdmin = await context.UserGroups.AnyAsync(ug =>
                ug.ChatGroupId == groupId &&
                ug.UserId == currentUser.Id &&
                ug.Type == MemberType.Administrator);

            if (!isAdmin)
                return Forbid();

            var userGroup = await context.UserGroups.FirstOrDefaultAsync(ug =>
                ug.ChatGroupId == groupId &&
                ug.UserId == userId);

            if (userGroup != null)
            {
                userGroup.Type = MemberType.Banned;
                await context.SaveChangesAsync();
            }

            return RedirectToAction("Members", new { id = groupId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var currentUser = await userManager.GetUserAsync(User);

            var isAdmin = await context.UserGroups.AnyAsync(ug =>
                ug.ChatGroupId == groupId &&
                ug.UserId == currentUser.Id &&
                ug.Type == MemberType.Administrator);

            if (!isAdmin)
                return Forbid();

            var group = await context.Groups
                .Include(g => g.Members)
                .Include(g => g.Messages)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound();

            context.UserGroups.RemoveRange(group.Members);

            if (group.Messages != null)
                context.GroupMessages.RemoveRange(group.Messages);

            context.Groups.Remove(group);
            await context.SaveChangesAsync();

            return RedirectToAction("ShowUserGroups");
        }

        public async Task<IActionResult> SelectGroupTags(int groupID)
        {
            var GroupTagIds = await context.GroupTags
                .Where(ut => ut.GroupId == groupID)
                .Select(ut => ut.TagId)
                .ToListAsync();

            var tags = await context.Tags.ToListAsync();

            var model = new SelectGroupTagsViewModel
            {
                GroupID = groupID,
                Tags = tags.Select(t => new TagItem
                {
                    TagId = t.Id,
                    TagName = t.Name,
                    IsSelected = GroupTagIds.Contains(t.Id)

                }).ToList()
            };
            Console.WriteLine("debug1");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SelectGroupTags(SelectGroupTagsViewModel model)
        {
            var currentUser = await userManager.GetUserAsync(User);

            var isAdmin = await context.UserGroups.AnyAsync(ug =>
                ug.ChatGroupId == model.GroupID &&
                ug.UserId == currentUser.Id &&
                ug.Type == MemberType.Administrator);


            if (!isAdmin)
                return Forbid();

            var selectedTagIds = model.Tags
                .Where(t => t.IsSelected)
                .Select(t => t.TagId)
                .ToList();

            var existingGroupTags = await context.GroupTags
                .Where(ut => ut.GroupId == model.GroupID)
                .ToListAsync();

            context.GroupTags.RemoveRange(existingGroupTags);

            foreach (var tagId in selectedTagIds)
            {
                context.GroupTags.Add(new GroupTag
                {
                    GroupId = model.GroupID,
                    TagId = tagId
                });
            }

            await context.SaveChangesAsync();
            return RedirectToAction("EditGroup", new {model.GroupID});
        }
    }
}
