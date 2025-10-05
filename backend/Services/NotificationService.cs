using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;

namespace SantiyeTalepApi.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string title, string message, NotificationType type, int? userId = null, int? requestId = null, int? offerId = null, int? supplierId = null);
        Task<List<NotificationDto>> GetNotificationsAsync(int? userId = null, bool unreadOnly = false);
        Task<NotificationSummaryDto> GetNotificationSummaryAsync(int? userId = null);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int? userId = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(string title, string message, NotificationType type, int? userId = null, int? requestId = null, int? offerId = null, int? supplierId = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                UserId = userId,
                RequestId = requestId,
                OfferId = offerId,
                SupplierId = supplierId,
                CreatedDate = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(int? userId = null, bool unreadOnly = false)
        {
            var query = _context.Notifications.AsQueryable();

            if (userId.HasValue)
            {
                // Kullanýcýnýn rolünü kontrol et
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    if (user.Role == UserRole.Supplier)
                    {
                        // Tedarikçi için: Sadece kendi tedarikçi ID'si ile iliþkili bildirimler
                        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                        if (supplier != null)
                        {
                            // Tedarikçiler için sadece belirli bildirim tiplerini ve sadece okunmamýþlarý göster
                            var allowedNotificationTypes = new[] { 
                                NotificationType.OfferApproved, 
                                NotificationType.OfferRejected, 
                                NotificationType.SupplierApproved, 
                                NotificationType.SupplierRejected, 
                                NotificationType.RequestSentToSupplier 
                            };
                            
                            query = query.Where(n => (n.SupplierId == supplier.Id || 
                                                   (n.SupplierId == null && n.UserId == userId.Value)) &&
                                                   allowedNotificationTypes.Contains(n.Type) &&
                                                   !n.IsRead); // Sadece okunmamýþ bildirimler
                        }
                        else
                        {
                            // Tedarikçi kaydý bulunamadýysa sadece UserId ile eþleþenleri getir (yine sadece okunmamýþ)
                            query = query.Where(n => n.UserId == userId.Value && !n.IsRead);
                        }
                    }
                    else if (user.Role == UserRole.Admin)
                    {
                        // Admin için: Admin bildirimleri (UserId null olanlar) veya admin'e özel olanlar
                        query = query.Where(n => n.UserId == null || n.UserId == userId.Value);
                        
                        if (unreadOnly)
                        {
                            query = query.Where(n => !n.IsRead);
                        }
                    }
                    else
                    {
                        // Diðer roller için: Sadece kendi bildirimler
                        query = query.Where(n => n.UserId == userId.Value);
                        
                        if (unreadOnly)
                        {
                            query = query.Where(n => !n.IsRead);
                        }
                    }
                }
                else
                {
                    // Kullanýcý bulunamadýysa boþ liste döndür
                    return new List<NotificationDto>();
                }
            }
            else
            {
                // Admin bildirimleri (UserId null olanlar)
                query = query.Where(n => n.UserId == null);
                
                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedDate)
                .Take(50) // Son 50 bildirim
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedDate = n.CreatedDate,
                    IsRead = n.IsRead,
                    UserId = n.UserId,
                    RequestId = n.RequestId,
                    OfferId = n.OfferId,
                    SupplierId = n.SupplierId
                })
                .ToListAsync();

            return notifications;
        }

        public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(int? userId = null)
        {
            var query = _context.Notifications.AsQueryable();

            if (userId.HasValue)
            {
                // Kullanýcýnýn rolünü kontrol et
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    if (user.Role == UserRole.Supplier)
                    {
                        // Tedarikçi için: Sadece kendi tedarikçi ID'si ile iliþkili bildirimler
                        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                        if (supplier != null)
                        {
                            // Tedarikçiler için sadece belirli bildirim tiplerini ve sadece okunmamýþlarý göster
                            var allowedNotificationTypes = new[] { 
                                NotificationType.OfferApproved, 
                                NotificationType.OfferRejected, 
                                NotificationType.SupplierApproved, 
                                NotificationType.SupplierRejected, 
                                NotificationType.RequestSentToSupplier 
                            };
                            
                            query = query.Where(n => (n.SupplierId == supplier.Id || 
                                                   (n.SupplierId == null && n.UserId == userId.Value)) &&
                                                   allowedNotificationTypes.Contains(n.Type) &&
                                                   !n.IsRead); // Sadece okunmamýþ bildirimler
                        }
                        else
                        {
                            // Tedarikçi kaydý bulunamadýysa sadece UserId ile eþleþenleri getir (yine sadece okunmamýþ)
                            query = query.Where(n => n.UserId == userId.Value && !n.IsRead);
                        }
                    }
                    else if (user.Role == UserRole.Admin)
                    {
                        // Admin için: Admin bildirimleri (UserId null olanlar) veya admin'e özel olanlar
                        query = query.Where(n => n.UserId == null || n.UserId == userId.Value);
                    }
                    else
                    {
                        // Diðer roller için: Sadece kendi bildirimler
                        query = query.Where(n => n.UserId == userId.Value);
                    }
                }
                else
                {
                    // Kullanýcý bulunamadýysa boþ sonuç döndür
                    return new NotificationSummaryDto
                    {
                        TotalCount = 0,
                        UnreadCount = 0,
                        RecentNotifications = new List<NotificationDto>()
                    };
                }
            }
            else
            {
                // Admin bildirimleri (UserId null olanlar)
                query = query.Where(n => n.UserId == null);
            }

            var totalCount = await query.CountAsync();
            var unreadCount = await query.Where(n => !n.IsRead).CountAsync();

            var recentNotifications = await query
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    CreatedDate = n.CreatedDate,
                    IsRead = n.IsRead,
                    UserId = n.UserId,
                    RequestId = n.RequestId,
                    OfferId = n.OfferId,
                    SupplierId = n.SupplierId
                })
                .ToListAsync();

            return new NotificationSummaryDto
            {
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                RecentNotifications = recentNotifications
            };
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int? userId = null)
        {
            var query = _context.Notifications.AsQueryable();

            if (userId.HasValue)
            {
                // Kullanýcýnýn rolünü kontrol et
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    if (user.Role == UserRole.Supplier)
                    {
                        // Tedarikçi için: Sadece kendi tedarikçi ID'si ile iliþkili bildirimler
                        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                        if (supplier != null)
                        {
                            // Tedarikçiler için sadece belirli bildirim tiplerini iþaretle
                            var allowedNotificationTypes = new[] { 
                                NotificationType.OfferApproved, 
                                NotificationType.OfferRejected, 
                                NotificationType.SupplierApproved, 
                                NotificationType.SupplierRejected, 
                                NotificationType.RequestSentToSupplier 
                            };
                            
                            query = query.Where(n => (n.SupplierId == supplier.Id || 
                                                   (n.SupplierId == null && n.UserId == userId.Value)) &&
                                                   allowedNotificationTypes.Contains(n.Type));
                        }
                        else
                        {
                            // Tedarikçi kaydý bulunamadýysa sadece UserId ile eþleþenleri getir
                            query = query.Where(n => n.UserId == userId.Value);
                        }
                    }
                    else if (user.Role == UserRole.Admin)
                    {
                        // Admin için: Admin bildirimleri (UserId null olanlar) veya admin'e özel olanlar
                        query = query.Where(n => n.UserId == null || n.UserId == userId.Value);
                    }
                    else
                    {
                        // Diðer roller için: Sadece kendi bildirimler
                        query = query.Where(n => n.UserId == userId.Value);
                    }
                }
            }
            else
            {
                // Admin bildirimleri (UserId null olanlar)
                query = query.Where(n => n.UserId == null);
            }

            var notifications = await query.Where(n => !n.IsRead).ToListAsync();
            
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}