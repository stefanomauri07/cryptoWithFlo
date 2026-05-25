let authToken = localStorage.getItem('crypto_token') || null;
let currentUser = JSON.parse(localStorage.getItem('crypto_user') || 'null');

function getAuthHeaders() {
    return authToken
        ? { 'Authorization': 'Bearer ' + authToken, 'Content-Type': 'application/json' }
        : { 'Content-Type': 'application/json' };
}

function isAuthenticated() {
    return !!authToken && !!currentUser;
}

function getCurrentUser() {
    return currentUser;
}

function requireAuth() {
    if (!isAuthenticated()) {
        window.location.href = 'login.html';
        return false;
    }
    return true;
}

async function api(url, options = {}) {
    const headers = { ...getAuthHeaders(), ...options.headers };

    if (options.body && typeof options.body === 'object' && !(options.body instanceof FormData)) {
        options.body = JSON.stringify(options.body);
    }

    const res = await fetch(API_BASE + url, { ...options, headers });

    if (res.status === 401) {
        logout();
        window.location.href = 'login.html';
        throw new Error('Unauthorized');
    }

    return res;
}

async function login(email, password) {
    const res = await fetch(API_BASE + '/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
    });

    if (!res.ok) {
        const err = await res.json().catch(() => ({ error: 'Login failed' }));
        throw new Error(err.error || 'Login failed');
    }

    const data = await res.json();
    authToken = data.token;
    currentUser = data.user;
    localStorage.setItem('crypto_token', authToken);
    localStorage.setItem('crypto_user', JSON.stringify(currentUser));
    return data;
}

async function register(email, password, name) {
    const res = await fetch(API_BASE + '/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password, name })
    });
    if (!res.ok) {
        const err = await res.json().catch(() => ({ error: 'Registration failed' }));
        throw new Error(err.error || 'Registration failed');
    }
    return { email };
}

async function verifyOtp(email, code) {
    const res = await fetch(API_BASE + '/api/auth/verify', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, code })
    });
    if (!res.ok) {
        const err = await res.json().catch(() => ({ error: 'OTP verification failed' }));
        throw new Error(err.error || 'OTP verification failed');
    }
    const data = await res.json();
    authToken = data.token;
    currentUser = data.user;
    localStorage.setItem('crypto_token', authToken);
    localStorage.setItem('crypto_user', JSON.stringify(currentUser));
    return data;
}

async function forgotPassword(email) {
    const res = await fetch(API_BASE + '/api/auth/forgot-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email })
    });
    if (!res.ok) throw new Error('Request failed');
}

async function resetPassword(email, code, newPassword) {
    const res = await fetch(API_BASE + '/api/auth/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, code, newPassword })
    });
    if (!res.ok) {
        const err = await res.json().catch(() => ({ error: 'Reset failed' }));
        throw new Error(err.error || 'Reset failed');
    }
}

function logout() {
    authToken = null;
    currentUser = null;
    localStorage.removeItem('crypto_token');
    localStorage.removeItem('crypto_user');
    window.location.href = 'login.html';
}
