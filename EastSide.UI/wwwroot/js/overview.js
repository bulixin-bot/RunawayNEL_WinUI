function renderOverview(userData) {
    // 概括页显示一个公告和实时时钟
    return `
        <div class="page overview-page">
            <div class="overview-top">
                <div class="overview-notice" id="overview-notice">公告：暂无最新公告。</div>
                <div class="overview-clock" id="overview-clock">--:--:--</div>
            </div>
            <div class="overview-body">
                <p>欢迎使用 Runaway NEL。请通过左侧菜单打开功能页面。</p>
                <div id="recent-play"></div>
            </div>
        </div>`;
}

let __overviewClockTimer = null;
function startOverviewClock() {
    const el = document.getElementById('overview-clock');
    if (!el) return;
    function update() {
        const d = new Date();
        const hh = String(d.getHours()).padStart(2, '0');
        const mm = String(d.getMinutes()).padStart(2, '0');
        const ss = String(d.getSeconds()).padStart(2, '0');
        el.textContent = `${hh}:${mm}:${ss}`;
    }
    update();
    __overviewClockTimer = setInterval(update, 1000);
}

function stopOverviewClock() {
    if (__overviewClockTimer) { clearInterval(__overviewClockTimer); __overviewClockTimer = null; }
}

async function loadRecentPlay() {
    const container = document.getElementById('recent-play');
    if (!container) return;

    try {
        const result = await Bridge.send('overview:recent');
        if (!result.success || !result.data?.items?.length) {
            container.innerHTML = '';
            return;
        }

        const items = result.data.items;
        const listHtml = items.map(item => {
            const typeLabel = item.type === 'rental' ? '租赁服' : '网络服';
            const typeCls = item.type === 'rental' ? 'type-rental' : 'type-network';
            const time = formatDate(item.playTime);
            return `
                <div class="recent-play-item" data-id="${escapeHtml(item.serverId)}"
                     data-name="${escapeHtml(item.serverName)}" data-type="${escapeHtml(item.type)}"
                     data-version="${escapeHtml(item.mcVersion || '')}"
                     data-pwd="${item.hasPassword ? '1' : '0'}">
                    <div class="recent-play-info">
                        <span class="recent-play-name">${escapeHtml(item.serverName)}</span>
                        <span class="recent-play-type ${typeCls}">${typeLabel}</span>
                    </div>
                    <div class="recent-play-meta">
                        <span class="recent-play-time">${time}</span>
                        <button class="recent-play-join">加入</button>
                    </div>
                </div>`;
        }).join('');

        container.innerHTML = `
            <div class="recent-play-section">
                <h3 class="recent-play-title">最近游玩</h3>
                <div class="recent-play-list">${listHtml}</div>
            </div>`;

        container.querySelectorAll('.recent-play-join').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const item = btn.closest('.recent-play-item');
                handleRecentJoin(item.dataset);
            });
        });
    } catch (e) {
        container.innerHTML = '';
    }
}

function handleRecentJoin(data) {
    if (!_currentUserData || !_currentUserData.userId) {
        document.querySelector('.nav-item[data-page="account"]')?.click();
        showToast('请先登录账号', 'warning');
        return;
    }

    const page = data.type === 'rental' ? 'rental' : 'network';
    document.querySelector(`.nav-item[data-page="${page}"]`)?.click();

    setTimeout(() => {
        if (data.type === 'rental') {
            openRentalDetail(data.id, data.name, data.version, data.pwd === '1');
        } else {
            openNetworkDetail(data.id, data.name);
        }
    }, 300);
}

async function loadGameDuration() {
    const container = document.getElementById('game-duration');
    if (!container) return;

    try {
        const result = await Bridge.send('overview:gameDuration');
        const totalMinutes = result.success && result.data ? result.data.totalMinutes : 0;
        const formattedDuration = formatDuration(totalMinutes);
        
        container.innerHTML = `
            <div class="game-duration-section">
                <h3 class="game-duration-title">游戏时长</h3>
                <div class="game-duration-value">${formattedDuration}</div>
            </div>`;
    } catch (e) {
        container.innerHTML = '';
    }
}

function loadRunawayIntro() {
    // 已移除默认介绍内容，概括页为简洁提示
}

function formatDuration(minutes) {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0) {
        return `${hours}小时${mins}分钟`;
    } else {
        return `${mins}分钟`;
    }
}
