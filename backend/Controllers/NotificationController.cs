using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Services;
using System.Security.Claims;

namespace SantiyeTalepApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int daysBack = 7)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
                {
                    userId = parsedUserId;
                }

                var notifications = await _notificationService.GetNotificationsAsync(userId, unreadOnly, daysBack);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bildirimler alınırken bir hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetNotificationSummary()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                // Debug logging
                Console.WriteLine($"NotificationController.GetNotificationSummary - UserIdClaim: {userIdClaim}, UserRole: {userRole}");
                
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
                {
                    userId = parsedUserId;
                    Console.WriteLine($"NotificationController.GetNotificationSummary - Parsed UserId: {userId}");
                }
                else
                {
                    Console.WriteLine($"NotificationController.GetNotificationSummary - Failed to parse UserId from claim: {userIdClaim}");
                    // If we can't parse userId, return empty summary instead of potentially showing admin notifications
                    return Ok(new NotificationSummaryDto
                    {
                        TotalCount = 0,
                        UnreadCount = 0,
                        RecentNotifications = new List<NotificationDto>()
                    });
                }

                var summary = await _notificationService.GetNotificationSummaryAsync(userId);
                Console.WriteLine($"NotificationController.GetNotificationSummary - Summary UnreadCount: {summary.UnreadCount}");
                return Ok(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NotificationController.GetNotificationSummary - Exception: {ex.Message}");
                return StatusCode(500, new { message = "Bildirim özeti alınırken bir hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id);
                return Ok(new { message = "Bildirim okundu olarak işaretlendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bildirim güncellenirken bir hata oluştu", error = ex.Message });
            }
        }

        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
                {
                    userId = parsedUserId;
                }

                await _notificationService.MarkAllAsReadAsync(userId);
                return Ok(new { message = "Tüm bildirimler okundu olarak işaretlendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bildirimler güncellenirken bir hata oluştu", error = ex.Message });
            }
        }
    }
}