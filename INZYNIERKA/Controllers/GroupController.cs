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
        private readonly UserManager<User> _userManager;
        private readonly IGroupService<User> _groupService;
        private readonly IGroupMemberService<User> _groupMemberService;
        private readonly ILogger<GroupController> _logger;

        public GroupController(
            UserManager<User> userManager, 
            IGroupService<User> groupService,
            IGroupMemberService<User> groupMemberService,
            ILogger<GroupController> logger)
        {
            this._userManager = userManager;
            this._groupService = groupService;
            this._groupMemberService = groupMemberService;
            this._logger = logger;
        }

        // Group Service //

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ShowAvailableGroups(string? searchQuery, int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            try
            {
                int pageSize = 10;
                var (model, totalCount) = await _groupService.AvailableGroups(userId, searchQuery, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                _logger.LogInformation("User {UserId} requested available groups (Page: {Page}).", userId, page);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load available groups for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load available groups.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowUserGroups(string? searchQuery, int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            try
            {
                int pageSize = 10;
                var (model, totalCount) = await _groupService.UserGroups(userId, searchQuery, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                _logger.LogInformation("User {UserId} requested their own groups (Page: {Page}).", userId, page);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load groups for user {UserId}.", userId);
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
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("CreateGroup failed: Group name was empty (User: {UserId}).", userId);
                ModelState.AddModelError("", "Group name cannot be empty.");
                return View();
            }

            try
            {
                await _groupService.CreateGroup(name, userId);
                _logger.LogInformation("User {UserId} successfully created group '{GroupName}'.", userId, name);

                TempData["SuccessMessage"] = "Group created successfully.";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create group '{GroupName}' for user {UserId}.", name, userId);
                ModelState.AddModelError("", "Failed to create the group.");
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            if (groupId <= 0)
            {
                _logger.LogWarning("JoinGroup failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowAvailableGroups");
            }

            try
            {
                await _groupService.JoinGroup(groupId, userId);
                _logger.LogInformation("User {UserId} successfully joined group {GroupId}.", userId, groupId);

                TempData["SuccessMessage"] = "Successfully joined the group";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {UserId} failed to join group {GroupId}.", userId, groupId);
                TempData["ErrorMessage"] = "Failed to join the group.";
                return RedirectToAction("ShowAvailableGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            if (groupId <= 0)
            {
                _logger.LogWarning("LeaveGroup failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                await _groupService.LeaveGroup(groupId, userId);
                _logger.LogInformation("User {UserId} successfully left group {GroupId}.", userId, groupId);

                TempData["SuccessMessage"] = "You have left the group.";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {UserId} failed to leave group {GroupId}.", userId, groupId);
                TempData["ErrorMessage"] = "Failed to leave the group.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditGroup(int groupID)
        {
            var userId = _userManager.GetUserId(User);

            if (groupID <= 0)
            {
                _logger.LogWarning("EditGroup (GET) failed: Invalid GroupId {GroupId} (User: {UserId}).", groupID, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var model = await _groupService.EditGroup(groupID, userId);

                if (model == null)
                {
                    _logger.LogWarning("EditGroup (GET) failed: Group {GroupId} not found or access denied for user {UserId}.", groupID, userId);
                    TempData["ErrorMessage"] = "Cannot find the group.";
                    return RedirectToAction("ShowUserGroups");
                }

                _logger.LogInformation("User {UserId} accessed edit page for group {GroupId}.", userId, groupID);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load group details for group {GroupId} requested by user {UserId}.", groupID, userId);
                TempData["ErrorMessage"] = "Failed to load group details.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup(EditGroupViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (model == null || model.Id <= 0)
            {
                _logger.LogWarning("EditGroup (POST) failed: Invalid model or GroupId (User: {UserId}).", userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            if (!ModelState.IsValid) return View(model);

            try
            {
                await _groupService.UpdateGroup(model, userId);
                _logger.LogInformation("User {UserId} successfully updated settings for group {GroupId}.", userId, model.Id);
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update settings for group {GroupId} by user {UserId}.", model.Id, userId);
                ModelState.AddModelError("", "Failed to update group settings.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            if (groupId <= 0)
            {
                _logger.LogWarning("DeleteGroup failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                await _groupService.DeleteGroup(groupId, userId);
                _logger.LogInformation("User {UserId} successfully deleted group {GroupId}.", userId, groupId);

                TempData["SuccessMessage"] = "Group has been deleted.";
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete group {GroupId} by user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to delete the group.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SelectGroupTags(int groupId)
        {
            var userId = _userManager.GetUserId(User);

            if (groupId <= 0)
            {
                _logger.LogWarning("SelectGroupTags (GET) failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var model = await _groupService.GroupTags(groupId, userId);

                if (model == null)
                {
                    _logger.LogWarning("SelectGroupTags (GET) failed: Group {GroupId} not found or access denied for user {UserId}.", groupId, userId);
                    TempData["ErrorMessage"] = "Cannot find the group.";
                    return RedirectToAction("ShowUserGroups");
                }

                _logger.LogInformation("User {UserId} accessed tag selection for group {GroupId}.", userId, groupId);
                return View(model);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Failed to load group tags for group {GroupId} requested by user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to load group tags.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelectGroupTags(SelectGroupTagsViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (model == null || model.GroupID <= 0)
            {
                _logger.LogWarning("SelectGroupTags (POST) failed: Invalid model or GroupId (User: {UserId}).", userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var selectedTagsIds = model.Tags
                                        .Where(t => t.Selected)
                                        .Select(t => t.TagId)
                                        .ToList();

                await _groupService.UpdateGroupTags(model.GroupID, userId, selectedTagsIds);
                _logger.LogInformation("User {UserId} successfully updated tags for group {GroupId}.", userId, model.GroupID);

                TempData["SuccessMessage"] = "Group tags updated successfully.";
                return RedirectToAction("EditGroup", new {model.GroupID});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update tags for group {GroupId} by user {UserId}.", model.GroupID, userId);
                ModelState.AddModelError("", "Failed to update tags.");
                return View(model);
            }
        }

        // GroupMember Service //

        [HttpGet]
        public async Task<IActionResult> ShowGroupMembers(int groupId)
        {
            var userId = _userManager.GetUserId(User);

            if (groupId <= 0)
            {
                _logger.LogWarning("ShowGroupMembers failed: Invalid GroupId {GroupId} (User: {UserId}).", groupId, userId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var model = await _groupMemberService.GroupMembers(groupId, userId);

                if (model == null)
                {
                    _logger.LogWarning("ShowGroupMembers failed: Cannot find members for group {GroupId} (User: {UserId}).", groupId, userId);
                    TempData["ErrorMessage"] = "Cannot find the group members.";
                    return RedirectToAction("ShowUserGroups");
                }

                _logger.LogInformation("User {UserId} loaded member list for group {GroupId}.", userId, groupId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load member list for group {GroupId} requested by user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to load members list.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GiveAdmin(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("GiveAdmin failed: Invalid GroupId {GroupId} or TargetUserId {TargetUserId} (Action by: {CurrentUserId}).", groupId, userId, currentUserId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {   
                var result = await _groupMemberService.GiveAdmin(groupId, userId, currentUserId);

                if (!result)
                {
                    _logger.LogWarning("GiveAdmin failed: Cannot assign admin role to {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                    TempData["ErrorMessage"] = "Cannot assign admin role.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                _logger.LogInformation("User {CurrentUserId} successfully promoted user {TargetUserId} to admin in group {GroupId}.", currentUserId, userId, groupId);
                TempData["SuccessMessage"] = "User promoted to administrator.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while user {CurrentUserId} attempted to promote {TargetUserId} in group {GroupId}.", currentUserId, userId, groupId);
                TempData["ErrorMessage"] = "Server error while changing roles.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DemoteAdmin(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("DemoteAdmin failed: Invalid GroupId {GroupId} or TargetUserId {TargetUserId} (Action by: {CurrentUserId}).", groupId, userId, currentUserId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var result = await _groupMemberService.DemoteAdmin(groupId, userId, currentUserId);
                if (!result)
                {
                    _logger.LogWarning("DemoteAdmin failed: Cannot demote {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                    TempData["ErrorMessage"] = "Cannot demote this administrator.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                TempData["SuccessMessage"] = "Administrator demoted to member.";
                _logger.LogInformation("User {CurrentUserId} successfully demoted user {TargetUserId} to member in group {GroupId}.", currentUserId, userId, groupId);
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while user {CurrentUserId} attempted to demote {TargetUserId} in group {GroupId}.", currentUserId, userId, groupId);
                TempData["ErrorMessage"] = "Server error while changing roles.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> KickUser(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("KickUser failed: Invalid GroupId {GroupId} or TargetUserId {TargetUserId} (Action by: {CurrentUserId}).", groupId, userId, currentUserId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var result = await _groupMemberService.KickUser(groupId, userId, currentUserId);
                if (!result)
                {
                    _logger.LogWarning("KickUser failed: Cannot kick {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                    TempData["ErrorMessage"] = "Cannot kick this user.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                _logger.LogInformation("User {CurrentUserId} successfully kicked user {TargetUserId} from group {GroupId}.", currentUserId, userId, groupId);
                TempData["SuccessMessage"] = "User has been removed from the group.";
                return RedirectToAction("ShowGroupMembers", new {groupId});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while user {CurrentUserId} attempted to kick {TargetUserId} from group {GroupId}.", currentUserId, userId, groupId);
                TempData["ErrorMessage"] = "Server error while kicking user.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("BanUser failed: Invalid GroupId {GroupId} or TargetUserId {TargetUserId} (Action by: {CurrentUserId}).", groupId, userId, currentUserId);
                TempData["ErrorMessage"] = "Invalid group ID.";
                return RedirectToAction("ShowUserGroups");  
            }

            try
            {
                var result = await _groupMemberService.BanUser(groupId, userId, currentUserId);
                if (!result)
                {
                    _logger.LogWarning("BanUser failed: Cannot ban {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                    TempData["ErrorMessage"] = "Cannot ban this user.";
                    return RedirectToAction("ShowGroupMembers", new { groupId });
                }

                _logger.LogInformation("User {CurrentUserId} successfully banned user {TargetUserId} from group {GroupId}.", currentUserId, userId, groupId);
                TempData["SuccessMessage"] = "User has been banned from the group.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while user {CurrentUserId} attempted to ban {TargetUserId} from group {GroupId}.", currentUserId, userId, groupId);
                TempData["ErrorMessage"] = "Server error while banning user.";
                return RedirectToAction("ShowGroupMembers", new { groupId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowBannedMembers(int groupId)
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                var viewModel = await _groupMemberService.GetBannedUsers(groupId);
                _logger.LogInformation("User {UserId} requested banned members list for group {GroupId}.", userId, groupId);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load banned members list for group {GroupId} requested by user {UserId}.", groupId, userId);
                TempData["ErrorMessage"] = "Failed to load banned members list.";
                return RedirectToAction("ShowUserGroups");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnbanUser(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            try
            {
                await _groupMemberService.UnbanUser(groupId, userId);

                _logger.LogInformation("User {CurrentUserId} successfully unbanned user {TargetUserId} in group {GroupId}.", currentUserId, userId, groupId);
                TempData["SuccessMessage"] = "User has been successfully unbanned and restored as a member.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unban user {TargetUserId} in group {GroupId} (Action by: {CurrentUserId}).", userId, groupId, currentUserId);
                TempData["ErrorMessage"] = "Failed to unban the user.";
            }

            return RedirectToAction("ShowBannedMembers", new { groupId });
        }
    }
}
