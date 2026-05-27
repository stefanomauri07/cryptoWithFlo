function initLayout(activePage) {
    var pageToNavIdx = { dashboard: 0, markets: 1, portfolio: 2, alerts: 3, news: 4, compare: 5, advisor: 6 };
    var pageToSideIdx = { dashboard: 0, markets: 1, alerts: 2, portfolio: 3, news: 4, compare: 5, advisor: 6 };

    var navLinks = [
        { label: 'Dashboard', href: 'index.html' },
        { label: 'Markets', href: 'market-explorer.html' },
        { label: 'Portfolio', href: 'portfolio.html' },
        { label: 'Alerts', href: 'alert-history.html' },
        { label: 'News', href: 'news.html' },
        { label: 'Compare', href: 'compare.html' },
        { label: 'Advisor', href: 'advisor.html' }
    ];

    var sidebarItems = [
        { label: 'Overview', href: 'index.html', icon: 'dashboard' },
        { label: 'Assets', href: 'market-explorer.html', icon: 'account_balance_wallet' },
        { label: 'History', href: 'alert-history.html', icon: 'history' },
        { label: 'Portfolio', href: 'portfolio.html', icon: 'pie_chart' },
        { label: 'News', href: 'news.html', icon: 'newspaper' },
        { label: 'Compare', href: 'compare.html', icon: 'compare_arrows' },
        { label: 'Advisor', href: 'advisor.html', icon: 'psychology' }
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
        '<span id="user-role" class="text-label-md font-label-md px-2 py-0.5 rounded"></span>' +
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
    var userRole = document.getElementById('user-role');

    if (typeof getCurrentUser === 'function') {
        var user = getCurrentUser();
        if (!user) return;

        var isPro = user.role === 'vip' || user.role === 'admin';

        var roleLabel = '';
        if (user.role === 'vip') {
            roleLabel = 'VIP';
        } else if (user.role === 'admin') {
            roleLabel = 'PRO';
        } else {
            roleLabel = 'USER';
        }

        if (userRole) {
            if (user.role === 'vip' || user.role === 'admin') {
                userRole.textContent = roleLabel;
                userRole.style.background = 'rgba(255,215,0,0.15)';
                userRole.style.color = '#FFD700';
            } else {
                userRole.textContent = 'USER';
                userRole.style.background = 'rgba(0,255,136,0.15)';
                userRole.style.color = '#00ff88';
            }
        }

        if (isPro) {
            if (navUpgrade) {
                navUpgrade.classList.remove('hidden');
                navUpgrade.innerHTML = '<span class="material-symbols-outlined text-sm" style="font-variation-settings:\'FILL\'1">check_circle</span> Pro Active';
                navUpgrade.className = 'flex items-center gap-1 text-[#FFD700] font-label-md text-label-md px-3 py-1 border border-[#FFD700]/40 rounded-lg hover:bg-[#FFD700]/10 transition-colors';
            }
            if (sidebarUpgrade) {
                sidebarUpgrade.classList.remove('hidden');
                sidebarUpgrade.innerHTML = '<span class="material-symbols-outlined text-sm" style="font-variation-settings:\'FILL\'1">verified</span> Pro Active';
                sidebarUpgrade.className = 'w-full bg-[#FFD700]/10 text-[#FFD700] hover:bg-[#FFD700]/20 transition-colors font-label-md text-label-md py-3 rounded-lg border border-[#FFD700]/30 text-center block flex items-center justify-center gap-1';
            }
        } else {
            if (navUpgrade) {
                navUpgrade.classList.remove('hidden');
                navUpgrade.innerHTML = 'Upgrade';
                navUpgrade.className = 'text-primary-fixed-dim font-label-md text-label-md px-3 py-1 border border-primary-container/30 rounded-lg hover:bg-primary-container/10 transition-colors';
            }
            if (sidebarUpgrade) {
                sidebarUpgrade.classList.remove('hidden');
                sidebarUpgrade.innerHTML = 'Upgrade to Pro';
                sidebarUpgrade.className = 'w-full bg-primary-container/10 text-primary-fixed-dim hover:bg-primary-container/20 transition-colors font-label-md text-label-md py-3 rounded-lg border border-primary-container/20 text-center block';
            }
        }
    }
}

window.initLayout = initLayout;
