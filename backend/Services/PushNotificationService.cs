using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.Models;

namespace SantiyeTalepApi.Services
{
    public interface IPushNotificationService
    {
        Task SendNotificationToUserAsync(int userId, string title, string body, object? data = null);
        Task SendNotificationToSupplierAsync(int supplierId, string title, string body, object? data = null);
        Task SendNotificationToSuppliersAsync(List<int> supplierIds, string title, string body, object? data = null);
        Task SendBulkNotificationAsync(List<string> fcmTokens, string title, string body, object? data = null);
    }

    public class PushNotificationService : IPushNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(ApplicationDbContext context, ILogger<PushNotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendNotificationToUserAsync(int userId, string title, string body, object? data = null)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user?.FcmToken != null)
                {
                    await SendSingleNotificationAsync(user.FcmToken, title, body, data);
                }
                else
                {
                    _logger.LogWarning($"User {userId} does not have an FCM token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to user {userId}");
            }
        }

        public async Task SendNotificationToSupplierAsync(int supplierId, string title, string body, object? data = null)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == supplierId);

                if (supplier?.User?.FcmToken != null)
                {
                    await SendSingleNotificationAsync(supplier.User.FcmToken, title, body, data);
                    _logger.LogInformation($"Sent notification to supplier {supplierId}");
                }
                else
                {
                    _logger.LogWarning($"Supplier {supplierId} does not have an FCM token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to supplier {supplierId}");
            }
        }

        public async Task SendNotificationToSuppliersAsync(List<int> supplierIds, string title, string body, object? data = null)
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Include(s => s.User)
                    .Where(s => supplierIds.Contains(s.Id) && s.User.FcmToken != null)
                    .ToListAsync();

                var fcmTokens = suppliers
                    .Where(s => !string.IsNullOrEmpty(s.User.FcmToken))
                    .Select(s => s.User.FcmToken!)
                    .ToList();

                if (fcmTokens.Any())
                {
                    await SendBulkNotificationAsync(fcmTokens, title, body, data);
                    _logger.LogInformation($"Sent notifications to {fcmTokens.Count} suppliers");
                }
                else
                {
                    _logger.LogWarning("No suppliers found with FCM tokens");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notifications to suppliers");
            }
        }

        public async Task SendBulkNotificationAsync(List<string> fcmTokens, string title, string body, object? data = null)
        {
            try
            {
                // In a real implementation, you would use Firebase Admin SDK here
                // For now, we'll simulate the notification sending
                
                foreach (var token in fcmTokens)
                {
                    await SendSingleNotificationAsync(token, title, body, data);
                }

                _logger.LogInformation($"Sent bulk notifications to {fcmTokens.Count} devices");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notifications");
            }
        }

        private async Task SendSingleNotificationAsync(string fcmToken, string title, string body, object? data = null)
        {
            try
            {
                // In a real implementation, you would use Firebase Admin SDK here
                // Example with Firebase Admin SDK:
                
                // var message = new Message()
                // {
                //     Token = fcmToken,
                //     Notification = new Notification()
                //     {
                //         Title = title,
                //         Body = body,
                //     },
                //     Data = data != null ? 
                //         JsonSerializer.Serialize(data).ToDictionary() : 
                //         new Dictionary<string, string>()
                // };
                
                // var messaging = FirebaseMessaging.DefaultInstance;
                // var result = await messaging.SendAsync(message);
                
                // For now, just log the notification
                _logger.LogInformation($"Simulated FCM notification sent to token: {fcmToken.Substring(0, 10)}...");
                _logger.LogInformation($"Title: {title}");
                _logger.LogInformation($"Body: {body}");
                
                if (data != null)
                {
                    _logger.LogInformation($"Data: {System.Text.Json.JsonSerializer.Serialize(data)}");
                }

                // Simulate network delay
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to token: {fcmToken}");
            }
        }
    }
}