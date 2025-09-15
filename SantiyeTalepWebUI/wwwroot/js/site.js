// Custom JavaScript for Şantiye Talep Sistemi

// Auto-hide alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function() {
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    alerts.forEach(function(alert) {
        setTimeout(function() {
            if (alert && alert.parentNode) {
                alert.style.transition = 'opacity 0.5s';
                alert.style.opacity = '0';
                setTimeout(function() {
                    if (alert && alert.parentNode) {
                        alert.remove();
                    }
                }, 500);
            }
        }, 5000);
    });
});

// Confirm delete actions
function confirmDelete(message) {
    return confirm(message || 'Bu işlemi gerçekleştirmek istediğinizden emin misiniz?');
}

// Format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('tr-TR', {
        style: 'currency',
        currency: 'TRY'
    }).format(amount);
}

// Format date
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('tr-TR');
}

// Status badge helper
function getStatusBadge(status, type) {
    const badges = {
        request: {
            1: '<span class="badge status-open">Açık</span>',
            2: '<span class="badge status-pending">Devam Ediyor</span>',
            3: '<span class="badge status-completed">Tamamlandı</span>',
            4: '<span class="badge status-cancelled">İptal Edildi</span>'
        },
        offer: {
            1: '<span class="badge status-pending">Bekliyor</span>',
            2: '<span class="badge status-approved">Onaylandı</span>',
            3: '<span class="badge status-rejected">Reddedildi</span>'
        },
        supplier: {
            1: '<span class="badge status-pending">Bekliyor</span>',
            2: '<span class="badge status-approved">Onaylandı</span>',
            3: '<span class="badge status-rejected">Reddedildi</span>'
        }
    };
    
    return badges[type] && badges[type][status] ? badges[type][status] : '<span class="badge bg-secondary">Bilinmiyor</span>';
}

// Search functionality
function filterTable(searchInput, tableId) {
    const filter = searchInput.value.toUpperCase();
    const table = document.getElementById(tableId);
    const rows = table.getElementsByTagName('tr');
    
    for (let i = 1; i < rows.length; i++) {
        const row = rows[i];
        const cells = row.getElementsByTagName('td');
        let found = false;
        
        for (let j = 0; j < cells.length; j++) {
            const cell = cells[j];
            if (cell && cell.textContent.toUpperCase().indexOf(filter) > -1) {
                found = true;
                break;
            }
        }
        
        row.style.display = found ? '' : 'none';
    }
}

// Initialize tooltips
document.addEventListener('DOMContentLoaded', function() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function(tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});

// Form validation enhancement
document.addEventListener('DOMContentLoaded', function() {
    const forms = document.querySelectorAll('.needs-validation');
    
    Array.prototype.slice.call(forms).forEach(function(form) {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
});