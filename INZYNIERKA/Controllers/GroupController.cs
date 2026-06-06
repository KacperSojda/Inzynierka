using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using INZYNIERKA.Services.Services;
using INZYNIERKA.Services.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IGroupService<User> groupService;
        private readonly IGroupMemberService<User> groupMemberService;
        private readonly ILogger<GroupController> logger;

        public GroupController(
            UserManager<User> userManager, 
            IGroupService<User> groupService,
            IGroupMemberService<User> groupMemberService,
            ILogger<GroupController> logger)
        {
            this.userManager = userManager;
            this.groupService = groupService;
            this.groupMemberService = groupMemberService;
            this.logger = logger;
        }

        // Group Service //

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
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

                logger.LogInformation("User {UserId} requested available groups (Page: {Page}).", userId, page);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load available groups for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load available groups.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
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

                logger.LogInformation("User {UserId} requested their own groups (Page: {Page}).", userId, page);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load groups for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load your groups.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult CreateGroup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(string name)
        {
            var userId = userManager.GetUserId(User);
            if (string.IsNullOrEmpty(name))
            {
                logger.LogWarning("CreateGroup failed: Group name was empty (User: {UserId}).", userId);
                ModelState.AddModelError("", "Group name cannot be empty.");
                return View();
            }

            try
            {
                await groupService.CreateGroup(name, userId);
                logger.LogInformation("User {UserId} successfully created group '{GroupName}'.", userId, name);

                TempData["SuccessMessage"] = "Group created successfully.";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create group '{GroupName}' for user {UserId}.", name, userId);
                ModelState.AddModelError("", "Failed to create the group.");
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            var userId = userManager.GetUserId(User);
            if (groupId <= 0)
            {
                logger.LogWarning("JoinGroup failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowAvailableGroups");
            }

            try
            {
                await groupService.JoinGroup(groupId, userId);
                logger.LogInformation("User {UserId} successfully joined group {GroupId}.", userId, groupId);

                TempData["SuccessMessage"] = "Successfully joined the group";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "User {UserId} failed to join group {GroupId}.", userId, groupId);
                TempData["ErrorMessage"] = "Failed to join the group.";
                return RedirectToAction("ShowAvailableGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var userId = userManager.GetUserId(User);
            if (groupId <= 0)
            {
                logger.LogWarning("LeaveGroup failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                await groupService.LeaveGroup(groupId, userId);
                logger.LogInformation("User {UserId} successfully left group {GroupId}.", userId, groupId);

                TempData["SuccessMessage"] = "You have left the group.";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "User {UserId} failed to leave group {GroupId}.", userId, groupId);
                TempData["ErrorMessage"] = "Failed to leave the group.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditGroup(int groupID)
        {
            var userId = userManager.GetUserId(User);

            if (groupID <= 0)
            {
                logger.LogWarning("EditGroup (GET) failed: Invalid GroupId {GroupId} (User: {UserId}).", groupID, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var model = await groupService.EditGroup(groupID, userId);

                if (model == null)
                {
                    logger.LogWarning("EditGroup (GET) failed: Group {GroupId} not found or access denied for user {UserId}.", groupID, userId);
                    TempData["ErrorMessage"] = "Cannot find the group.";
                    return RedirectToAction("ShowUserGroups");
                }

                logger.LogInformation("User {UserId} accessed edit page for group {GroupId}.", userId, groupID);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load group details for group {GroupId} requested by user {UserId}.", groupID, userId);
                TempData["ErrorMessage"] = "Failed to load group details.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup(EditGroupViewModel model)
        {
            var userId = userManager.GetUserId(User);

            if (model == null || model.Id <= 0)
            {
                logger.LogWarning("EditGroup (POST) failed: Invalid model or GroupId (User: {UserId}).", userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            if (!ModelState.IsValid) return View(model);

            try
            {
                await groupService.UpdateGroup(model, userId);
                logger.LogInformation("User {UserId} successfully updated settings for group {GroupId}.", userId, model.Id);
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update settings for group {GroupId} by user {UserId}.", model.Id, userId);
                ModelState.AddModelError("", "Failed to update group settings.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var userId = userManager.GetUserId(User);
            if (groupId <= 0)
            {
                logger.LogWarning("DeleteGroup failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                await groupService.DeleteGroup(groupId, userId);
                logger.LogInformation("User {UserId} successfully deleted group {GroupId}.", userId, groupId);

                TempData["SuccessMessage"] = "Group has been deleted.";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete group {GroupId} by user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to delete the group.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SelectGroupTags(int groupId)
        {
            var userId = userManager.GetUserId(User);

            if (groupId <= 0)
            {
                logger.LogWarning("SelectGroupTags (GET) failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var model = await groupService.GroupTags(groupId, userId);

                if (model == null)
                {
                    logger.LogWarning("SelectGroupTags (GET) failed: Group {GroupId} not found or access denied for user {UserId}.", groupId, userId);
                    TempData["ErrorMessage"] = "Cannot find the group.";
                    return RedirectToAction("ShowUserGroups");
                }

                logger.LogInformation("User {UserId} accessed tag selection for group {GroupId}.", userId, groupId);
                return View(model);
            }
            catch (Exception ex) 
            {
                logger.LogError(ex, "Failed to load group tags for group {GroupId} requested by user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to load group tags.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelectGroupTags(SelectGroupTagsViewModel model)
        {
            var userId = userManager.GetUserId(User);

            if (model == null || model.GroupID <= 0)
            {
                logger.LogWarning("SelectGroupTags (POST) failed: Invalid model or GroupId (User: {UserId}).", userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var selectedTagsIds = model.Tags
                                        .Where(t => t.Selected)
                                        .Select(t => t.TagId)
                                        .ToList();

                await groupService.UpdateGroupTags(model.GroupID, userId, selectedTagsIds);
                logger.LogInformation("User {UserId} successfully updated tags for group {GroupId}.", userId, model.GroupID);

                TempData["SuccessMessage"] = "Group tags updated successfully.";
                return RedirectToAction("EditGroup", new {model.GroupID});
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update tags for group {GroupId} by user {UserId}.", model.GroupID, userId);
                ModelState.AddModelError("", "Failed to update tags.");
                return View(model);
            }
        }

        // GroupMember Service //

        [HttpGet]
        public async Task<IActionResult> ShowGroupMembers(int groupId)
        {
            var userId = userManager.GetUserId(User);

            if (groupId <= 0)
            {
                logger.LogWarning("ShowGroupMembers failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var model = await groupMemberService.GroupMembers(groupId, userId);

                if (model == null)
                {
                    logger.LogWarning("ShowGroupMembers failed: Cannot find members for group {GroupId} (User: {UserId}).", groupId, userId);
                    TempData["ErrorMessage"] = "Cannot find the group members.";
                    return RedirectToAction("ShowUserGroups");
                }

                logger.LogInformation("User {UserId} loaded member list for group {GroupId}.", userId, groupId);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load member list for group {GroupId} requested by user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to load members list.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GiveAdmin(int groupId, string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("GiveAdmin failed: Invalid GroupId {GroupId} or TargetUserId {TargetUserId} (Action by: {CurrentUserId}).", groupId, userId, currentUserId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {   
                var success = await groupMemberService.GiveAdmin(groupId, userId, currentUserId);

                if (!success)
                {
                    logger.LogWarning("GiveAdmin failed: Cannot assign admin role to {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                    TempData["ErrorMessage"] = "Cannot assign admin role.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                logger.LogInformation("User {CurrentUserId} successfully promoted user {TargetUserId} to admin in group {GroupId}.", currentUserId, userId, groupId);
                TempData["SuccessMessage"] = "User promoted to administrator.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while user {CurrentUserId} attempted to promote {TargetUserId} in group {GroupId}.", currentUserId, userId, groupId);
                TempData["ErrorMessage"] = "Server error while changing roles.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DemoteAdmin(int groupId, string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("DemoteAdmin failed: Invalid GroupId {GroupId} or TargetUserId {TargetUserId} (Action by: {CurrentUserId}).", groupId, userId, currentUserId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var success = await groupMemberService.DemoteAdmin(groupId, userId, currentUserId);
                if (!success)
                {
                    logger.LogWarning("DemoteAdmin failed: Cannot demote {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                    TempData["ErrorMessage"] = "Cannot demote this administrator.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                TempData["SuccessMessage"] = "Administrator demoted to member.";
                logger.LogInformation("User {CurrentUserId} successfully demoted user {TargetUserId} to member in group {GroupId}.", currentUserId, userId, groupId);
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while user {CurrentUserId} attempted to demote {TargetUserId} in group {GroupId}.", currentUserId, userId, groupId);
                TempData["ErrorMessage"] = "Server error while changing roles.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> KickUser(int groupId, string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("KickUser failed: Invalid GroupId {GroupId} or TargetUserId {TargetUserId} (Action by: {CurrentUserId}).", groupId, userId, currentUserId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var success = await groupMemberService.KickUser(groupId, userId, currentUserId);
                if (!success)
                {
                    logger.LogWarning("KickUser failed: Cannot kick {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                    TempData["ErrorMessage"] = "Cannot kick this user.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                logger.LogInformation("User {CurrentUserId} successfully kicked user {TargetUserId} from group {GroupId}.", currentUserId, userId, groupId);
                TempData["SuccessMessage"] = "User has been removed from the group.";
                return RedirectToAction("ShowGroupMembers", new {groupId});
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while user {CurrentUserId} attempted to kick {TargetUserId} from group {GroupId}.", currentUserId, userId, groupId);
                TempData["ErrorMessage"] = "Server error while kicking user.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(int groupId, string userId)
        {
            var currentUserId = userManager.GetUserId(User);

            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("BanUser failed: Invalid GroupId {GroupId} or TargetUserId {TargetUserId} (Action by: {CurrentUserId}).", groupId, userId, currentUserId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");  
            }

            try
            {
                var success = await groupMemberService.BanUser(groupId, userId, currentUserId);
                if (!success)
                {
                    logger.LogWarning("BanUser failed: Cannot ban {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                    TempData["ErrorMessage"] = "Cannot ban this user.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                logger.LogInformation("User {CurrentUserId} successfully banned user {TargetUserId} from group {GroupId}.", currentUserId, userId, groupId);
                TempData["SuccessMessage"] = "User has been banned from the group.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while user {CurrentUserId} attempted to ban {TargetUserId} from group {GroupId}.", currentUserId, userId, groupId);
                TempData["ErrorMessage"] = "Server error while banning user.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowBannedMembers(int groupId)
        {
            var userId = userManager.GetUserId(User);

            try
            {
                var viewModel = await groupMemberService.GetBannedUsersViewModel(groupId);
                logger.LogInformation("User {UserId} requested banned members list for group {GroupId}.", userId, groupId);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load banned members list for group {GroupId} requested by user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to load banned members list.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnbanUser(int groupId, string userId)
        {
            var currentUserId = userManager.GetUserId(User);
            try
            {
                await groupMemberService.UnbanUser(groupId, userId);

                logger.LogInformation("User {CurrentUserId} successfully unbanned user {TargetUserId} in group {GroupId}.", currentUserId, userId, groupId);
                TempData["SuccessMessage"] = "User has been successfully unbanned and restored as a member.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unban user {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                TempData["ErrorMessage"] = "Failed to unban the user.";
            }

            return RedirectToAction("ShowBannedMembers", new { groupId });
        }
    }
}
