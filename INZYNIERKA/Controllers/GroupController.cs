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
            catch (Exception ex)
            {
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
                return View();
            }

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.CreateGroup(name, userId);
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
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
                await groupService.JoinGroup(groupId, userId);
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
                await groupService.LeaveGroup(groupId, userId);
                return RedirectToAction("ShowUserGroups");
            }
            catch (Exception ex)
            {
                return RedirectToAction("ShowUserGroups");
            }
        }

        public async Task<IActionResult> EditGroup(int groupID)
        {
            if (groupID <= 0) return RedirectToAction("ShowUserGroups");

            try
            {
                var userId = userManager.GetUserId(User);

                var model = await groupService.EditGroup(groupID, userId);

                if (model == null)
                {
                    return NotFound("Cannot find the group.");
                }
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
        public async Task<IActionResult> EditGroup(EditGroupViewModel model)
        {
            if (model == null || model.Id <= 0)
            {
                return RedirectToAction("ShowUserGroups");
            }

            if (!ModelState.IsValid) return View(model);

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.UpdateGroup(model, userId);
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
            if (groupId <= 0)
            {
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                await groupService.DeleteGroup(groupId, userId);
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
            if (groupId <= 0)
            {
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                var model = await groupService.GroupTags(groupId, userId);
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
            if (model == null || model.GroupID <= 0)
            {
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
            if (groupId <= 0)
            {
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var userId = userManager.GetUserId(User);
                var model = await groupMemberService.GroupMembers(groupId, userId);

                if (model == null)
                {
                    return NotFound("Cannot find the group members.");
                }
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
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var currentUserId = userManager.GetUserId(User);
                var success = await groupMemberService.GiveAdmin(groupId, userId, currentUserId);
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
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var success = await groupMemberService.DemoteAdmin(groupId, userId, userManager.GetUserId(User));
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
                var success = await groupMemberService.KickUser(groupId, userId, userManager.GetUserId(User));
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
            if (groupId <= 0 || string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction("ShowUserGroups");
            }

            try
            {
                var currentUserId = userManager.GetUserId(User);
                var success = await groupMemberService.BanUser(groupId, userId, currentUserId);
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
