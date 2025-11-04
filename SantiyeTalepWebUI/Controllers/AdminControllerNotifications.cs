using Microsoft.AspNetCore.Mvc;

namespace SantiyeTalepWebUI.Controllers
{
    public partial class AdminController
    {
        //// Notification Management Actions
        //[HttpGet]
        //public async Task<IActionResult> GetNotifications()
        //{
        //    var token = _authService.GetStoredToken();
        //    if (string.IsNullOrEmpty(token))
        //        return Json(new { success = false, message = "Oturum süresi dolmuþ" });

        //    try
        //    {
        //        var notifications = await _apiService.GetAsync<List<NotificationDto>>("api/Notification", token) ?? new List<NotificationDto>();
        //        return Json(new { success = true, notifications = notifications });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting notifications");
        //        return Json(new { success = false, message = "Bildirimler yüklenirken hata oluþtu" });
        //    }
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetNotificationSummary()
        //{
        //    var token = _authService.GetStoredToken();
        //    if (string.IsNullOrEmpty(token))
        //        return Json(new { success = false, message = "Oturum süresi dolmuþ" });

        //    try
        //    {
        //        var summary = await _apiService.GetAsync<NotificationSummaryDto>("api/Notification/summary", token);
        //        return Json(new { success = true, summary = summary });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting notification summary");
        //        return Json(new { success = false, message = "Bildirim özeti yüklenirken hata oluþtu" });
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> MarkNotificationAsRead([FromBody] MarkAsReadRequest request)
        //{
        //    var token = _authService.GetStoredToken();
        //    if (string.IsNullOrEmpty(token))
        //        return Json(new { success = false, message = "Oturum süresi dolmuþ" });

        //    try
        //    {
        //        var result = await _apiService.PutAsync<object>($"api/Notification/{request.Id}/read", new { }, token);
        //        return Json(new { success = true, message = "Bildirim okundu olarak iþaretlendi" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error marking notification as read");
        //        return Json(new { success = false, message = "Bildirim güncellenirken hata oluþtu" });
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> MarkAllNotificationsAsRead()
        //{
        //    var token = _authService.GetStoredToken();
        //    if (string.IsNullOrEmpty(token))
        //        return Json(new { success = false, message = "Oturum süresi dolmuþ" });

        //    try
        //    {
        //        var result = await _apiService.PutAsync<object>("api/Notification/mark-all-read", new { }, token);
        //        return Json(new { success = true, message = "Tüm bildirimler okundu olarak iþaretlendi" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error marking all notifications as read");
        //        return Json(new { success = false, message = "Bildirimler güncellenirken hata oluþtu" });
        //    }
        //}
    }

    // NotificationDto ve NotificationSummaryDto class'larý
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public int? UserId { get; set; }
        public int? RequestId { get; set; }
        public int? OfferId { get; set; }
        public int? SupplierId { get; set; }
    }

    public class NotificationSummaryDto
    {
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; } = new List<NotificationDto>();
    }

    public class MarkAsReadRequest
    {
        public int Id { get; set; }
    }
}