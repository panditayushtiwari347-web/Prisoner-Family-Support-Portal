document.addEventListener('DOMContentLoaded', function () {
    // Sidebar toggle
    const sidebarToggle = document.getElementById('sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function (e) {
            e.preventDefault();
            document.getElementById('sidebar').classList.toggle('show');
        });
    }

    // Row clickable
    const clickableRows = document.querySelectorAll('tr[data-href]');
    clickableRows.forEach(row => {
        row.style.cursor = 'pointer';
        row.addEventListener('click', () => {
            window.location.href = row.getAttribute('data-href');
        });
    });

    // Notifications fetch & polling
    const notiBadge = document.getElementById('notiBadge');
    
    function fetchNotificationCount() {
        if (!notiBadge) return;
        
        fetch('/Notifications/GetUnreadCount')
            .then(res => res.json())
            .then(data => {
                if (data.count > 0) {
                    notiBadge.textContent = data.count > 99 ? '99+' : data.count;
                    notiBadge.classList.remove('d-none');
                } else {
                    notiBadge.classList.add('d-none');
                }
            })
            .catch(err => console.error('Error fetching notification count:', err));
    }

    // Initial fetch
    fetchNotificationCount();
    // Poll every 30 seconds
    setInterval(fetchNotificationCount, 30000);

    // Mark all as read
    const markAllReadBtn = document.getElementById('markAllReadBtn');
    if (markAllReadBtn) {
        markAllReadBtn.addEventListener('click', function (e) {
            e.preventDefault();
            fetch('/Notifications/MarkAllAsRead', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                }
            }).then(() => {
                notiBadge.classList.add('d-none');
                // Could refresh notification list here
            });
        });
    }

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });
});
