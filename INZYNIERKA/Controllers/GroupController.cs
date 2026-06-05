using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.Services;
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
        private readonly IGroupService<User> groupService;
        private readonly IGroupMemberService<User> groupMemberService;

        public GroupController(
            UserManager<User> userManager, 
            IGroupService<User> groupService,
            IGroupMemberService<User> groupMemberService)
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

        public async Task<IActionResult> ShowAvailableGroups(string? searchQuery, int page = 1)
        {
            var userId = userManager.GetUserId(User);
            try
            {
                int pageSize = 10;
                var (model, totalCount) = await groupService.AvailableGroups(userId, searchQuery, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to load available groups.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> ShowUserGroups(string? searchQuery, int page = 1)
        {
            var userId = userManager.GetUserId(User);
            try
            {
                int pageSize = 10;
                var (model, totalCount) = await groupService.UserGroups(userId, searchQuery, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to load your groups.";
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
                await groupService.CreateGroup(name, userId);

                TempData["SuccessMessage"] = "Group created successfully.";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to create the group.");
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            if (groupId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowAvailableGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.JoinGroup(groupId, userId);

                TempData["SuccessMessage"] = "Successfully joined the group";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to join the group.";
                return RedirectToAction("ShowAvailableGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            if (groupId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.LeaveGroup(groupId, userId);

                TempData["SuccessMessage"] = "You have left the group.";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to leave the group.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        public async Task<IActionResult> EditGroup(int groupID)
        {
            if (groupID <= 0)
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);

                var model = await groupService.EditGroup(groupID, userId);

                if (model == null)
                {
                    TempData["ErrorMessage"] = "Cannot find the group.";
                    return RedirectToAction("ShowUserGroups");
                }
                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to load group details.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup(EditGroupViewModel model)
        {
            if (model == null || model.Id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            if (!ModelState.IsValid) return View(model);

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.UpdateGroup(model, userId);
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to update group settings.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            if (groupId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.DeleteGroup(groupId, userId);

                TempData["SuccessMessage"] = "Group has been deleted.";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to delete the group.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        public async Task<IActionResult> SelectGroupTags(int groupId)
        {
            if (groupId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                var model = await groupService.GroupTags(groupId, userId);

                if (model == null)
                {
                    TempData["ErrorMessage"] = "Cannot find the group.";
                    return RedirectToAction("ShowUserGroups");
                }
                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to load group tags.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelectGroupTags(SelectGroupTagsViewModel model)
        {
            if (model == null || model.GroupID <= 0)
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);

                var selectedTagsIds = model.Tags
                                        .Where(t => t.Selected)
                                        .Select(t => t.TagId)
                                        .ToList();

                await groupService.UpdateGroupTags(model.GroupID, userId, selectedTagsIds);

                TempData["SuccessMessage"] = "Group tags updated successfully.";
                return RedirectToAction("EditGroup", new {model.GroupID});
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Failed to update tags.");
                return View(model);
            }
        }

        // GroupMember Service //

        public async Task<IActionResult> ShowGroupMembers(int groupId)
        {
            if (groupId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                var model = await groupMemberService.GroupMembers(groupId, userId);

                if (model == null)
                {
                    TempData["ErrorMessage"] = "Cannot find the group members.";
                    return RedirectToAction("ShowUserGroups");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load members list.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GiveAdmin(int groupId, string userId)
        {
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var currentUserId = userManager.GetUserId(User);
                var success = await groupMemberService.GiveAdmin(groupId, userId, currentUserId);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Cannot assign admin role.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                TempData["SuccessMessage"] = "User promoted to administrator.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Server error while changing roles.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DemoteAdmin(int groupId, string userId)
        {
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var success = await groupMemberService.DemoteAdmin(groupId, userId, userManager.GetUserId(User));
                if (!success)
                {
                    TempData["ErrorMessage"] = "Cannot demote this administrator.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                TempData["SuccessMessage"] = "Administrator demoted to member.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Server error while changing roles.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> KickUser(int groupId, string userId)
        {
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var success = await groupMemberService.KickUser(groupId, userId, userManager.GetUserId(User));
                if (!success)
                {
                    TempData["ErrorMessage"] = "Cannot kick this user.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                TempData["SuccessMessage"] = "User has been removed from the group.";
                return RedirectToAction("ShowGroupMembers", new {groupId});
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Server error while kicking user.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(int groupId, string userId)
        {
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");  
            }

            try
            {
                var currentUserId = userManager.GetUserId(User);
                var success = await groupMemberService.BanUser(groupId, userId, currentUserId);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Cannot ban this user.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                TempData["SuccessMessage"] = "User has been banned from the group.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Server error while banning user.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowBannedMembers(int groupId)
        {
            try
            {
                var viewModel = await groupService.GetBannedUsersViewModel(groupId);
                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to load banned members list.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnbanUser(int groupId, string userId)
        {
            try
            {
                await groupService.UnbanUser(groupId, userId);
                TempData["SuccessMessage"] = "User has been successfully unbanned and restored as a member.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to unban the user.";
            }

            return RedirectToAction("ShowBannedMembers", new { groupId });
        }
    }
}
