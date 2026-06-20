// auth.js - Handles registrations and login AJAX submissions

document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handleLoginSubmit);
    }

    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', handleRegisterSubmit);
    }

    const sellerLoginForm = document.getElementById('sellerLoginForm');
    if (sellerLoginForm) {
        sellerLoginForm.addEventListener('submit', handleSellerLoginSubmit);
    }

    const sellerRegisterForm = document.getElementById('sellerRegisterForm');
    if (sellerRegisterForm) {
        sellerRegisterForm.addEventListener('submit', handleSellerRegisterSubmit);
    }

    // Toggle registration fields based on selected role (if roleSelect exists)
    const roleSelect = document.getElementById('regRole');
    if (roleSelect) {
        roleSelect.addEventListener('change', (e) => {
            const companyGroup = document.getElementById('regCompanyGroup');
            const businessGroup = document.getElementById('regBusinessGroup');
            
            if (e.target.value === '1') { // Seller
                companyGroup.style.display = 'block';
                businessGroup.style.display = 'block';
                document.getElementById('regCompanyName').setAttribute('required', 'required');
            } else { // Buyer
                companyGroup.style.display = 'block';
                businessGroup.style.display = 'none';
                document.getElementById('regCompanyName').removeAttribute('required');
            }
        });
    }
});

async function handleLoginSubmit(e) {
    e.preventDefault();
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password, loginType: "Customer" })
        });

        const data = await response.json();

        if (response.ok) {
            showToast("Successfully logged in!");
            window.currentUser = data;
            updateNavigation();
            closeModal('loginModal');
            
            // Redirect after successful login to dashboard or admin panel
            setTimeout(() => {
                if (data.role === 'Admin') {
                    window.location.href = '/admin.html';
                } else {
                    window.location.href = '/dashboard.html';
                }
            }, 1000);
        } else {
            showToast(data.message || "Login failed", true);
        }
    } catch (err) {
        showToast("An error occurred during login", true);
        console.error(err);
    }
}

async function handleRegisterSubmit(e) {
    e.preventDefault();
    const username = document.getElementById('regUsername').value;
    const email = document.getElementById('regEmail').value;
    const password = document.getElementById('regPassword').value;
    const roleSelect = document.getElementById('regRole');
    const roleVal = roleSelect ? parseInt(roleSelect.value) : 0; // default to Buyer (0)
    const companyName = document.getElementById('regCompanyName') ? document.getElementById('regCompanyName').value : '';
    const businessType = document.getElementById('regBusinessType') ? document.getElementById('regBusinessType').value : '';
    const contactInfo = document.getElementById('regContactInfo').value;

    try {
        const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username,
                email,
                password,
                role: roleVal,
                companyName,
                businessType,
                contactInfo
            })
        });

        const data = await response.json();

        if (response.ok) {
            showToast("Registration successful! You can now log in.");
            closeModal('registerModal');
            document.getElementById('registerForm').reset();
            openModal('loginModal');
        } else {
            showToast(data.message || "Registration failed", true);
        }
    } catch (err) {
        showToast("An error occurred during registration", true);
        console.error(err);
    }
}

async function handleSellerLoginSubmit(e) {
    e.preventDefault();
    const email = document.getElementById('sellerLoginEmail').value;
    const password = document.getElementById('sellerLoginPassword').value;

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password, loginType: "Seller" })
        });

        const data = await response.json();

        if (response.ok) {
            if (data.role !== 'Seller' && data.role !== 'Admin') {
                showToast("Access Denied: This login is only for Sellers.", true);
                return;
            }
            showToast("Successfully logged into Seller Panel!");
            window.currentUser = data;
            updateNavigation();
            closeModal('sellerLoginModal');
            
            setTimeout(() => {
                window.location.href = '/dashboard.html';
            }, 1000);
        } else {
            showToast(data.message || "Login failed", true);
        }
    } catch (err) {
        showToast("An error occurred during login", true);
        console.error(err);
    }
}

async function handleSellerRegisterSubmit(e) {
    e.preventDefault();
    const username = document.getElementById('sellerRegUsername').value;
    const email = document.getElementById('sellerRegEmail').value;
    const password = document.getElementById('sellerRegPassword').value;
    const roleVal = 1; // Seller
    const companyName = document.getElementById('sellerRegCompanyName').value;
    const contactInfo = document.getElementById('sellerRegContactInfo').value;
    const whatsAppNumber = document.getElementById('sellerRegWhatsApp').value;
    const alternateEmail = document.getElementById('sellerRegGmail').value;
    const taxNumber = document.getElementById('sellerRegTax').value;
    const companyAddress = document.getElementById('sellerRegCompanyAddress').value;
    const yearEstablished = document.getElementById('sellerRegYearEstablished').value;
    const websiteUrl = document.getElementById('sellerRegWebsite').value;

    try {
        const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username,
                email,
                password,
                role: roleVal,
                companyName,
                contactInfo,
                whatsAppNumber,
                alternateEmail,
                taxNumber,
                companyAddress,
                yearEstablished,
                websiteUrl
            })
        });

        const data = await response.json();

        if (response.ok) {
            showToast("Seller application submitted! Please log in.");
            closeModal('sellerRegisterModal');
            document.getElementById('sellerRegisterForm').reset();
            openModal('sellerLoginModal');
        } else {
            showToast(data.message || "Registration failed", true);
        }
    } catch (err) {
        showToast("An error occurred during registration", true);
        console.error(err);
    }
}
