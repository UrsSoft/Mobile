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
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
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

                var notifications = await _notificationService.GetNotificationsAsync(userId, unreadOnly);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bildirimler alýnýrken bir hata oluþtu", error = ex.Message });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetNotificationSummary()
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

                var summary = await _notificationService.GetNotificationSummaryAsync(userId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bildirim özeti alýnýrken bir hata oluþtu", error = ex.Message });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id);
                return Ok(new { message = "Bildirim okundu olarak iþaretlendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bildirim güncellenirken bir hata oluþtu", error = ex.Message });
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
                return Ok(new { message = "Tüm bildirimler okundu olarak iþaretlendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bildirimler güncellenirken bir hata oluþtu", error = ex.Message });
            }
        }
    }
}