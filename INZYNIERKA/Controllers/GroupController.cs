using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
            try
            {
                var userId = userManager.GetUserId(User);
                return View(await groupService.GetAvailableGroupsAsync(userId));
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> ShowUserGroups()
        {
            try
            {
                var userId = userManager.GetUserId(User);
                return View(await groupService.GetUserGroupsAsync(userId));
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult CreateGroup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                ModelState.AddModelError("", "Group name cannot be empty.");
                return View();
            }

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.CreateGroupAsync(name, userId);
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the group.");
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            if (groupId <= 0) return RedirectToAction("ShowAvailableGroups");

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.JoinGroupAsync(groupId, userId);
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowAvailableGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            if (groupId <= 0) return RedirectToAction("ShowUserGroups");

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.LeaveGroupAsync(groupId, userId);
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowUserGroups");
            }
        }

        public async Task<IActionResult> EditGroup(int GroupID)
        {
            if (GroupID <= 0) return RedirectToAction("ShowUserGroups");

            try
            {
                var group = await groupService.GetGroupForEditAsync(GroupID, userManager.GetUserId(User));
                if (group == null) return NotFound("Cannot find the group.");
                return View(group);
            }
            catch (UnauthorizedAccessException) 
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup(Domain.Models.Group model)
        {
            if (model == null || model.Id <= 0) return RedirectToAction("ShowUserGroups");
            if (!ModelState.IsValid) return View(model);

            try
            {
                await groupService.UpdateGroupAsync(model, userManager.GetUserId(User));
                return RedirectToAction("ShowUserGroups");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            if (groupId <= 0) return RedirectToAction("ShowUserGroups");

            try
            {
                await groupService.DeleteGroupAsync(groupId, userManager.GetUserId(User));
                return RedirectToAction("ShowUserGroups");
            }
            catch (UnauthorizedAccessException) 
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowUserGroups");
            }
        }

        public async Task<IActionResult> SelectGroupTags(int groupId)
        {
            if (groupId <= 0) return RedirectToAction("ShowUserGroups");

            try
            {
                var model = await groupService.GetGroupTagsForSelectionAsync(groupId, userManager.GetUserId(User));
                return View(model);
            }
            catch (UnauthorizedAccessException) 
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelectGroupTags(SelectGroupTagsViewModel model)
        {
            if (model == null || model.GroupID <= 0) return RedirectToAction("ShowUserGroups");
            if (!ModelState.IsValid) return View(model);

            try
            {
                var selectedTagIds = model.Tags.Where(t => t.IsSelected).Select(t => t.TagId).ToList();
                await groupService.UpdateGroupTagsAsync(model.GroupID, userManager.GetUserId(User), selectedTagIds);
                return RedirectToAction("EditGroup", new {model.GroupID});
            }
            catch (UnauthorizedAccessException) 
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return View(model);
            }
        }

        // GroupMember Service //

        public async Task<IActionResult> ShowGroupMembers(int groupId)
        {
            if (groupId <= 0) return RedirectToAction("ShowUserGroups");

            try
            {
                var userId = userManager.GetUserId(User);
                var model = await groupMemberService.GetGroupMembersAsync(groupId, userId);

                if (model == null) return NotFound("Cannot find the group members.");
                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GiveAdmin(int groupId, string userId)
        {
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId)) return RedirectToAction("ShowUserGroups");

            try
            {
                var success = await groupMemberService.GiveAdminAsync(groupId, userId, userManager.GetUserId(User));
                if (!success) return NotFound("Cannot give admin role.");
                return RedirectToAction("ShowGroupMembers", new {groupId});
            }
            catch (UnauthorizedAccessException) 
            {
                return Forbid();
            }
            catch (Exception ex) {
                return RedirectToAction("ShowGroupMembers", new { groupId }); 
            }
        }

        [HttpPost]
        public async Task<IActionResult> DemoteAdmin(int groupId, string userId)
        {
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId)) return RedirectToAction("ShowUserGroups");

            try
            {
                var success = await groupMemberService.DemoteAdminAsync(groupId, userId, userManager.GetUserId(User));
                if (!success) return NotFound("Cannot demote admin role.");
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> KickUser(int groupId, string userId)
        {
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId)) return RedirectToAction("ShowUserGroups");

            try
            {
                var success = await groupMemberService.KickUserAsync(groupId, userId, userManager.GetUserId(User));
                if (!success) return NotFound("Cannot kick user.");
                return RedirectToAction("ShowGroupMembers", new {groupId});
            }
            catch (UnauthorizedAccessException) 
            {
                return Forbid();
            }
            catch (Exception ex) {
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(int groupId, string userId)
        {
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId)) return RedirectToAction("ShowUserGroups");

            try
            {
                var success = await groupMemberService.BanUserAsync(groupId, userId, userManager.GetUserId(User));
                if (!success) return NotFound("Cannot ban user.");
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }
    }
}
