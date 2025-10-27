using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;

namespace SantiyeTalepApi.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string title, string message, NotificationType type, int? userId = null, int? requestId = null, int? offerId = null, int? supplierId = null);
        Task CreateAdminNotificationAsync(string title, string message, NotificationType type, int? requestId = null, int? offerId = null, int? supplierId = null);
        Task<List<NotificationDto>> GetNotificationsAsync(int? userId = null, bool unreadOnly = false, int daysBack = 7);
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
                CreatedDate = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateAdminNotificationAsync(string title, string message, NotificationType type, int? requestId = null, int? offerId = null, int? supplierId = null)
        {
            // Admin kullanıcılarını bul
            var adminUsers = await _context.Users
                .Where(u => u.Role == UserRole.Admin && u.IsActive)
                .ToListAsync();

            // Her admin kullanıcısı için bildirim oluştur
            foreach (var admin in adminUsers)
            {
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    UserId = admin.Id, // Admin kullanıcısının ID'si
                    RequestId = requestId,
                    OfferId = offerId,
                    SupplierId = supplierId,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(int? userId = null, bool unreadOnly = false, int daysBack = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysBack);
            var query = _context.Notifications.Where(n => n.CreatedDate >= cutoffDate && n.IsRead==false).AsQueryable();

            if (userId.HasValue)
            {
                // Kullanıcının rolünü kontrol et
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    if (user.Role == UserRole.Supplier)
                    {
                        // Tedarikçi için: Sadece kendi tedarikçi ID'si ile ilişkili bildirimler
                        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                        if (supplier != null)
                        {
                            // Tedarikçiler için sadece belirli bildirim tiplerini göster (okunmuş/okunmamış tümü)
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
                            
                            // unreadOnly parametresi true ise sadece okunmamışları filtrele
                            if (unreadOnly)
                            {
                                query = query.Where(n => !n.IsRead);
                            }
                        }
                        else
                        {
                            // Tedarikçi kaydı bulunamadıysa sadece UserId ile eşleşenleri getir
                            query = query.Where(n => n.UserId == userId.Value);
                            
                            if (unreadOnly)
                            {
                                query = query.Where(n => !n.IsRead);
                            }
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
                        // Diğer roller için: Sadece kendi bildirimler
                        query = query.Where(n => n.UserId == userId.Value);
                        
                        if (unreadOnly)
                        {
                            query = query.Where(n => !n.IsRead);
                        }
                    }
                }
                else
                {
                    // Kullanıcı bulunamaduysa boş liste döndür
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

            Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - UserId: {userId}");

            if (userId.HasValue)
            {
                // Kullanıcının olünü kontrol et
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - User found, Role: {user.Role}");
                    
                    if (user.Role == UserRole.Supplier)
                    {
                        // Tedarikçi için: Sadece kendi tedarikçi ID'si ile ilişkili bildirimler
                        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                        if (supplier != null)
                        {
                            // Tedarikçiler için sadece belirli bildirim tiplerini ve sadece okunmamışları göster
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
                                                   !n.IsRead); // Sadece okunmamış bildirimler
                        }
                        else
                        {
                            // Tedarik�i kayd� bulunamad�ysa sadece UserId ile e�le�enleri getir (yine sadece okunmam��)
                            query = query.Where(n => n.UserId == userId.Value && !n.IsRead);
                        }
                    }
                    else if (user.Role == UserRole.Admin)
                    {
                        // Admin i�in: Admin bildirimleri (UserId null olanlar) veya admin'e �zel olanlar
                        query = query.Where(n => n.UserId == null || n.UserId == userId.Value);
                        Console.WriteLine("NotificationService.GetNotificationSummaryAsync - Applied Admin filter");
                    }
                    else
                    {
                        // Di�er roller i�in: Sadece kendi bildirimler
                        query = query.Where(n => n.UserId == userId.Value);
                        Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - Applied Employee filter for UserId: {userId.Value}");
                    }
                }
                else
                {
                    Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - User not found for UserId: {userId.Value}");
                    // Kullan�c� bulunamad�ysa bo� sonu� d�nd�r
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
                Console.WriteLine("NotificationService.GetNotificationSummaryAsync - UserId is null, applying admin filter");
                // Admin bildirimleri (UserId null olanlar)
                query = query.Where(n => n.UserId == null);
            }

            var totalCount = await query.CountAsync();
            var unreadCount = await query.Where(n => !n.IsRead).CountAsync();

            Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - TotalCount: {totalCount}, UnreadCount: {unreadCount}");

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
                // Kullan�c�n�n rol�n� kontrol et
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    if (user.Role == UserRole.Supplier)
                    {
                        // Tedarik�i i�in: Sadece kendi tedarik�i ID'si ile ili�kili bildirimler
                        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                        if (supplier != null)
                        {
                            // Tedarik�iler i�in sadece belirli bildirim tiplerini i�aretle
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
                            // Tedarik�i kayd� bulunamad�ysa sadece UserId ile e�le�enleri getir
                            query = query.Where(n => n.UserId == userId.Value);
                        }
                    }
                    else if (user.Role == UserRole.Admin)
                    {
                        // Admin i�in: Admin bildirimleri (UserId null olanlar) veya admin'e �zel olanlar
                        query = query.Where(n => n.UserId == null || n.UserId == userId.Value);
                    }
                    else
                    {
                        // Di�er roller i�in: Sadece kendi bildirimler
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