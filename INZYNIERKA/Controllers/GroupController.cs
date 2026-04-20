using INZYNIERKA.Domain.Models;
using INZYNIERKA.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using INZYNIERKA.Services;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IGroupService groupService;
        private readonly IGroupMemberService groupMemberService;

        public GroupController(
            UserManager<User> userManager, 
            IGroupService groupService,
            IGroupMemberService groupMemberService)
        {
            this.userManager = userManager;
            this.groupService = groupService;
            this.groupMemberService = groupMemberService;
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
        public async Task<IActionResult> EditGroup(Domain.Models.Group model)
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
                return RedirectToAction("EditGroup", new {model.GroupID});
            }
            catch (UnauthorizedAccessException) {return Forbid();}
        }

        // GroupMember Service //

        public async Task<IActionResult> ShowGroupMembers(int groupId)
        {
            var userId = userManager.GetUserId(User);
            var model = await groupMemberService.GetGroupMembersAsync(groupId, userId);

            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GiveAdmin(int groupId, string userId)
        {
            try
            {
                var success = await groupMemberService.GiveAdminAsync(groupId, userId, userManager.GetUserId(User));
                if (!success) return NotFound();
                return RedirectToAction("ShowGroupMembers", new {groupId});
            }
            catch (UnauthorizedAccessException) {return Forbid();}
        }

        [HttpPost]
        public async Task<IActionResult> DemoteAdmin(int groupId, string userId)
        {
            try
            {
                var success = await groupMemberService.DemoteAdminAsync(groupId, userId, userManager.GetUserId(User));
                if (!success) return NotFound();
                return RedirectToAction("ShowGroupMembers", new {groupId});
            }
            catch (UnauthorizedAccessException) {return Forbid();}    
        }

        [HttpPost]
        public async Task<IActionResult> KickUser(int groupId, string userId)
        {
            try
            {
                var success = await groupMemberService.KickUserAsync(groupId, userId, userManager.GetUserId(User));
                if (!success) return NotFound();
                return RedirectToAction("ShowGroupMembers", new {groupId});
            }
            catch (UnauthorizedAccessException) {return Forbid();}
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(int groupId, string userId)
        {
            try
            {
                var success = await groupMemberService.BanUserAsync(groupId, userId, userManager.GetUserId(User));
                if (!success) return NotFound();
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (UnauthorizedAccessException) {return Forbid();}
        }
    }
}
