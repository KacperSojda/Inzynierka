using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace INZYNIERKA.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly INotificationService<User> _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(UserManager<User> userManager, INotificationService<User> notificationService, ILogger<NotificationsController> logger)
        {
            this._userManager = userManager;
            this._notificationService = notificationService;
            this._logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Notifications(int page = 1)
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                int pageSize = 10;

                var (model, totalCount) = await _notificationService.Notifications(userId, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                _logger.LogInformation("User {UserId} accessed notifications page {Page}.", userId, page);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load notifications for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load notifications.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var userId = _userManager.GetUserId(User);

            if (notificationId <= 0)
            {
                _logger.LogWarning("DeleteNotification failed: Invalid NotificationId {NotificationId} for user {UserId}.", notificationId, userId);
                TempData["ErrorMessage"] = "Wrong notification Id";
                return RedirectToAction("Notifications");
            }

            try
            {
                var result = await _notificationService.DeleteNotification(userId, notificationId);

                if (!result)
                {
                    _logger.LogWarning("DeleteNotification failed: Could not delete notification {NotificationId} for user {UserId}.", notificationId, userId);
                    TempData["ErrorMessage"] = "Cannot delete the notification.";
                }
                else
                {
                    _logger.LogInformation("User {UserId} deleted notification {NotificationId} successfully.", userId, notificationId);
                    TempData["SuccessMessage"] = "Notification deleted.";
                }

                return RedirectToAction("Notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while deleting notification {NotificationId} for user {UserId}.", notificationId, userId);
                TempData["ErrorMessage"] = "Server error";
                return RedirectToAction("Notifications");
            }
        }
    }
}
