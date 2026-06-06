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
        private readonly UserManager<User> userManager;
        private readonly INotificationService<User> notificationService;
        private readonly ILogger<NotificationsController> logger;

        public NotificationsController(UserManager<User> userManager, INotificationService<User> notificationService, ILogger<NotificationsController> logger)
        {
            this.userManager = userManager;
            this.notificationService = notificationService;
            this.logger = logger;
        }
        public async Task<IActionResult> Notifications(int page = 1)
        {
            var userId = userManager.GetUserId(User);

            try
            {
                int pageSize = 10;

                var (model, totalCount) = await notificationService.Notifications(userId, page, pageSize);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                logger.LogInformation("User {UserId} accessed notifications page {Page}.", userId, page);
                return View(model);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load notifications for user {UserId}.", userId);
                TempData["ErrorMessage"] = "Failed to load notifications.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var userId = userManager.GetUserId(User);

            if (notificationId <= 0)
            {
                logger.LogWarning("DeleteNotification failed: Invalid NotificationId {NotificationId} for user {UserId}.", notificationId, userId);
                TempData["ErrorMessage"] = "Wrong notification Id";
                return RedirectToAction("Notifications");
            }

            try
            {
                var success = await notificationService.DeleteNotification(userId, notificationId);

                if (!success)
                {
                    logger.LogWarning("DeleteNotification failed: Could not delete notification {NotificationId} for user {UserId}.", notificationId, userId);
                    TempData["ErrorMessage"] = "Cannot delete the notification.";
                }
                else
                {
                    logger.LogInformation("User {UserId} deleted notification {NotificationId} successfully.", userId, notificationId);
                    TempData["SuccessMessage"] = "Notification deleted.";
                }

                return RedirectToAction("Notifications");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error while deleting notification {NotificationId} for user {UserId}.", notificationId, userId);
                TempData["ErrorMessage"] = "Server error";
                return RedirectToAction("Notifications");
            }
        }
    }
}
