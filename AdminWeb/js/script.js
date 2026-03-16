// Global variables
let currentUser = null;
let apiBaseUrl = 'http://localhost:5000/api';

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    const token = localStorage.getItem('adminToken');
    const currentPage = window.location.pathname.split('/').pop();
    
    if (currentPage === 'dashboard.html' && !token) {
        window.location.href = 'index.html';
        return;
    }
    
    if (currentPage === 'dashboard.html') {
        initDashboard();
        updateDateTime();
        setInterval(updateDateTime, 1000);
        loadDashboardData();
        setInterval(loadDashboardData, 30000);
    }
});

// Update date and time
function updateDateTime() {
    const now = new Date();
    const options = { 
        weekday: 'long', 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    };
    const dateTimeStr = now.toLocaleDateString('vi-VN', options);
    const dateTimeElement = document.getElementById('currentDateTime');
    if (dateTimeElement) {
        dateTimeElement.textContent = dateTimeStr;
    }
}

// Show section
function showSection(sectionId) {
    document.querySelectorAll('.sidebar-nav a').forEach(link => {
        link.classList.remove('active');
    });
    event.currentTarget.classList.add('active');
    
    const titles = {
        'dashboard': 'Dashboard',
        'keys': 'Quản lý Key',
        'accounts': 'Tài khoản Riot',
        'sessions': 'Phiên hoạt động',
        'violations': 'Vi phạm',
        'revenue': 'Doanh thu',
        'settings': 'Cài đặt'
    };
    document.getElementById('pageTitle').textContent = titles[sectionId];
    
    document.querySelectorAll('.section').forEach(section => {
        section.classList.remove('active');
    });
    document.getElementById(sectionId + '-section').classList.add('active');
    
    switch(sectionId) {
        case 'keys':
            loadKeys();
            break;
        case 'accounts':
            loadAccounts();
            break;
        case 'sessions':
            loadSessions();
            break;
        case 'violations':
            loadViolations();
            break;
        case 'revenue':
            loadRevenueData();
            break;
    }
}

// Initialize dashboard
function initDashboard() {
    initRevenueChart();
    initGameChart();
}

// Initialize revenue chart
function initRevenueChart() {
    const ctx = document.getElementById('revenueChart').getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'],
            datasets: [{
                label: 'Doanh thu (VNĐ)',
                data: [0, 0, 0, 0, 0, 0, 0],
                borderColor: '#ff4655',
                backgroundColor: 'rgba(255, 70, 85, 0.1)',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(255, 255, 255, 0.1)'
                    },
                    ticks: {
                        color: '#888'
                    }
                },
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        color: '#888'
                    }
                }
            }
        }
    });
}

// Initialize game distribution chart
function initGameChart() {
    const ctx = document.getElementById('gameChart').getContext('2d');
    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Valorant'],
            datasets: [{
                data: [100],
                backgroundColor: ['#ff4655'],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        color: '#888'
                    }
                }
            },
            cutout: '70%'
        }
    });
}

// Load dashboard data
async function loadDashboardData() {
    try {
        const response = await fetch(`${apiBaseUrl}/admin/statistics`);
        const data = await response.json();
        
        document.getElementById('totalUsers').textContent = data.totalUsers || 0;
        document.getElementById('activeSessions').textContent = data.activeSessions || 0;
        document.getElementById('availableAccounts').textContent = data.availableAccounts || 0;
        document.getElementById('todayRevenue').textContent = formatCurrency(data.todayRevenue || 0);
        
        updateRecentActivities(data.recentActivities || []);
        updateCharts(data);
    } catch (error) {
        console.error('Error loading dashboard data:', error);
        showToast('❌ Không thể tải dữ liệu dashboard', 'error');
    }
}

// Update recent activities
function updateRecentActivities(activities) {
    const tbody = document.getElementById('recentActivities');
    
    if (!activities || activities.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" class="loading">Không có hoạt động nào</td></tr>';
        return;
    }
    
    let html = '';
    activities.forEach(activity => {
        html += `
            <tr>
                <td>${formatDateTime(activity.time)}</td>
                <td>${activity.user}</td>
                <td>${activity.action}</td>
                <td>${activity.details}</td>
            </tr>
        `;
    });
    
    tbody.innerHTML = html;
}

// Load keys data - ĐÃ THÊM NÚT FORCE DELETE
async function loadKeys() {
    try {
        const response = await fetch(`${apiBaseUrl}/admin/keys`);
        const keys = await response.json();
        
        const tbody = document.getElementById('keysTableBody');
        
        if (!keys || keys.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="loading">Không có dữ liệu</td></tr>';
            return;
        }
        
        let html = '';
        keys.forEach(key => {
            const statusClass = getStatusClass(key.status);
            html += `
                <tr>
                    <td><code>${key.keyCode}</code></td>
                    <td>${key.packageType}</td>
                    <td>${key.duration}h</td>
                    <td>${formatCurrency(key.price)}</td>
                    <td><span class="status-badge ${statusClass}">${getStatusText(key.status)}</span></td>
                    <td>${formatDate(key.createdDate)}</td>
                    <td>${key.buyer || '-'}</td>
                    <td>
                        <button class="btn-icon" onclick="viewKey('${key.keyCode}')" title="Xem chi tiết">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button class="btn-icon" onclick="deleteKey('${key.keyCode}')" title="Xóa thường" style="color: #f44336;">
                            <i class="fas fa-trash"></i>
                        </button>
                        <button class="btn-icon" onclick="forceDeleteKeyNow('${key.keyCode}')" title="Force xóa (kể cả lịch sử)" style="color: #ff9800;">
                            <i class="fas fa-bomb"></i>
                        </button>
                    </td>
                </tr>
            `;
        });
        
        tbody.innerHTML = html;
    } catch (error) {
        console.error('Error loading keys:', error);
        document.getElementById('keysTableBody').innerHTML = 
            '<tr><td colspan="8" class="loading">Lỗi tải dữ liệu</td></tr>';
    }
}

// Load accounts data - ĐÃ THÊM NÚT FORCE DELETE
async function loadAccounts() {
    try {
        console.log('Đang tải danh sách tài khoản...');
        const response = await fetch(`${apiBaseUrl}/admin/riot-accounts`);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const accounts = await response.json();
        console.log('Đã nhận dữ liệu:', accounts);
        
        const tbody = document.getElementById('accountsTableBody');
        
        if (!accounts || accounts.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="loading">Không có dữ liệu</td></tr>';
            return;
        }
        
        let html = '';
        accounts.forEach(account => {
            const statusClass = account.isAvailable ? 'success' : 'warning';
            const statusText = account.isAvailable ? 'Trống' : 'Đang dùng';
            
            html += `
                <tr>
                    <td>${account.accountId}</td>
                    <td>${account.riotUsername || 'N/A'}</td>
                    <td>${account.region || 'VN'}</td>
                    <td><span class="status-badge ${statusClass}">${statusText}</span></td>
                    <td>${account.currentUser || '-'}</td>
                    <td>${account.lastUsedDate ? formatDateTime(account.lastUsedDate) : '-'}</td>
                    <td>${account.totalUsed || 0}</td>
                    <td>
                        <button class="btn-icon" onclick="editAccount(${account.accountId})" title="Chỉnh sửa">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn-icon" onclick="deleteAccount(${account.accountId})" title="Xóa thường" style="color: #f44336;">
                            <i class="fas fa-trash"></i>
                        </button>
                        <button class="btn-icon" onclick="forceDeleteAccountNow(${account.accountId})" title="Force xóa (kể cả lịch sử)" style="color: #ff9800;">
                            <i class="fas fa-bomb"></i>
                        </button>
                    </td>
                </tr>
            `;
        });
        
        tbody.innerHTML = html;
    } catch (error) {
        console.error('Lỗi khi tải tài khoản:', error);
        document.getElementById('accountsTableBody').innerHTML = 
            `<tr><td colspan="8" class="loading">Lỗi tải dữ liệu: ${error.message}</td></tr>`;
        showToast('❌ Không thể tải danh sách tài khoản', 'error');
    }
}

// Load sessions data
async function loadSessions() {
    try {
        const response = await fetch(`${apiBaseUrl}/admin/sessions`);
        const sessions = await response.json();
        
        const tbody = document.getElementById('sessionsTableBody');
        
        if (!sessions || sessions.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="loading">Không có dữ liệu</td></tr>';
            return;
        }
        
        let html = '';
        sessions.forEach(session => {
            const statusClass = session.status === 'Active' ? 'success' : 'secondary';
            html += `
                <tr>
                    <td>${session.sessionId}</td>
                    <td>${session.userId}</td>
                    <td>${session.accountUsername || 'N/A'}</td>
                    <td>${formatDateTime(session.startTime)}</td>
                    <td>${session.duration ? session.duration + ' phút' : '-'}</td>
                    <td><span class="status-badge ${statusClass}">${session.status}</span></td>
                    <td>
                        ${session.status === 'Active' ? 
                            `<button class="btn-icon" onclick="endSession(${session.sessionId})" title="Kết thúc session">
                                <i class="fas fa-stop"></i>
                            </button>` : 
                            '-'}
                    </td>
                </tr>
            `;
        });
        
        tbody.innerHTML = html;
    } catch (error) {
        console.error('Error loading sessions:', error);
        document.getElementById('sessionsTableBody').innerHTML = 
            '<tr><td colspan="7" class="loading">Lỗi tải dữ liệu</td></tr>';
    }
}

// Load violations data
async function loadViolations() {
    try {
        const response = await fetch(`${apiBaseUrl}/admin/violations`);
        const violations = await response.json();
        
        const tbody = document.getElementById('violationsTableBody');
        
        if (!violations || violations.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="loading">Không có dữ liệu</td></tr>';
            return;
        }
        
        let html = '';
        violations.forEach(violation => {
            const actionClass = violation.action === 'Ban' ? 'danger' : 'warning';
            html += `
                <tr>
                    <td>${formatDateTime(violation.detectedTime)}</td>
                    <td>${violation.userId}</td>
                    <td>${violation.violationType}</td>
                    <td>${violation.details || '-'}</td>
                    <td><span class="status-badge ${actionClass}">${violation.action}</span></td>
                </tr>
            `;
        });
        
        tbody.innerHTML = html;
    } catch (error) {
        console.error('Error loading violations:', error);
        document.getElementById('violationsTableBody').innerHTML = 
            '<tr><td colspan="5" class="loading">Lỗi tải dữ liệu</td></tr>';
    }
}

// Load revenue data
async function loadRevenueData() {
    try {
        const response = await fetch(`${apiBaseUrl}/admin/revenue`);
        const data = await response.json();
        
        document.getElementById('revenueToday').textContent = formatCurrency(data.today || 0);
        document.getElementById('revenueWeek').textContent = formatCurrency(data.week || 0);
        document.getElementById('revenueMonth').textContent = formatCurrency(data.month || 0);
        document.getElementById('revenueTotal').textContent = formatCurrency(data.total || 0);
        
        updateRevenueDetailChart(data.daily || []);
    } catch (error) {
        console.error('Error loading revenue data:', error);
    }
}

// Update revenue detail chart
function updateRevenueDetailChart(dailyData) {
    const ctx = document.getElementById('revenueDetailChart').getContext('2d');
    
    if (window.revenueChart) {
        window.revenueChart.destroy();
    }
    
    window.revenueChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: dailyData.map(d => d.date),
            datasets: [{
                label: 'Doanh thu (VNĐ)',
                data: dailyData.map(d => d.amount),
                backgroundColor: '#ff4655',
                borderRadius: 5
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(255, 255, 255, 0.1)'
                    },
                    ticks: {
                        color: '#888',
                        callback: function(value) {
                            return formatCurrency(value);
                        }
                    }
                },
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        color: '#888'
                    }
                }
            }
        }
    });
}

// Update charts
function updateCharts(data) {
    if (window.revenueChart) {
        window.revenueChart.data.datasets[0].data = data.revenueByDay || [0, 0, 0, 0, 0, 0, 0];
        window.revenueChart.update();
    }
    
    if (window.gameChart) {
        window.gameChart.data.datasets[0].data = [data.valorantSessions || 100];
        window.gameChart.update();
    }
}

// Filter keys
function filterKeys() {
    const search = document.getElementById('keySearch').value;
    const status = document.getElementById('keyStatusFilter').value;
    console.log('Filtering keys:', { search, status });
    loadKeys();
}

// Show generate key modal
function showGenerateKeyModal() {
    document.getElementById('generateKeyModal').style.display = 'flex';
}

// Show add account modal
function showAddAccountModal() {
    document.getElementById('addAccountModal').style.display = 'flex';
}

// Close modal
function closeModal(modalId) {
    document.getElementById(modalId).style.display = 'none';
}

// Generate keys
async function generateKeys() {
    const packageType = document.getElementById('keyPackageType').value;
    const quantity = parseInt(document.getElementById('keyQuantity').value);
    const price = parseFloat(document.getElementById('keyPrice').value);
    const duration = document.getElementById('keyDuration').value;
    
    try {
        const response = await fetch(`${apiBaseUrl}/admin/generate-valorant-keys`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
            },
            body: JSON.stringify({
                packageType,
                quantity,
                price,
                duration: parseInt(duration)
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            alert(`✅ Đã tạo ${result.keys.length} key thành công!`);
            closeModal('generateKeyModal');
            loadKeys();
        } else {
            alert('❌ Lỗi: ' + result.message);
        }
    } catch (error) {
        alert('❌ Lỗi kết nối server: ' + error.message);
    }
}

// Add Riot account
async function addRiotAccount() {
    const username = document.getElementById('riotUsername').value;
    const password = document.getElementById('riotPassword').value;
    const region = document.getElementById('riotRegion').value;
    
    if (!username || !password) {
        alert('❌ Vui lòng nhập đầy đủ thông tin!');
        return;
    }
    
    try {
        const response = await fetch(`${apiBaseUrl}/admin/add-riot-account`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
            },
            body: JSON.stringify({
                username,
                password,
                region
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            alert('✅ Thêm tài khoản thành công!');
            closeModal('addAccountModal');
            loadAccounts();
            
            document.getElementById('riotUsername').value = '';
            document.getElementById('riotPassword').value = '';
        } else {
            alert('❌ Lỗi: ' + result.message);
        }
    } catch (error) {
        alert('❌ Lỗi kết nối server: ' + error.message);
    }
}

// View key details
function viewKey(keyCode) {
    alert('Xem chi tiết key: ' + keyCode);
}

// ==================== DELETE KEY FUNCTIONS ====================

// Xóa key thường (có confirm)
async function deleteKey(keyCode) {
    if (!confirm(`Bạn có chắc chắn muốn xóa key ${keyCode}?`)) {
        return;
    }
    
    try {
        console.log(`Đang xóa key: ${keyCode}`);
        
        const response = await fetch(`${apiBaseUrl}/admin/keys/${keyCode}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
            }
        });
        
        const responseText = await response.text();
        console.log('Response text:', responseText);
        
        let result;
        try {
            result = JSON.parse(responseText);
        } catch (e) {
            console.error('Không thể parse JSON:', responseText);
            showToast('❌ Lỗi server: ' + responseText, 'error');
            return;
        }
        
        if (response.ok) {
            showToast('✅ ' + (result.message || 'Xóa key thành công!'), 'success');
            loadKeys();
        } else {
            showToast('❌ ' + (result.message || 'Không thể xóa key'), 'error');
        }
    } catch (error) {
        console.error('Lỗi khi xóa key:', error);
        showToast('❌ Lỗi kết nối server: ' + error.message, 'error');
    }
}

// FORCE DELETE KEY - XÓA NGAY LẬP TỨC (CÓ CONFIRM)
async function forceDeleteKeyNow(keyCode) {
    if (!confirm(`⚠️ BẠN CÓ CHẮC MUỐN FORCE XÓA key ${keyCode}?\n\nThao tác này sẽ xóa HOÀN TOÀN key và tất cả lịch sử liên quan.`)) {
        return;
    }
    
    try {
        console.log(`Đang force xóa key: ${keyCode}`);
        
        const response = await fetch(`${apiBaseUrl}/admin/keys/force/${keyCode}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
            }
        });
        
        const responseText = await response.text();
        console.log('Response text:', responseText);
        
        let result;
        try {
            result = JSON.parse(responseText);
        } catch (e) {
            console.error('Không thể parse JSON:', responseText);
            showToast('❌ Lỗi server: ' + responseText, 'error');
            return;
        }
        
        if (response.ok) {
            showToast('✅ ' + (result.message || 'Force xóa key thành công!'), 'success');
            loadKeys();
        } else {
            showToast('❌ ' + (result.message || 'Không thể force xóa key'), 'error');
        }
    } catch (error) {
        console.error('Lỗi khi force xóa key:', error);
        showToast('❌ Lỗi kết nối server: ' + error.message, 'error');
    }
}

// ==================== DELETE ACCOUNT FUNCTIONS ====================

// Xóa tài khoản thường (có confirm)
async function deleteAccount(accountId) {
    if (!confirm(`Bạn có chắc chắn muốn xóa tài khoản ID ${accountId}?`)) {
        return;
    }
    
    try {
        console.log(`Đang xóa tài khoản ID: ${accountId}`);
        
        const response = await fetch(`${apiBaseUrl}/admin/riot-accounts/${accountId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
            }
        });
        
        const responseText = await response.text();
        console.log('Response text:', responseText);
        
        let result;
        try {
            result = JSON.parse(responseText);
        } catch (e) {
            console.error('Không thể parse JSON:', responseText);
            showToast('❌ Lỗi server: ' + responseText, 'error');
            return;
        }
        
        if (response.ok) {
            showToast('✅ ' + (result.message || 'Xóa tài khoản thành công!'), 'success');
            loadAccounts();
        } else {
            showToast('❌ ' + (result.message || 'Không thể xóa tài khoản'), 'error');
        }
    } catch (error) {
        console.error('Lỗi khi xóa tài khoản:', error);
        showToast('❌ Lỗi kết nối server: ' + error.message, 'error');
    }
}

// FORCE DELETE ACCOUNT - XÓA NGAY LẬP TỨC (CÓ CONFIRM)
async function forceDeleteAccountNow(accountId) {
    if (!confirm(`⚠️ BẠN CÓ CHẮC MUỐN FORCE XÓA tài khoản ID ${accountId}?\n\nThao tác này sẽ xóa HOÀN TOÀN tài khoản và tất cả lịch sử liên quan.`)) {
        return;
    }
    
    try {
        console.log(`Đang force xóa tài khoản ID: ${accountId}`);
        
        const response = await fetch(`${apiBaseUrl}/admin/riot-accounts/force/${accountId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
            }
        });
        
        const responseText = await response.text();
        console.log('Response text:', responseText);
        
        let result;
        try {
            result = JSON.parse(responseText);
        } catch (e) {
            console.error('Không thể parse JSON:', responseText);
            showToast('❌ Lỗi server: ' + responseText, 'error');
            return;
        }
        
        if (response.ok) {
            showToast('✅ ' + (result.message || 'Force xóa tài khoản thành công!'), 'success');
            loadAccounts();
        } else {
            showToast('❌ ' + (result.message || 'Không thể force xóa tài khoản'), 'error');
        }
    } catch (error) {
        console.error('Lỗi khi force xóa tài khoản:', error);
        showToast('❌ Lỗi kết nối server: ' + error.message, 'error');
    }
}

// Edit account
function editAccount(accountId) {
    alert('Chỉnh sửa tài khoản: ' + accountId);
}

// End session
async function endSession(sessionId) {
    if (!confirm(`Bạn có chắc muốn kết thúc session ${sessionId}?`)) {
        return;
    }
    
    try {
        const response = await fetch(`${apiBaseUrl}/admin/sessions/${sessionId}/end`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
            }
        });
        
        const result = await response.json();
        
        if (response.ok) {
            showToast('✅ Đã kết thúc session!', 'success');
            loadSessions();
        } else {
            showToast('❌ ' + (result.message || 'Không thể kết thúc session'), 'error');
        }
    } catch (error) {
        showToast('❌ Lỗi kết nối server: ' + error.message, 'error');
    }
}

// Backup database
function backupDatabase() {
    showToast('⏳ Chức năng đang phát triển', 'info');
}

// Restore database
function restoreDatabase() {
    showToast('⏳ Chức năng đang phát triển', 'info');
}

// Save settings
function saveSettings() {
    const settings = {
        apiUrl: document.getElementById('apiUrl').value,
        jwtSecret: document.getElementById('jwtSecret').value,
        sessionTimeout: document.getElementById('sessionTimeout').value,
        adminEmail: document.getElementById('adminEmail').value,
        smtpServer: document.getElementById('smtpServer').value
    };
    
    localStorage.setItem('adminSettings', JSON.stringify(settings));
    showToast('✅ Đã lưu cài đặt!', 'success');
}

// Logout
function logout() {
    localStorage.removeItem('adminToken');
    window.location.href = 'index.html';
}

// Show toast notification
function showToast(message, type = 'info') {
    let toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toastContainer';
        toastContainer.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
        `;
        document.body.appendChild(toastContainer);
    }
    
    const toast = document.createElement('div');
    
    let bgColor = '#2196f3';
    if (type === 'success') bgColor = '#4caf50';
    if (type === 'error') bgColor = '#f44336';
    if (type === 'warning') bgColor = '#ff9800';
    
    toast.style.cssText = `
        background: ${bgColor};
        color: white;
        padding: 12px 24px;
        border-radius: 4px;
        margin-bottom: 10px;
        box-shadow: 0 2px 5px rgba(0,0,0,0.2);
        animation: slideIn 0.3s ease;
        cursor: pointer;
        min-width: 250px;
        font-weight: 500;
        z-index: 10000;
    `;
    toast.textContent = message;
    
    // Thêm animation
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(100%);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);
    
    toastContainer.appendChild(toast);
    
    const timeoutId = setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 300);
    }, 3000);
    
    toast.addEventListener('click', () => {
        clearTimeout(timeoutId);
        toast.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 300);
    });
}

// Utility functions
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { 
        style: 'currency', 
        currency: 'VND' 
    }).format(amount);
}

function formatDate(dateStr) {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('vi-VN');
}

function formatDateTime(dateStr) {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleString('vi-VN');
}

function getStatusClass(status) {
    switch(status) {
        case 'Available':
            return 'success';
        case 'Sold':
            return 'warning';
        case 'Expired':
            return 'danger';
        default:
            return 'secondary';
    }
}

function getStatusText(status) {
    switch(status) {
        case 'Available':
            return 'Còn hạn';
        case 'Sold':
            return 'Đã bán';
        case 'Expired':
            return 'Hết hạn';
        default:
            return status;
    }
}

// Close modal when clicking outside
window.onclick = function(event) {
    if (event.target.classList.contains('modal')) {
        event.target.style.display = 'none';
    }
}