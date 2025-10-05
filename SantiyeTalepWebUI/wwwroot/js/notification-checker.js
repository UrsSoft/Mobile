/**
 * Notification Checker - Real-time notification system for new requests
 * Checks for new requests every 60 seconds and shows notifications
 */

class NotificationChecker {
    constructor(options = {}) {
        this.checkInterval = options.checkInterval || 60000; // 60 seconds default
        this.checkUrl = options.checkUrl || '';
        this.userRole = options.userRole || 'supplier';
        this.intervalId = null;
        this.isInitialized = false;
        this.lastNotificationTime = Date.now();
        this.notificationSound = options.enableSound !== false; // Enable by default
        
        // Toast notification settings
        this.toastSettings = {
            position: 'top-right',
            duration: 8000,
            closable: true
        };
        
        this.init();
    }

    init() {
        if (this.isInitialized) return;
        
        // Check if page is visible to avoid unnecessary API calls
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                this.stop();
            } else {
                this.start();
            }
        });

        // Start checking immediately
        this.start();
        this.isInitialized = true;
        
        console.log('NotificationChecker initialized for', this.userRole);
    }

    start() {
        if (this.intervalId) return; // Already running
        
        // Initial check
        this.checkForNewRequests();
        
        // Set up periodic checking
        this.intervalId = setInterval(() => {
            this.checkForNewRequests();
        }, this.checkInterval);
        
        console.log('NotificationChecker started - checking every', this.checkInterval / 1000, 'seconds');
    }

    stop() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
            console.log('NotificationChecker stopped');
        }
    }

    async checkForNewRequests() {
        try {
            const response = await fetch(this.checkUrl, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            
            if (result.success && result.data) {
                this.handleNewRequestsResponse(result.data);
            }
        } catch (error) {
            console.error('Error checking for new requests:', error);
            // Don't show error to user unless it's critical
        }
    }

    handleNewRequestsResponse(data) {
        if (this.userRole === 'supplier') {
            this.handleSupplierNotifications(data);
        } else if (this.userRole === 'admin') {
            this.handleAdminNotifications(data);
        }
    }

    handleSupplierNotifications(data) {
        const { newRequestCount, unreadNotificationCount, hasNewContent } = data;
        
        if (hasNewContent) {
            let message = '';
            let notificationTitle = 'Yeni Bildirim';
            let type = 'info';
            
            if (newRequestCount > 0 && unreadNotificationCount > 0) {
                message = `${newRequestCount} yeni talep ve ${unreadNotificationCount} okunmam\u0131\u015f bildiriminiz var!`;
                notificationTitle = 'Yeni Talepler ve Bildirimler';
                type = 'success';
            } else if (newRequestCount > 0) {
                message = `${newRequestCount} yeni talep bulunuyor! Teklif verebilirsiniz.`;
                notificationTitle = 'Yeni Talepler';
                type = 'success';
            } else if (unreadNotificationCount > 0) {
                message = `${unreadNotificationCount} okunmam\u0131\u015f bildiriminiz var.`;
                notificationTitle = 'Yeni Bildirimler';
                type = 'info';
            }
            
            if (message) {
                this.showNotification(notificationTitle, message, type, {
                    onClick: () => {
                        if (newRequestCount > 0) {
                            window.location.href = '/Supplier/AvailableRequests';
                        } else {
                            // Show notifications panel
                            if (typeof toggleNotifications === 'function') {
                                toggleNotifications();
                            }
                        }
                    }
                });
                
                // Update dashboard counters if elements exist
                this.updateDashboardCounters({
                    newRequests: newRequestCount,
                    notifications: unreadNotificationCount
                });
            }
        }
    }

    handleAdminNotifications(data) {
        const { newRequestsToday, pendingSuppliersCount, pendingOffersCount, hasNewContent } = data;
        
        if (hasNewContent) {
            let messages = [];
            
            if (newRequestsToday > 0) {
                messages.push(`${newRequestsToday} yeni talep`);
            }
            if (pendingSuppliersCount > 0) {
                messages.push(`${pendingSuppliersCount} bekleyen tedarikçi`);
            }
            if (pendingOffersCount > 0) {
                messages.push(`${pendingOffersCount} bekleyen teklif`);
            }
            
            if (messages.length > 0) {
                const message = messages.join(', ') + ' bulunuyor.';
                this.showNotification('Yeni Aktiviteler', message, 'warning', {
                    onClick: () => {
                        if (newRequestsToday > 0) {
                            window.location.href = '/Admin/Requests';
                        } else if (pendingSuppliersCount > 0) {
                            window.location.href = '/Admin/PendingSuppliers';
                        } else if (pendingOffersCount > 0) {
                            window.location.href = '/Admin/PendingOffers';
                        }
                    }
                });
                
                // Update dashboard counters if elements exist
                this.updateDashboardCounters({
                    newRequests: newRequestsToday,
                    pendingSuppliers: pendingSuppliersCount,
                    pendingOffers: pendingOffersCount
                });
            }
        }
    }

    showNotification(title, message, type = 'info', options = {}) {
        // Avoid spam notifications - don't show more than once per minute
        const now = Date.now();
        if (now - this.lastNotificationTime < 60000) {
            return;
        }
        this.lastNotificationTime = now;
        
        // Play notification sound if enabled
        if (this.notificationSound) {
            this.playNotificationSound();
        }
        
        // Try to use the toast notification system if available
        if (typeof Toastify !== 'undefined') {
            this.showToastifyNotification(title, message, type, options);
        } else if (typeof toastr !== 'undefined') {
            this.showToastrNotification(title, message, type, options);
        } else {
            // Fallback to browser notification or alert
            this.showBrowserNotification(title, message, options);
        }
    }

    showToastifyNotification(title, message, type, options) {
        const backgroundColor = this.getTypeColor(type);
        
        Toastify({
            text: `<div class="notification-content">
                     <div class="notification-title">${title}</div>
                     <div class="notification-message">${message}</div>
                   </div>`,
            duration: this.toastSettings.duration,
            close: this.toastSettings.closable,
            gravity: "top",
            position: "right",
            backgroundColor: backgroundColor,
            className: `notification-toast notification-${type}`,
            escapeMarkup: false,
            onClick: options.onClick
        }).showToast();
    }

    showToastrNotification(title, message, type, options) {
        toastr.options = {
            "closeButton": true,
            "progressBar": true,
            "positionClass": "toast-top-right",
            "timeOut": this.toastSettings.duration,
            "onclick": options.onClick
        };
        
        toastr[type === 'success' ? 'success' : type === 'warning' ? 'warning' : 'info'](message, title);
    }

    showBrowserNotification(title, message, options) {
        // Check if browser supports notifications
        if ("Notification" in window) {
            // Request permission if not granted
            if (Notification.permission === "granted") {
                const notification = new Notification(title, {
                    body: message,
                    icon: '/favicon.ico',
                    tag: 'santiye-talep'
                });
                
                notification.onclick = function() {
                    window.focus();
                    if (options.onClick) {
                        options.onClick();
                    }
                    notification.close();
                };
                
                // Auto close after 8 seconds
                setTimeout(() => notification.close(), 8000);
            } else if (Notification.permission !== "denied") {
                Notification.requestPermission().then(permission => {
                    if (permission === "granted") {
                        this.showBrowserNotification(title, message, options);
                    }
                });
            }
        } else {
            // Fallback to alert for very old browsers
            alert(`${title}: ${message}`);
        }
    }

    updateDashboardCounters(counts) {
        // Update various counter elements on the dashboard
        if (counts.newRequests !== undefined) {
            const elements = document.querySelectorAll('.new-requests-count, .requests-count');
            elements.forEach(el => {
                el.textContent = counts.newRequests;
                if (counts.newRequests > 0) {
                    el.classList.add('badge-pulse');
                }
            });
        }
        
        if (counts.notifications !== undefined) {
            const elements = document.querySelectorAll('.notifications-count, .unread-count');
            elements.forEach(el => {
                el.textContent = counts.notifications;
                if (counts.notifications > 0) {
                    el.classList.add('badge-pulse');
                }
            });
        }
        
        if (counts.pendingSuppliers !== undefined) {
            const elements = document.querySelectorAll('.pending-suppliers-count');
            elements.forEach(el => {
                el.textContent = counts.pendingSuppliers;
                if (counts.pendingSuppliers > 0) {
                    el.classList.add('badge-pulse');
                }
            });
        }
        
        if (counts.pendingOffers !== undefined) {
            const elements = document.querySelectorAll('.pending-offers-count');
            elements.forEach(el => {
                el.textContent = counts.pendingOffers;
                if (counts.pendingOffers > 0) {
                    el.classList.add('badge-pulse');
                }
            });
        }
    }

    playNotificationSound() {
        try {
            // Create and play a subtle notification sound
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();
            
            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);
            
            oscillator.frequency.setValueAtTime(800, audioContext.currentTime);
            oscillator.frequency.exponentialRampToValueAtTime(600, audioContext.currentTime + 0.1);
            
            gainNode.gain.setValueAtTime(0.1, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.1);
            
            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.1);
        } catch (error) {
            // Sound failed, but that's okay - continue silently
        }
    }

    getTypeColor(type) {
        const colors = {
            'success': '#28a745',
            'info': '#17a2b8',
            'warning': '#ffc107',
            'danger': '#dc3545',
            'error': '#dc3545'
        };
        return colors[type] || colors.info;
    }

    // Public methods for manual control
    manualCheck() {
        this.checkForNewRequests();
    }

    setInterval(newInterval) {
        this.checkInterval = newInterval;
        if (this.intervalId) {
            this.stop();
            this.start();
        }
    }

    destroy() {
        this.stop();
        this.isInitialized = false;
    }
}

// Export for global use
window.NotificationChecker = NotificationChecker;

// Auto-initialize based on page context
document.addEventListener('DOMContentLoaded', function() {
    // Detect user role from page context
    const bodyClasses = document.body.className;
    let userRole = 'supplier'; // default
    let checkUrl = '/Supplier/CheckNewRequests';
    
    if (bodyClasses.includes('admin-page') || window.location.pathname.startsWith('/Admin')) {
        userRole = 'admin';
        checkUrl = '/Admin/CheckNewRequests';
    } else if (bodyClasses.includes('supplier-page') || window.location.pathname.startsWith('/Supplier')) {
        userRole = 'supplier';
        checkUrl = '/Supplier/CheckNewRequests';
    }
    
    // Only initialize on dashboard pages or if explicitly enabled
    const isDashboard = window.location.pathname.includes('Dashboard') || 
                       document.querySelector('[data-enable-notifications="true"]');
    
    if (isDashboard) {
        // Initialize notification checker
        window.notificationChecker = new NotificationChecker({
            userRole: userRole,
            checkUrl: checkUrl,
            checkInterval: 60000, // 60 seconds
            enableSound: true
        });
        
        console.log('Real-time notifications enabled for', userRole);
    }
});