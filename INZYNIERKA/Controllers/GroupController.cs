using System.Text.RegularExpressions;
using INZYNIERKA.Data;
using INZYNIERKA.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using INZYNIERKA.Services;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly INZDbContext context;
        private readonly UserManager<User> userManager;
        private readonly IGroupService groupService;
        public GroupController(
            UserManager<User> userManager, 
            INZDbContext dbcontext,
            IGroupService groupService)
        {
            this.userManager = userManager;
            this.context = dbcontext;
            this.groupService = groupService;
        }

        // Group Service //

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ShowAvailableGroups()
        {
            var userId = userManager.GetUserId(User);
            return View(await groupService.GetAvailableGroupsAsync(userId));
        }

        public async Task<IActionResult> ShowUserGroups()
        {
            var userId = userManager.GetUserId(User);
            return View(await groupService.GetUserGroupsAsync(userId));
        }

        public IActionResult CreateGroup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(string name)
        {
            var userId = userManager.GetUserId(User);
            await groupService.CreateGroupAsync(name, userId);
            return RedirectToAction("ShowUserGroups");
        }

        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            var userId = userManager.GetUserId(User);
            await groupService.JoinGroupAsync(groupId, userId);
            return RedirectToAction("ShowUserGroups");
        }

        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var userId = userManager.GetUserId(User);
            await groupService.LeaveGroupAsync(groupId, userId);
            return RedirectToAction("ShowUserGroups");
        }

        public async Task<IActionResult> EditGroup(int GroupID)
        {
            try
            {
                var group = await groupService.GetGroupForEditAsync(GroupID, userManager.GetUserId(User));
                if (group == null) return NotFound();
                return View(group);
            }
            catch (UnauthorizedAccessException) {return Forbid();}
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup(Models.Group model)
        {
            try
            {
                await groupService.UpdateGroupAsync(model, userManager.GetUserId(User));
                return RedirectToAction("ShowUserGroups");
            }
            catch (UnauthorizedAccessException) {return Forbid();}
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            try
            {
                await groupService.DeleteGroupAsync(groupId, userManager.GetUserId(User));
                return RedirectToAction("ShowUserGroups");
            }
            catch (UnauthorizedAccessException) {return Forbid();}
        }

        public async Task<IActionResult> SelectGroupTags(int groupID)
        {
            try
            {
                var model = await groupService.GetGroupTagsForSelectionAsync(groupID, userManager.GetUserId(User));
                return View(model);
            }
            catch (UnauthorizedAccessException) {return Forbid();}
        }

        [HttpPost]
        public async Task<IActionResult> SelectGroupTags(SelectGroupTagsViewModel model)
        {
            try
            {
                var selectedTagIds = model.Tags.Where(t => t.IsSelected).Select(t => t.TagId).ToList();
                await groupService.UpdateGroupTagsAsync(model.GroupID, userManager.GetUserId(User), selectedTagIds);
                return RedirectToAction("EditGroup", new { model.GroupID });
            }
            catch (UnauthorizedAccessException) {return Forbid();}
        }

        // GroupMember Service //

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

            return RedirectToAction("ShowGroupMembers", new { groupId });
        }
    }
}
