using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Data;
using SantiyeTalepApi.Models;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System.Text.Json;

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
        private readonly bool _isFirebaseInitialized;

        public PushNotificationService(ApplicationDbContext context, ILogger<PushNotificationService> logger)
        {
            _context = context;
            _logger = logger;

            // Initialize Firebase Admin SDK if not already initialized
            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    // Check if service account file exists
                    var serviceAccountPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-service-account.json");
                    
                    if (File.Exists(serviceAccountPath))
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromFile(serviceAccountPath)
                        });
                        _isFirebaseInitialized = true;
                        _logger.LogInformation("Firebase Admin SDK initialized successfully");
                    }
                    else
                    {
                        _isFirebaseInitialized = false;
                        _logger.LogWarning("Firebase service account file not found. Push notifications will be simulated.");
                        _logger.LogWarning($"Expected path: {serviceAccountPath}");
                    }
                }
                else
                {
                    _isFirebaseInitialized = true;
                }
            }
            catch (Exception ex)
            {
                _isFirebaseInitialized = false;
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK. Push notifications will be simulated.");
            }
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
                if (_isFirebaseInitialized)
                {
                    // Use data-only messages for reliable background delivery
                    // The notification will be displayed by the client app
                    var dataDict = new Dictionary<string, string>
                    {
                        ["title"] = title,
                        ["body"] = body
                    };

                    // Add custom data payload if provided
                    if (data != null)
                    {
                        var jsonData = JsonSerializer.Serialize(data);
                        var dataObj = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                        
                        if (dataObj != null)
                        {
                            foreach (var kvp in dataObj)
                            {
                                dataDict[kvp.Key] = kvp.Value?.ToString() ?? "";
                            }
                        }
                    }

                    // Create message with data-only payload (no notification field)
                    // This ensures the message is delivered in all app states
                    var messageBuilder = new Message()
                    {
                        Token = fcmToken,
                        Data = dataDict,
                        Android = new AndroidConfig()
                        {
                            Priority = Priority.High,
                        },
                        Apns = new ApnsConfig()
                        {
                            Headers = new Dictionary<string, string>
                            {
                                ["apns-priority"] = "10"
                            }
                        }
                    };

                    var messaging = FirebaseMessaging.DefaultInstance;
                    var result = await messaging.SendAsync(messageBuilder);
                    
                    _logger.LogInformation($"Successfully sent FCM data message. Message ID: {result}");
                }
                else
                {
                    // Simulated implementation for development
                    _logger.LogInformation($"[SIMULATED] FCM notification sent to token: {fcmToken.Substring(0, Math.Min(10, fcmToken.Length))}...");
                    _logger.LogInformation($"[SIMULATED] Title: {title}");
                    _logger.LogInformation($"[SIMULATED] Body: {body}");
                    
                    if (data != null)
                    {
                        _logger.LogInformation($"[SIMULATED] Data: {JsonSerializer.Serialize(data)}");
                    }

                    // Simulate network delay
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to token: {fcmToken}");
            }
        }
    }
}