// main.js - Shared utilities, auth checking, and dynamic header renderer

window.currentUser = null;
window.siteSettings = {};

document.addEventListener('DOMContentLoaded', async () => {
    await loadSiteSettings();
    await checkAuth();
    updateNavigation();
    initModals();
});

async function checkAuth() {
    try {
        const response = await fetch('/api/auth/status');
        if (response.ok) {
            window.currentUser = await response.json();
        } else {
            window.currentUser = null;
        }
    } catch (e) {
        console.error("Auth check failed", e);
        window.currentUser = null;
    }
}

function updateNavigation() {
    const authNav = document.getElementById('authNav');
    if (!authNav) return;

    if (window.currentUser) {
        const isBuyer = window.currentUser.role === 'Buyer' || window.currentUser.role === 'Admin';
        let customerButtons = '';
        if (isBuyer) {
            customerButtons = `
                <a href="/cart.html" class="btn btn-secondary" style="display: inline-flex; align-items: center; gap: 6px;">
                    🛒 Cart <span id="headerCartCount" class="badge" style="display:none; padding: 2px 6px; border-radius:10px; background-color: var(--color-primary); color: white; font-size:11px; font-weight:700;">0</span>
                </a>
                <a href="/wishlist.html" class="btn btn-secondary" style="display: inline-flex; align-items: center; gap: 6px;">
                    ❤️ Wishlist <span id="headerWishCount" class="badge" style="display:none; padding: 2px 6px; border-radius:10px; background-color: var(--color-danger); color: white; font-size:11px; font-weight:700;">0</span>
                </a>
                <a href="/compare.html" class="btn btn-secondary" style="display: inline-flex; align-items: center; gap: 6px;">
                    🔄 Compare <span id="headerCompareCount" class="badge" style="display:none; padding: 2px 6px; border-radius:10px; background-color: var(--color-accent); color: white; font-size:11px; font-weight:700;">0</span>
                </a>
            `;
        }

        authNav.innerHTML = `
            <span style="color: var(--text-secondary); font-size: 14px; margin-right: 12px; display: inline-flex; align-items: center; vertical-align: middle;">
                Hello, <strong style="color: var(--color-primary); margin-left: 4px; margin-right: 4px;">${window.currentUser.username}</strong> 
                (${window.currentUser.role === 'Buyer' ? 'Customer' : window.currentUser.role})
                ${window.currentUser.role === 'Seller' && window.currentUser.isVerified ? '<span class="badge badge-verified" style="padding: 2px 6px; font-size: 10px; margin-left: 5px;">✓ Verified</span>' : ''}
            </span>
            ${customerButtons}
            <a href="/dashboard.html" class="btn btn-secondary">Dashboard</a>
            <button onclick="handleLogout()" class="btn btn-danger">Logout</button>
        `;
        if (isBuyer) {
            fetchHeaderStats();
        }
    } else {
        authNav.innerHTML = `
            <button onclick="openModal('loginModal')" class="btn btn-secondary" style="display: inline-flex; align-items: center; gap: 6px;">
                🛒 Cart
            </button>
            <button onclick="openModal('loginModal')" class="btn btn-secondary" style="display: inline-flex; align-items: center; gap: 6px;">
                ❤️ Wishlist
            </button>
            <button onclick="openModal('loginModal')" class="btn btn-secondary" style="display: inline-flex; align-items: center; gap: 6px;">
                🔄 Compare
            </button>
            <button onclick="openModal('loginModal')" class="btn btn-secondary">Log In</button>
            <button onclick="openModal('registerModal')" class="btn btn-primary">Register</button>
        `;
    }
}

async function fetchHeaderStats() {
    if (!window.currentUser) return;
    try {
        const response = await fetch('/api/customer/stats');
        if (response.ok) {
            const stats = await response.json();
            
            const cartBadge = document.getElementById('headerCartCount');
            if (cartBadge) {
                if (stats.cartCount > 0) {
                    cartBadge.textContent = stats.cartCount;
                    cartBadge.style.display = 'inline-block';
                } else {
                    cartBadge.style.display = 'none';
                }
            }

            const wishBadge = document.getElementById('headerWishCount');
            if (wishBadge) {
                if (stats.wishlistCount > 0) {
                    wishBadge.textContent = stats.wishlistCount;
                    wishBadge.style.display = 'inline-block';
                } else {
                    wishBadge.style.display = 'none';
                }
            }

            const compareBadge = document.getElementById('headerCompareCount');
            if (compareBadge) {
                if (stats.compareCount > 0) {
                    compareBadge.textContent = stats.compareCount;
                    compareBadge.style.display = 'inline-block';
                } else {
                    compareBadge.style.display = 'none';
                }
            }
        }
    } catch(e) {
        console.error("Failed to fetch header stats", e);
    }
}

window.updateHeaderStats = fetchHeaderStats;

// Toast Notifications
function showToast(message, isError = false) {
    let toast = document.getElementById('toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'toast';
        toast.className = 'toast';
        document.body.appendChild(toast);
    }
    
    toast.innerHTML = `
        <span style="font-size: 18px">${isError ? '❌' : '⚡'}</span>
        <span>${message}</span>
    `;
    
    if (isError) {
        toast.classList.add('toast-error');
    } else {
        toast.classList.remove('toast-error');
    }
    
    toast.classList.add('show');
    setTimeout(() => {
        toast.classList.remove('show');
    }, 3500);
}

// Modal handling
function openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'flex';
    }
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
    }
}

function initModals() {
    window.addEventListener('click', (e) => {
        if (e.target.classList.contains('modal-overlay')) {
            e.target.style.display = 'none';
        }
    });
    
    window.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            const overlays = document.querySelectorAll('.modal-overlay');
            overlays.forEach(overlay => {
                overlay.style.display = 'none';
            });
        }
    });
}

async function loadSiteSettings() {
    try {
        const response = await fetch('/api/settings');
        if (response.ok) {
            window.siteSettings = await response.json();
            applySiteSettings();
        }
    } catch (e) {
        console.error("Failed to load site settings", e);
    }
}

function applySiteSettings() {
    const brandName = window.siteSettings.site_title || "E-trail Global";
    document.title = document.title.replace("EtrailGlobal", brandName);

    const logos = document.querySelectorAll('.logo');
    logos.forEach(el => {
        const svg = el.querySelector('svg');
        if (svg) {
            el.innerHTML = '';
            el.appendChild(svg);
            el.appendChild(document.createTextNode(' ' + brandName));
        } else {
            el.textContent = brandName;
        }
    });

    const addressEl = document.getElementById('footerAddress');
    if (addressEl) addressEl.textContent = window.siteSettings.contact_address || "";

    const phoneEl = document.getElementById('footerPhone');
    if (phoneEl) phoneEl.textContent = window.siteSettings.contact_phone || "";

    const emailEl = document.getElementById('footerEmail');
    if (emailEl) emailEl.textContent = window.siteSettings.contact_email || "";
}

