/**
 * Supplier Notification Checker
 * Automatically checks for new notifications and displays toast notifications
 */

class SupplierNotificationChecker {
    constructor(options = {}) {
        this.checkInterval = options.checkInterval || 30000; // 30 seconds
        this.notificationSound = options.sound || true;
        this.lastNotificationTime = 0;
        this.intervalId = null;
        this.lastNotificationId = null;
        this.lastShownRequestCount = 0; // Track last shown request count
        this.lastShownNotificationCount = 0; // Track last shown notification count
        
        this.toastSettings = {
            duration: 5000,
            closable: true,
            position: 'top-right'
        };
        
        // Check if we're on supplier page
        if (this.isSupplierPage()) {
            this.initialize();
        }
    }

    isSupplierPage() {
        return window.location.pathname.toLowerCase().includes('/supplier');
    }

    initialize() {
        console.log('Supplier Notification Checker initialized');
        
        // Start checking for notifications
        this.startChecking();
        
        // Listen for visibility changes to pause/resume checking
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                this.stopChecking();
            } else {
                this.startChecking();
                this.checkNow(); // Check immediately when page becomes visible
            }
        });
    }

    startChecking() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
        }
        
        // Check immediately
        this.checkNow();
        
        // Then check periodically
        this.intervalId = setInterval(() => {
            this.checkNow();
        }, this.checkInterval);
        
        console.log('Notification checking started (every 30 seconds)');
    }

    stopChecking() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
            console.log('Notification checking stopped');
        }
    }

    async checkNow() {
        try {
            // Check for new requests assigned to supplier
            const response = await fetch('/Supplier/CheckNewRequests');
            if (!response.ok) {
                console.warn('Failed to check new requests:', response.status);
                return;
            }

            const result = await response.json();
            
            if (result.success && result.data) {
                this.handleSupplierNotifications(result.data);
            }
        } catch (error) {
            console.error('Error checking notifications:', error);
        }
    }

    handleSupplierNotifications(data) {
        const { newRequestCount, unreadNotificationCount, hasNewContent } = data;
        
        // Only proceed if there's actually NEW content (increased from last check)
        if (hasNewContent) {
            // Check for new requests - only show if count INCREASED
            if (newRequestCount > 0 && newRequestCount > this.lastShownRequestCount) {
                const message = `${newRequestCount} yeni talep size atandý!`;
                this.showNotification('Yeni Talep', message, 'info', {
                    onClick: () => {
                        window.location.href = '/Supplier/AvailableRequests';
                    }
                });
                this.lastShownRequestCount = newRequestCount;
            }
            
            // Check for unread notifications - only load if count INCREASED
            if (unreadNotificationCount > 0 && unreadNotificationCount > this.lastShownNotificationCount) {
                this.loadAndDisplayNotifications();
                this.lastShownNotificationCount = unreadNotificationCount;
            }
        }
        
        // Update counters when they decrease (user read notifications)
        if (newRequestCount < this.lastShownRequestCount) {
            this.lastShownRequestCount = newRequestCount;
        }
        if (unreadNotificationCount < this.lastShownNotificationCount) {
            this.lastShownNotificationCount = unreadNotificationCount;
        }
        
        // Always update dashboard counters
        this.updateDashboardCounters({
            newRequests: newRequestCount || 0,
            notifications: unreadNotificationCount || 0
        });
    }

    async loadAndDisplayNotifications() {
        try {
            const response = await fetch('/Supplier/GetNotifications');
            if (!response.ok) return;

            const result = await response.json();
            
            if (result.success && result.data && result.data.length > 0) {
                // Get the most recent unread notification
                const latestNotification = result.data
                    .filter(n => !n.isRead)
                    .sort((a, b) => new Date(b.createdDate) - new Date(a.createdDate))[0];
                
                if (latestNotification && latestNotification.id !== this.lastNotificationId) {
                    this.lastNotificationId = latestNotification.id;
                    
                    const message = latestNotification.message || this.getNotificationMessage(latestNotification);
                    const type = this.getNotificationType(latestNotification.type);
                    
                    this.showNotification(
                        latestNotification.title || 'Yeni Bildirim',
                        message,
                        type,
                        {
                            onClick: () => {
                                this.navigateToNotification(latestNotification);
                            }
                        }
                    );
                }
            }
        } catch (error) {
            console.error('Error loading notifications:', error);
        }
    }

    getNotificationMessage(notification) {
        switch (notification.type) {
            case 10: // RequestSentToSupplier
                return 'Size yeni bir talep atandý';
            case 11: // ExcelRequestAssigned
                return 'Size yeni bir Excel talebi atandý';
            case 6: // OfferApproved
                return 'Teklifiniz onaylandý';
            case 7: // OfferRejected
                return 'Teklifiniz reddedildi';
            case 8: // SupplierApproved
                return 'Hesabýnýz onaylandý';
            case 9: // SupplierRejected
                return 'Hesabýnýz reddedildi';
            default:
                return notification.message || 'Yeni bildirim';
        }
    }

    getNotificationType(type) {
        switch (type) {
            case 10: // RequestSentToSupplier
            case 11: // ExcelRequestAssigned
                return 'info';
            case 6: // OfferApproved
            case 8: // SupplierApproved
                return 'success';
            case 7: // OfferRejected
            case 9: // SupplierRejected
                return 'error';
            default:
                return 'info';
        }
    }

    navigateToNotification(notification) {
        // Navigate based on notification type
        let targetUrl = '/Supplier/Dashboard';
        
        switch(notification.type) {
            case 10: // RequestSentToSupplier
                if (notification.requestId) {
                    targetUrl = `/Supplier/RequestDetails?id=${notification.requestId}`;
                } else {
                    targetUrl = '/Supplier/AvailableRequests';
                }
                break;
            case 11: // ExcelRequestAssigned
                targetUrl = '/Supplier/ExcelRequests'; // Yeni sayfa
                break;
            case 6: // OfferApproved
            case 7: // OfferRejected
                if (notification.offerId) {
                    targetUrl = `/Supplier/OfferDetails?id=${notification.offerId}`;
                } else {
                    targetUrl = '/Supplier/Offers';
                }
                break;
            case 8: // SupplierApproved
            case 9: // SupplierRejected
                targetUrl = '/Supplier/Dashboard';
                break;
        }
        
        window.location.href = targetUrl;
    }

    showNotification(title, message, type = 'info', options = {}) {
        // Avoid spam - don't show more than once per 30 seconds
        const now = Date.now();
        if (now - this.lastNotificationTime < 30000) {
            return;
        }
        this.lastNotificationTime = now;
        
        // Play sound if enabled
        if (this.notificationSound) {
            this.playNotificationSound();
        }
        
        // Try different notification libraries
        if (typeof Toastify !== 'undefined') {
            this.showToastifyNotification(title, message, type, options);
        } else if (typeof toastr !== 'undefined') {
            this.showToastrNotification(title, message, type, options);
        } else if (typeof Swal !== 'undefined') {
            this.showSwalNotification(title, message, type, options);
        } else {
            this.showBrowserNotification(title, message, options);
        }
    }

    showToastifyNotification(title, message, type, options) {
        const backgroundColor = this.getTypeColor(type);
        
        const toast = Toastify({
            text: `<div class="notification-toast">
                     <div class="notification-title">${title}</div>
                     <div class="notification-message">${message}</div>
                   </div>`,
            duration: this.toastSettings.duration,
            close: this.toastSettings.closable,
            gravity: "top",
            position: "right",
            backgroundColor: backgroundColor,
            stopOnFocus: true,
            escapeMarkup: false,
            onClick: options.onClick || null
        });
        
        toast.showToast();
    }

    showToastrNotification(title, message, type, options) {
        toastr.options = {
            closeButton: true,
            progressBar: true,
            positionClass: "toast-top-right",
            timeOut: this.toastSettings.duration,
            onclick: options.onClick || null
        };
        
        switch(type) {
            case 'success':
                toastr.success(message, title);
                break;
            case 'error':
            case 'danger':
                toastr.error(message, title);
                break;
            case 'warning':
                toastr.warning(message, title);
                break;
            default:
                toastr.info(message, title);
        }
    }

    showSwalNotification(title, message, type, options) {
        const swalType = type === 'danger' ? 'error' : type;
        
        Swal.fire({
            title: title,
            text: message,
            icon: swalType,
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: this.toastSettings.duration,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('click', () => {
                    if (options.onClick) {
                        options.onClick();
                    }
                    Swal.close();
                });
            }
        });
    }

    showBrowserNotification(title, message, options) {
        // Check if browser supports notifications
        if (!("Notification" in window)) {
            console.warn('This browser does not support desktop notifications');
            return;
        }

        // Check permission
        if (Notification.permission === "granted") {
            this.displayBrowserNotification(title, message, options);
        } else if (Notification.permission !== "denied") {
            Notification.requestPermission().then(permission => {
                if (permission === "granted") {
                    this.displayBrowserNotification(title, message, options);
                }
            });
        }
    }

    displayBrowserNotification(title, message, options) {
        const notification = new Notification(title, {
            body: message,
            icon: '/favicon.ico',
            badge: '/favicon.ico',
            tag: 'supplier-notification',
            requireInteraction: false
        });

        notification.onclick = () => {
            window.focus();
            if (options.onClick) {
                options.onClick();
            }
            notification.close();
        };
    }

    playNotificationSound() {
        try {
            const audio = new Audio('/sounds/notification.mp3');
            audio.volume = 0.5;
            audio.play().catch(err => {
                console.warn('Could not play notification sound:', err);
            });
        } catch (error) {
            console.warn('Error playing notification sound:', error);
        }
    }

    getTypeColor(type) {
        switch(type) {
            case 'success':
                return 'linear-gradient(135deg, #34c38f 0%, #2fa97c 100%)';
            case 'error':
            case 'danger':
                return 'linear-gradient(135deg, #f46a6a 0%, #d9534f 100%)';
            case 'warning':
                return 'linear-gradient(135deg, #f1b44c 0%, #e5a835 100%)';
            case 'info':
            default:
                return 'linear-gradient(135deg, #556ee6 0%, #4054b2 100%)';
        }
    }

    updateDashboardCounters(counters) {
        // Update notification badge in header
        const notificationBadge = document.querySelector('.notification-badge');
        if (notificationBadge && counters.notifications) {
            notificationBadge.textContent = counters.notifications;
            notificationBadge.style.display = counters.notifications > 0 ? 'inline-block' : 'none';
        }

        // Update new requests counter
        const newRequestsCounter = document.querySelector('.new-requests-counter');
        if (newRequestsCounter && counters.newRequests !== undefined) {
            newRequestsCounter.textContent = counters.newRequests;
        }

        // Update notification counter elements
        document.querySelectorAll('.notifications-count').forEach(el => {
            if (counters.notifications !== undefined) {
                el.textContent = counters.notifications;
                el.style.display = counters.notifications > 0 ? 'inline-block' : 'none';
            }
        });
    }

    destroy() {
        this.stopChecking();
        console.log('Supplier Notification Checker destroyed');
    }
}

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Check if we're on a supplier page
    const isSupplierPage = window.location.pathname.toLowerCase().includes('/supplier');
    
    if (isSupplierPage) {
        window.supplierNotificationChecker = new SupplierNotificationChecker({
            checkInterval: 30000, // 30 seconds
            sound: true
        });
        
        console.log('Supplier notification checker auto-initialized');
    }
});

// Export for manual initialization if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SupplierNotificationChecker;
}
