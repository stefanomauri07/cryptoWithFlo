function initLayout(activePage) {
    var pageToNavIdx = { dashboard: 0, markets: 1, portfolio: 2, alerts: 3, news: 4, compare: 5 };
    var pageToSideIdx = { dashboard: 0, markets: 1, alerts: 2, portfolio: 3, news: 4, compare: 5 };

    var navLinks = [
        { label: 'Dashboard', href: 'index.html' },
        { label: 'Markets', href: 'market-explorer.html' },
        { label: 'Portfolio', href: 'portfolio.html' },
        { label: 'Alerts', href: 'alert-history.html' },
        { label: 'News', href: 'news.html' },
        { label: 'Compare', href: 'compare.html' }
    ];

    var sidebarItems = [
        { label: 'Overview', href: 'index.html', icon: 'dashboard' },
        { label: 'Assets', href: 'market-explorer.html', icon: 'account_balance_wallet' },
        { label: 'History', href: 'alert-history.html', icon: 'history' },
        { label: 'Portfolio', href: 'portfolio.html', icon: 'pie_chart' },
        { label: 'News', href: 'news.html', icon: 'newspaper' },
        { label: 'Compare', href: 'compare.html', icon: 'compare_arrows' }
    ];

    var activeNavIdx = pageToNavIdx[activePage] !== undefined ? pageToNavIdx[activePage] : -1;
    var activeSideIdx = pageToSideIdx[activePage] !== undefined ? pageToSideIdx[activePage] : -1;

    var navLinksHtml = '';
    for (var i = 0; i < navLinks.length; i++) {
        var link = navLinks[i];
        if (i === activeNavIdx) {
            navLinksHtml += '<a href="' + link.href + '" class="text-primary-fixed-dim border-b-2 border-primary-fixed-dim pb-1 font-label-md text-label-md">' + link.label + '</a>';
        } else {
            navLinksHtml += '<a href="' + link.href + '" class="text-on-surface-variant hover:text-on-surface transition-colors font-label-md text-label-md">' + link.label + '</a>';
        }
    }

    var sidebarLinksHtml = '';
    for (var j = 0; j < sidebarItems.length; j++) {
        var item = sidebarItems[j];
        var baseClass = 'sidebar-link flex items-center gap-md px-lg py-md rounded-lg text-body-md font-body-md group transition-transform hover:translate-x-1';
        var activeClass = 'bg-primary-container/10 text-primary-fixed-dim border-r-4 border-primary-fixed-dim';
        var inactiveClass = 'text-on-surface-variant hover:text-on-surface hover:bg-surface-container-high/50';
        var isActive = j === activeSideIdx;
        var itemClass = baseClass + ' ' + (isActive ? activeClass : inactiveClass);

        if (item.href) {
            sidebarLinksHtml += '<a href="' + item.href + '" class="' + itemClass + '">';
        } else {
            sidebarLinksHtml += '<div class="' + itemClass + ' cursor-default">';
        }
        sidebarLinksHtml += '<span class="material-symbols-outlined">' + item.icon + '</span>';
        sidebarLinksHtml += '<span>' + item.label + '</span>';
        if (item.href) {
            sidebarLinksHtml += '</a>';
        } else {
            sidebarLinksHtml += '</div>';
        }
    }

    var navbarHtml = '<nav class="fixed top-0 w-full z-50 flex justify-between items-center px-margin-desktop h-16 bg-surface/80 backdrop-blur-xl border-b border-outline-variant/30 shadow-sm">' +
        '<div class="flex items-center gap-xl">' +
        '<span class="font-headline-md text-headline-md font-bold text-primary-fixed tracking-tight">CryptoTracker</span>' +
        '<div class="hidden md:flex gap-lg">' + navLinksHtml + '</div>' +
        '</div>' +
        '<div class="flex items-center gap-md">' +
        '<div id="health-status" class="flex items-center gap-sm">' +
        '<span class="status-dot-connecting" id="status-dot"></span>' +
        '<span id="status-text" class="text-body-sm font-body-sm text-on-surface-variant">Connecting...</span>' +
        '</div>' +
        '<span class="text-on-surface-variant/40 hidden md:inline">|</span>' +
        '<span id="cache-age" class="text-body-sm font-body-sm text-on-surface-variant hidden md:inline">---</span>' +
        '<span class="text-on-surface-variant/40">|</span>' +
        '<div id="user-menu" class="flex items-center gap-md">' +
        '<span id="user-name" class="text-body-sm font-body-sm text-on-surface"></span>' +
        '<span id="user-role" class="text-label-md font-label-md px-2 py-0.5 rounded" style="background:rgba(0,255,136,0.15);color:#00ff88"></span>' +
        '<button onclick="logout()" class="text-on-surface-variant hover:text-error transition-colors font-label-md text-label-md">Logout</button>' +
        '<a href="upgrade.html" id="navbar-upgrade-btn" class="hidden text-primary-fixed-dim font-label-md text-label-md px-3 py-1 border border-primary-container/30 rounded-lg hover:bg-primary-container/10 transition-colors">Upgrade</a>' +
        '</div>' +
        '</div>' +
        '</nav>';

    var sidebarHtml = '<aside class="sidebar hidden lg:flex fixed left-0 top-16 bottom-0 bg-surface-container-lowest border-r border-outline-variant/20 flex-col z-40">' +
        '<div class="p-lg border-b border-outline-variant/10">' +
        '<div class="flex items-center gap-sm mb-2">' +
        '<span class="status-dot-connected"></span>' +
        '<span class="text-label-md font-label-md text-primary-fixed-dim uppercase tracking-wider">Terminal v2.4</span>' +
        '</div>' +
        '<p class="text-body-sm font-body-sm text-on-surface-variant">Live Sync Active</p>' +
        '</div>' +
        '<div class="flex-1 overflow-y-auto py-md px-sm space-y-1">' +
        sidebarLinksHtml +
        '</div>' +
        '<div class="p-lg border-t border-outline-variant/10 space-y-md">' +
        '<a href="upgrade.html" id="sidebar-upgrade-btn" class="hidden w-full bg-primary-container/10 text-primary-fixed-dim hover:bg-primary-container/20 transition-colors font-label-md text-label-md py-3 rounded-lg border border-primary-container/20 text-center block">Upgrade to Pro</a>' +
        '<a href="#" class="block text-center text-on-surface-variant text-body-sm font-body-sm hover:text-on-surface transition-colors">Support</a>' +
        '<button onclick="logout()" class="w-full text-on-surface-variant hover:text-error transition-colors font-label-md text-label-md py-2 rounded-lg hover:bg-error-container/10">Sign Out</button>' +
        '</div>' +
        '</aside>';

    var footerHtml = '<footer class="w-full py-md px-margin-desktop lg:pl-64 flex items-center justify-between bg-surface-dim border-t border-outline-variant/10">' +
        '<span class="font-label-md text-label-md font-bold text-on-surface">CryptoTracker Terminal</span>' +
        '<div class="flex items-center gap-lg">' +
        '<span class="text-on-surface-variant text-body-sm font-body-sm">' + new Date().getFullYear() + ' CryptoTracker. All rights reserved.</span>' +
        '<a href="#" class="text-on-surface-variant text-body-sm font-body-sm hover:text-on-surface transition-colors">Privacy</a>' +
        '<a href="#" class="text-on-surface-variant text-body-sm font-body-sm hover:text-on-surface transition-colors">Terms</a>' +
        '<a href="#" class="text-on-surface-variant text-body-sm font-body-sm hover:text-on-surface transition-colors">API</a>' +
        '<a href="#" class="text-on-surface-variant text-body-sm font-body-sm hover:text-on-surface transition-colors">Status</a>' +
        '</div>' +
        '</footer>';

    var temp = document.createElement('div');
    temp.innerHTML = navbarHtml;
    var navbar = temp.firstElementChild;
    document.body.insertBefore(navbar, document.body.firstChild);

    var temp2 = document.createElement('div');
    temp2.innerHTML = sidebarHtml;
    var sidebar = temp2.firstElementChild;
    document.body.insertBefore(sidebar, document.body.firstChild.nextSibling);

    var temp3 = document.createElement('div');
    temp3.innerHTML = footerHtml;
    var footer = temp3.firstElementChild;
    document.body.appendChild(footer);

    if (typeof updateUserMenu === 'function') updateUserMenu();
    updateUpgradeButtons();
    if (typeof fetchHealth === 'function') {
        fetchHealth();
    }
}

function updateUpgradeButtons() {
    var navUpgrade = document.getElementById('navbar-upgrade-btn');
    var sidebarUpgrade = document.getElementById('sidebar-upgrade-btn');

    if (typeof getCurrentUser === 'function') {
        var user = getCurrentUser();
        if (user && user.role === 'user') {
            if (navUpgrade) navUpgrade.classList.remove('hidden');
            if (sidebarUpgrade) sidebarUpgrade.classList.remove('hidden');
        } else {
            if (navUpgrade) navUpgrade.classList.add('hidden');
            if (sidebarUpgrade) sidebarUpgrade.classList.add('hidden');
        }
    }
}

window.initLayout = initLayout;

// ── AI Financial Advisor Chat Widget ──────────────────────────────────
(function () {
    // Only inject once (layout.js may be loaded on every page nav in SPA-like setups)
    if (document.getElementById('ai-chat-widget')) return;

    /* ── CSS ── */
    var style = document.createElement('style');
    style.textContent = [
        '#ai-chat-widget * { box-sizing: border-box; }',
        '#ai-chat-widget { position: fixed; bottom: 24px; right: 24px; z-index: 9999; font-family: Inter, sans-serif; }',
        '#ai-chat-button { width: 56px; height: 56px; border-radius: 50%; background: linear-gradient(135deg, #00ff88, #00e479); border: none; cursor: pointer; display: flex; align-items: center; justify-content: center; box-shadow: 0 4px 20px rgba(0,255,136,0.3); transition: transform 0.2s; }',
        '#ai-chat-button:hover { transform: scale(1.1); }',
        '#ai-chat-panel { display: none; position: absolute; bottom: 72px; right: 0; width: 380px; height: 520px; background: #0e1322; border: 1px solid #3b4b3d; border-radius: 16px; overflow: hidden; flex-direction: column; box-shadow: 0 8px 40px rgba(0,0,0,0.5); }',
        '#ai-chat-panel.open { display: flex; }',
        '#ai-chat-header { background: #161b2b; padding: 16px; border-bottom: 1px solid #3b4b3d; display: flex; align-items: center; justify-content: space-between; }',
        '#ai-chat-messages { flex: 1; overflow-y: auto; padding: 16px; display: flex; flex-direction: column; gap: 12px; }',
        '.ai-msg { max-width: 85%; padding: 12px 16px; border-radius: 12px; font-size: 14px; line-height: 1.5; }',
        '.ai-msg.user { align-self: flex-end; background: #1a3a2a; color: #dee1f7; }',
        '.ai-msg.bot { align-self: flex-start; background: #25293a; color: #dee1f7; }',
        '#ai-chat-input-area { padding: 12px; border-top: 1px solid #3b4b3d; display: flex; gap: 8px; }',
        '#ai-chat-input { flex: 1; background: #161b2b; border: 1px solid #3b4b3d; border-radius: 8px; padding: 10px 14px; color: #dee1f7; font-size: 14px; outline: none; }',
        '#ai-chat-input:focus { border-color: #00ff88; }',
        '#ai-chat-send { background: #00e479; border: none; border-radius: 8px; padding: 10px 16px; cursor: pointer; color: #003919; font-weight: 600; }',
        '.ai-typing { display: flex; gap: 4px; padding: 12px 16px; }',
        '.ai-typing span { width: 8px; height: 8px; background: #00ff88; border-radius: 50%; animation: bounce 1.4s infinite ease-in-out; }',
        '.ai-typing span:nth-child(2) { animation-delay: 0.2s; }',
        '.ai-typing span:nth-child(3) { animation-delay: 0.4s; }',
        '@keyframes bounce { 0%,80%,100% { transform: translateY(0); } 40% { transform: translateY(-8px); } }',
        '@media (max-width: 480px) { #ai-chat-panel { width: calc(100vw - 48px); height: 60vh; } }'
    ].join('\n');
    document.head.appendChild(style);

    /* ── HTML ── */
    var widget = document.createElement('div');
    widget.id = 'ai-chat-widget';
    widget.innerHTML =
        '<button id="ai-chat-button" title="AI Financial Advisor" aria-label="Open AI chat">' +
            '<span class="material-symbols-outlined" style="color:#003919; font-size:28px;">smart_toy</span>' +
        '</button>' +
        '<div id="ai-chat-panel">' +
            '<div id="ai-chat-header">' +
                '<div style="display:flex;align-items:center;gap:8px;">' +
                    '<span class="material-symbols-outlined" style="color:#00ff88;">smart_toy</span>' +
                    '<span style="color:#dee1f7;font-weight:600;font-size:16px;">AI Financial Advisor</span>' +
                '</div>' +
                '<button id="ai-chat-close" style="background:none;border:none;color:#8b92ad;cursor:pointer;font-size:20px;line-height:1;">&times;</button>' +
            '</div>' +
            '<div id="ai-chat-messages"></div>' +
            '<div id="ai-chat-input-area">' +
                '<input type="text" id="ai-chat-input" placeholder="Ask about your portfolio..." autocomplete="off" />' +
                '<button id="ai-chat-send">' +
                    '<span class="material-symbols-outlined" style="font-size:20px;">send</span>' +
                '</button>' +
            '</div>' +
        '</div>';
    document.body.appendChild(widget);

    /* ── DOM refs ── */
    var btn    = document.getElementById('ai-chat-button');
    var panel  = document.getElementById('ai-chat-panel');
    var close  = document.getElementById('ai-chat-close');
    var msgs   = document.getElementById('ai-chat-messages');
    var input  = document.getElementById('ai-chat-input');
    var send   = document.getElementById('ai-chat-send');

    /* ── State ── */
    var STORAGE_KEY = 'ai_chat_history';
    var isOpen = false;
    var isLoading = false;

    /* ── Load history ── */
    var history = [];
    try {
        var raw = sessionStorage.getItem(STORAGE_KEY);
        if (raw) history = JSON.parse(raw);
        else {
            // Seed with welcome message
            history = [{ role: 'bot', text: '👋 Hi! I\'m your AI Financial Advisor. Ask me about your portfolio, market trends, or what to do next.' }];
            sessionStorage.setItem(STORAGE_KEY, JSON.stringify(history));
        }
    } catch (e) {
        history = [];
    }

    function saveHistory() {
        try { sessionStorage.setItem(STORAGE_KEY, JSON.stringify(history)); } catch (e) {}
    }

    function scrollBottom() {
        msgs.scrollTop = msgs.scrollHeight;
    }

    /* ── Render ── */
    function renderMessages() {
        var html = '';
        for (var i = 0; i < history.length; i++) {
            var m = history[i];
            html += '<div class="ai-msg ' + (m.role === 'user' ? 'user' : 'bot') + '">' + escapeHtml(m.text) + '</div>';
        }
        msgs.innerHTML = html;
        scrollBottom();
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function showTyping() {
        msgs.innerHTML += '<div class="ai-msg bot ai-typing"><span></span><span></span><span></span></div>';
        scrollBottom();
    }

    function hideTyping() {
        var typing = msgs.querySelector('.ai-typing');
        if (typing) typing.remove();
    }

    /* ── Open / Close ── */
    function openPanel() {
        isOpen = true;
        panel.classList.add('open');
        renderMessages();
        input.focus();
    }

    function closePanel() {
        isOpen = false;
        panel.classList.remove('open');
    }

    btn.addEventListener('click', function () {
        if (isOpen) closePanel(); else openPanel();
    });

    close.addEventListener('click', function (e) {
        e.stopPropagation();
        closePanel();
    });

    /* ── Send ── */
    function sendMessage() {
        var text = input.value.trim();
        if (!text || isLoading) return;
        input.value = '';

        // Add user message
        history.push({ role: 'user', text: text });
        saveHistory();
        renderMessages();

        // Show typing
        isLoading = true;
        showTyping();

        // Call backend
        var apiFn = (typeof api === 'function') ? api : function (url, opts) {
            return fetch(url, opts).then(function (r) { return r.json(); });
        };

        apiFn('/api/advisor/chat', {
            method: 'POST',
            body: { message: text }
        }).then(function (data) {
            hideTyping();
            isLoading = false;
            var reply = (data && data.reply) ? data.reply : 'Sorry, I could not process that.';
            history.push({ role: 'bot', text: reply });
            saveHistory();
            renderMessages();
        }).catch(function () {
            hideTyping();
            isLoading = false;
            history.push({ role: 'bot', text: '⚠️ Unable to reach the advisor. Please try again later.' });
            saveHistory();
            renderMessages();
        });
    }

    send.addEventListener('click', sendMessage);
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') sendMessage();
    });

    /* ── Click outside to close (optional but nice) ── */
    document.addEventListener('click', function (e) {
        if (isOpen && !widget.contains(e.target)) {
            closePanel();
        }
    });

    /* ── Render initial state ── */
    renderMessages();
})();
