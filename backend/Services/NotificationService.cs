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
        private readonly IPushNotificationService _pushNotificationService;

        public NotificationService(ApplicationDbContext context, IPushNotificationService pushNotificationService)
        {
            _context = context;
            _pushNotificationService = pushNotificationService;
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

            // Push notification gönder
            if (userId.HasValue)
            {
                var notificationData = new
                {
                    notificationId = notification.Id,
                    type = (int)type,
                    requestId = requestId,
                    offerId = offerId,
                    supplierId = supplierId
                };

                await _pushNotificationService.SendNotificationToUserAsync(
                    userId.Value,
                    title,
                    message,
                    notificationData
                );
            }
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

            // Push notification gönder (tüm adminlere)
            var notificationData = new
            {
                type = (int)type,
                requestId = requestId,
                offerId = offerId,
                supplierId = supplierId
            };

            foreach (var admin in adminUsers)
            {
                await _pushNotificationService.SendNotificationToUserAsync(
                    admin.Id,
                    title,
                    message,
                    notificationData
                );
            }
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(int? userId = null, bool unreadOnly = false, int daysBack = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysBack);
            var query = _context.Notifications.Where(n => n.CreatedDate >= cutoffDate).AsQueryable();

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
                                NotificationType.RequestSentToSupplier,
                                NotificationType.ExcelRequestAssigned
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
            Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - UserId: {userId}");

            if (userId.HasValue)
            {
                // Kullanıcının rolünü kontrol et
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
                            // Tedarikçiler için sadece belirli bildirim tiplerini göster
                            var allowedNotificationTypes = new[] { 
                                NotificationType.OfferApproved, 
                                NotificationType.OfferRejected, 
                                NotificationType.SupplierApproved, 
                                NotificationType.SupplierRejected, 
                                NotificationType.RequestSentToSupplier,
                                NotificationType.ExcelRequestAssigned
                            };
                            
                            var supplierQuery = _context.Notifications
                                .Where(n => (n.SupplierId == supplier.Id || (n.SupplierId == null && n.UserId == userId.Value)) &&
                                           allowedNotificationTypes.Contains(n.Type));
                            
                            var totalCount = await supplierQuery.CountAsync();
                            var unreadCount = await supplierQuery.Where(n => !n.IsRead).CountAsync();
                            
                            var recentNotifications = await supplierQuery
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

                            Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - Supplier TotalCount: {totalCount}, UnreadCount: {unreadCount}");

                            return new NotificationSummaryDto
                            {
                                TotalCount = totalCount,
                                UnreadCount = unreadCount,
                                RecentNotifications = recentNotifications
                            };
                        }
                        else
                        {
                            // Tedarikçi kaydı bulunamadıysa sadece UserId ile eşleşenleri getir
                            var userQuery = _context.Notifications.Where(n => n.UserId == userId.Value);
                            
                            var totalCount = await userQuery.CountAsync();
                            var unreadCount = await userQuery.Where(n => !n.IsRead).CountAsync();
                            
                            var recentNotifications = await userQuery
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
                    }
                    else if (user.Role == UserRole.Admin)
                    {
                        // Admin için: Tüm admin bildirimleri (UserId ile eşleşenler dahil)
                        var adminQuery = _context.Notifications
                            .Where(n => n.UserId == null || n.UserId == userId.Value);
                        
                        Console.WriteLine("NotificationService.GetNotificationSummaryAsync - Applied Admin filter");
                        
                        var totalCount = await adminQuery.CountAsync();
                        var unreadCount = await adminQuery.Where(n => !n.IsRead).CountAsync();
                        
                        Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - Admin TotalCount: {totalCount}, UnreadCount: {unreadCount}");
                        
                        var recentNotifications = await adminQuery
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
                    else
                    {
                        // Diğer roller (Employee) için: Sadece kendi bildirimleri
                        var employeeQuery = _context.Notifications.Where(n => n.UserId == userId.Value);
                        
                        Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - Applied Employee filter for UserId: {userId.Value}");
                        
                        var totalCount = await employeeQuery.CountAsync();
                        var unreadCount = await employeeQuery.Where(n => !n.IsRead).CountAsync();
                        
                        var recentNotifications = await employeeQuery
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
                }
                else
                {
                    Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - User not found for UserId: {userId.Value}");
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
                var adminQuery = _context.Notifications.Where(n => n.UserId == null);
                
                var totalCount = await adminQuery.CountAsync();
                var unreadCount = await adminQuery.Where(n => !n.IsRead).CountAsync();
                
                Console.WriteLine($"NotificationService.GetNotificationSummaryAsync - No UserId - TotalCount: {totalCount}, UnreadCount: {unreadCount}");
                
                var recentNotifications = await adminQuery
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