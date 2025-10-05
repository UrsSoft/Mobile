using SantiyeTalepWebUI.Models;

namespace SantiyeTalepWebUI.Models.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
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

    public class CreateNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public int? UserId { get; set; }
        public int? RequestId { get; set; }
        public int? OfferId { get; set; }
        public int? SupplierId { get; set; }
    }
}