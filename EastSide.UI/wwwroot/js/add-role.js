function showAddRoleDialog(serverId, onSuccess, action, accountId) {
    const createAction = action || 'network:createRole';
    // 防止重复弹窗
    const existing = document.querySelector('.dialog-overlay[data-role-dialog="1"]');
    if (existing) {
        // 已有弹窗，聚焦输入框
        const input = existing.querySelector('#role-name-input');
        if (input) input.focus();
        return;
    }

function generateRandomChineseName() {
    const pick = arr => arr[Math.floor(Math.random() * arr.length)];

    const allWords = [];
    if (Array.isArray(_rnChinese)) _rnChinese.forEach(w => { if (w) allWords.push(w); });
    if (Array.isArray(_rnChineseExtras)) _rnChineseExtras.forEach(w => { if (w) allWords.push(w); });

    const chars = getChineseChars() || [];

    // category buckets
    const creatures = [];
    const professions = [];
    const flavors = [];
    const descriptors = [];
    const others = [];

    const creatureChars = ['龙','狼','猫','犬','虎','蛇','牛','鸡','羊','猪','狐','熊','豹','马','象','鹿','猴','兔','鱼','虫','鸟','鲨','鲸','鳄','豹','狮','豹','虎','豹'];
    const profKeywords = ['师','者','手','官','家','员','工','医','师','师长','师','官','者','者','长','员'];

    for (const w of allWords) {
        if (!w) continue;
        if (w.includes('味')) { flavors.push(w); continue; }
        let isCreature = false;
        for (const c of creatureChars) { if (w.includes(c)) { isCreature = true; break; } }
        if (isCreature) { creatures.push(w); continue; }
        let isProf = false;
        for (const k of profKeywords) { if (w.includes(k)) { isProf = true; break; } }
        if (isProf) { professions.push(w); continue; }
        if (w.endsWith('的') || w.includes('之')) { descriptors.push(w); continue; }
        others.push(w);
    }

    // ensure fallback content
    const baseChars = (chars.length > 0) ? chars : Array.from(new Set(allWords.join('').split(''))).filter(Boolean);
    const randChar = () => baseChars[Math.floor(Math.random() * baseChars.length)] || '风';

    function padOrTrim(s) {
        let res = (s || '').slice(0, 8);
        if (res.length < 6) {
            while (res.length < 6) res += randChar();
            res = res.slice(0, 8);
        }
        return res;
    }

    // strategies that mix at most two categories to avoid混杂
    for (let i = 0; i < 6000; i++) {
        const r = Math.random();
        let cand = '';
        if (r < 0.18 && creatures.length) {
            // creature-focused: maybe prefix/descriptor + creature
            const c = pick(creatures);
            if (Math.random() < 0.4 && descriptors.length) cand = pick(descriptors) + c;
            else if (Math.random() < 0.3 && professions.length) cand = pick(professions) + c;
            else cand = c;
        } else if (r < 0.36 && professions.length) {
            // profession-focused: profession + descriptor or descriptor + profession
            const p = pick(professions);
            if (Math.random() < 0.5 && descriptors.length) cand = p + pick(descriptors);
            else cand = pick(descriptors) + p;
        } else if (r < 0.52 && flavors.length && creatures.length) {
            // flavor + creature (small chance)
            cand = pick(flavors) + pick(creatures);
        } else if (r < 0.75 && descriptors.length && others.length) {
            // descriptor + other
            cand = pick(descriptors) + pick(others);
        } else {
            // hybrid fallback: word + random chars
            const w = pick(allWords) || '';
            cand = w + randChar();
        }

        cand = padOrTrim(cand.replace(/\s+/g, ''));
        if (cand.length >= 6 && cand.length <= 8) return cand;
    }

    // final fallback: random chars
    let fb = '';
    for (let i = 0; i < 6; i++) fb += randChar();
    return fb.slice(0, 8);
}
    const overlay = document.createElement('div');
    overlay.setAttribute('data-role-dialog', '1');
    overlay.className = 'dialog-overlay';
    overlay.style.zIndex = '5100';
    overlay.innerHTML = `
        <div class="dialog-box" style="width:360px">
            <div class="dialog-header">
                <h3>添加角色</h3>
                <button class="dialog-close" id="role-close-btn">&times;</button>
            </div>
            <div class="dialog-body">
                <div class="form-group">
                    <input id="role-name-input" type="text" placeholder="输入角色名称" maxlength="8" />
                </div>
                <div style="display:flex;gap:8px;">
                    <button class="btn-random-name" id="role-random-btn">随机英文角色</button>
                    <button class="btn-random-cn" id="role-random-cn-btn">随机中文角色</button>
                </div>
                <div id="role-error" class="dialog-error"></div>
            </div>
            <div class="dialog-footer">
                <button class="btn-secondary" id="role-cancel-btn">取消</button>
                <button class="btn-accent" id="role-confirm-btn">添加</button>
            </div>
        </div>
    `;
    document.body.appendChild(overlay);

    const close = () => {
        overlay.classList.add('closing');
        overlay.addEventListener('animationend', () => overlay.remove(), { once: true });
    };
    overlay.querySelector('#role-close-btn').addEventListener('click', close);
    overlay.querySelector('#role-cancel-btn').addEventListener('click', close);
    overlay.addEventListener('click', (e) => { if (e.target === overlay) close(); });

    setTimeout(() => overlay.querySelector('#role-name-input')?.focus(), 50);

    overlay.querySelector('#role-random-btn').addEventListener('click', async () => {
        const nameInput = overlay.querySelector('#role-name-input');
        try {
            const rolesAction = (createAction === 'rental:createRole') ? 'rental:roles' : 'network:roles';
            const resp = await Bridge.send(rolesAction, { serverId, accountId });
            const used = new Set();
            if (resp.success && resp.data?.roles && Array.isArray(resp.data.roles)) {
                resp.data.roles.forEach(r => {
                    try { if (r.name) used.add(String(r.name).toLowerCase()); } catch { }
                });
            }

            for (let i = 0; i < 2000; i++) {
                const name = generateRandomName();
                try {
                    if (!used.has(name.toLowerCase())) {
                        nameInput.value = name;
                        return;
                    }
                } catch { /* ignore casing issues for non-english names */ }
            }

            // Fallback: try base+number
            const base = generateRandomName().slice(0, 6);
            for (let n = 1; n < 10000; n++) {
                let candidate = base + n;
                if (candidate.length > 8) candidate = candidate.slice(0, 8);
                if (!used.has(candidate.toLowerCase())) {
                    nameInput.value = candidate;
                    return;
                }
            }

            // Last resort
            nameInput.value = generateRandomName();
        } catch (e) {
            nameInput.value = generateRandomName();
        }
    });

    // ensure chinese button has same visual style and is always bound
    const cnBtn = overlay.querySelector('#role-random-cn-btn');
    if (cnBtn) {
        cnBtn.className = 'btn-random-name';
        cnBtn.addEventListener('click', () => {
            const nameInput = overlay.querySelector('#role-name-input');
            if (!nameInput) return;
            try {
                let val = generateRandomChineseName();
                if (!val || val.trim().length === 0) {
                    // fallback: build from chinese chars
                    const chars = getChineseChars();
                    if (chars && chars.length > 0) {
                        val = '';
                        for (let i = 0; i < 6; i++) val += chars[Math.floor(Math.random() * chars.length)];
                    } else {
                        // ultimate fallback: pick first characters from _rnChinese
                        val = '';
                        for (const s of _rnChinese) {
                            if (!s) continue;
                            val += s[0];
                            if (val.length >= 6) break;
                        }
                    }
                }
                nameInput.value = val.slice(0, 8);
            } catch (e) {
                // silent fallback
                try {
                    const chars = getChineseChars() || [];
                    let val = '';
                    for (let i = 0; i < 6 && i < chars.length; i++) val += chars[i];
                    nameInput.value = val.slice(0, 8);
                } catch { nameInput.value = ''; }
            }
        });
    }

    overlay.querySelector('#role-confirm-btn').addEventListener('click', async () => {
        const name = overlay.querySelector('#role-name-input').value.trim();
        const errorEl = overlay.querySelector('#role-error');
        if (!name) { errorEl.textContent = '请输入角色名称'; return; }

        const btn = overlay.querySelector('#role-confirm-btn');
        btn.classList.add('btn-loading');

        try {
            const payload = { serverId, roleName: name };
            if (accountId) payload.accountId = accountId;
            const cr = await Bridge.send(createAction, payload);
            if (cr.success && cr.data?.roles) {
                const used = cr.data?.usedName || name;
                showToast(`角色创建成功: ${used}`, 'success');
                close();
                if (onSuccess) onSuccess(cr.data.roles, used);
            } else {
                errorEl.textContent = cr.data?.message || '创建失败';
            }
        } catch (e) {
            errorEl.textContent = '创建角色失败';
        } finally {
            btn.classList.remove('btn-loading');
        }
    });
}

const _rnAdj = [
    'Dark','Ice','Red','Sky','Sun','Ash','Dew','Fog','Grim','Hex','Ivy','Jet','Kin','Lux','Neo','Odd','Raw','Sly','Zen','Arc',
    'Dim','Elm','Fey','Haze','Cold','Hot','Dry','Wet','Old','New','Big','Mad','Sad','Bad','Shy','Coy','Wry','Icy','Orc','Elf',
    'Gem','Nyx','Pax','Rex','Rox','Zap','Ace','Axe','Bay','Blu','Cob','Dax','Eve','Fin','Gil','Hal','Ion','Jax','Koi','Leo',
    'Max','Nix','Oak','Pix','Qin','Rio','Sol','Tao','Uma','Vox','Wiz','Xen','Yew','Zed','Oni','Boa','Cub','Doe','Emu','Fig',
    'Gnu','Hop','Imp','Jay','Kit','Lac','Mew','Nap','Orb','Pug','Rum','Sap','Tin','Urn','Van','Wax','Yak','Zip','Ale','Aero',
    'Bram','Cinder','Drift','Ember','Fable','Gloom','Hollow','Ignis','Jolt','Kite','Lumen','Mirth','Nova','Orion','Plume','Quill','Rune','Sable','Thorn','Umber',
    'Valk','Wisp','Yara','Zephyr','Blaze','Cascade','Dynamo','Echo','Frost','Glint','Harbor','Ionix','Jade','Karma','Luxe','Mako','Nimbus','Onyx','Pulse','Quake',
    'Ripple','Solace','Tempest','Ulix','Vapor','Warden','Xylo','Yield','Zyra','Alpha','Beta','Gamma','Omega','Sigma','Prime','Novae','Solar','Lunar','Stellar','Photon',
    'Neon','Crystal','Silent','Noct','Vivid','Cobalt','Scarlet','Amber','Iron','Bronze','Copper','Gold','Silver','Chrome','Obsidian','Ivory','Marble','Granite','Turbo','Hyper',
    'Quantum','Vector','Polar','Axial','Hyperion','Echoic','Nebula','Glyph','Rogue','Stealth','Brave','Bold','Swift','Fleet','Fierce','Mighty','Nimble','Spry','Tiny','Micro',
    'Macro','Grand','Epic','Myth','Legend','Hero','Sage','Oracle','Chroma','Prism','Aura','Zenith','Nexus','Cipher','Vigil','Beacon','Draco','Hydra','Sigil','Arcan',
    'Myst','Ether','Chaos','Order','Urban','Rural','Coastal','Frosty','Sunny','Storm','Bramble','Thorny','Gale','Drifter','Emberly','Cerulean','Viridian','Saffron','Tender','Moss',
    'Glacier','Sterling','Talon','Rift','Spear','Blitz','Flicker','Shard','Torrent','Wisped','Glim','Ferno','Arcane','Mystic','Elder','Young','Minor','Loud','Hearth','Rust',
    'Forge','Valor','Honor','Gleam','Ranger','Tracker','Scribe','Weaver','Mender','Seer','Pilot','Navigator','Cruel','Kind','Bright','Dawn','Auric','Boreal','Crimson','Duskm',
    'Ebon','Flint','Galeon','Hearth','Ivory','Jasper','Kest','Lyr','Merc','Noble','Aether','Boreal','Cobalt','Dynamo','Eclipse','Flux','Gale','Harbor','Indigo','Jasper',
    'Kestrel','Lumina','Marble','Nimbus','Obsidian','Peregrine','Quasar','Raptor','Silvan','Tide','Vapor','Warden','Xenon','Yonder','Zeph',
    'Sylph','Vireo','Ardent','Brisk','Crimson','Dusky','Ebonic','Feral','Gilded','Hushed','Ionic','Jovial','Keen','Lucid','Mellow','Nebular','Opaline','Plush','Quaint','Rosy',
    'Stark','Torrid','Umbral','Verdant','Whim','Xylen','Yielding','Zealous','Azure','Beryl','Cerulean','Driftwood','Elder','Fjord','Gossamer','Hollow','Irides','Juniper','Kairo','Lattice'
];
const _rnNoun = [
    'Wolf','Fox','Hawk','Bear','Lynx','Crow','Deer','Owl','Pike','Wren','Moth','Fang','Claw','Bone','Star','Moon','Fire','Sage','Rune','Vex',
    'Bolt','Mist','Gale','Dusk','Dawn','Void','Flux','Haze','Jade','Onyx','Opal','Ruby','Tusk','Vine','Wave','Yeti','Apex','Bane','Core','Doom',
    'Edge','Fury','Glow','Horn','Iron','Jinx','Knot','Lore','Maze','Nuke','Omen','Pyre','Rift','Scar','Tide','Urge','Vale','Wisp','Xray','Yawn',
    'Zeal','Axle','Bark','Cave','Dune','Echo','Fern','Grit','Husk','Isle','Jolt','Kelp','Lava','Mane','Nest','Oath','Palm','Quay','Root','Silk',
    'Twig','Vent','Warp','Yarn','Zinc','Arch','Blot','Coil','Dart','Fist','Gust','Helm','Iris','Kite','Lamp','Mace','Node','Oryx','Pond','Reef',
    'Breeze','Cinder','Delta','Ember','Fjord','Glade','Harbor','Isthm','Jungle','Knoll','Lagoon','Meadow','Nexus','Orch','Prairie','Quarry','Ridge','Summit','Thicket','Upland',
    'Vortex','Willow','Xenon','Yonder','Zephyr','Boulder','Cascade','Drake','Ever','Frost','Glimmer','Hollow','Ivory','Jester','Keeper','Lantern','Mariner','Nimbus','Oracle','Pilot',
    'Quiver','Ranger','Seeker','Tracker','Umber','Voyage','Warden','Xplorer','Yarrow','Zenith','Dragon','Phoenix','Griffin','Hydra','Titan','Golem','Sprite','Nymph','Dryad','Pirate',
    'Rogue','Knight','Paladin','Mage','Warlock','Archer','Assassin','Monk','Spear','Blade','Aegis','Shield','Scroll','Tome','Crystal','Mirror','Crown','Banner','Harpoon','Anchor',
    'Compass','Beacon','Bow','Quill','Lyre','Grove','Thorn','Briar','Ravine','Cliff','Span','Bridge','Cairn','Comet','Meteor','Orbit','Nova','Nebula','Aurora','Galaxy',
    'Pulse','Quasar','Circuit','Wire','Chip','Core','Node','Kernel','Matrix','Vector','Byte','Cache','Echo','Whisper','Shout','Cry','Song','Chord','Note',
    'Melody','Rhythm','Bass','Bean','Pea','Corn','Wheat','Barley','Oats','Apple','Pear','Plum','Berry','Koala','Panda','Otter','Beetle','Dragonfly','Mantis','Falcon',
    'Condor','Stallion','Mustang','Raven','Sparrow','Heron','Mallard','Stag','Bison','Badger','Marauder','Nomad','Pilgrim','Vagabond','Wisp','Shade','Spark','Furnace','Keel','Talon',
    'Basin','Cavern','Spire','Obelisk','Pylon','Harbinger','Voyager','Corsair','Sailor','Gunner','Smith','Artisan','Bard','Minstrel','Keeper','Ally','Foe','Guardian','Rift','Pulse',
    'Matrix','Cipher','Quiver','Strider','Runner','Stalker','Hunter','Seeker','Watcher','Binder','Caller','Shaman','Druid','Forger','Tinker','Wright','Mariner','Cart','Vagary','Hearth',
    'Beacon','Voyager','Corsair','Sailor','Gunner','Smith','Artisan','Bard','Minstrel','Keeper','Ally','Foe','Guardian','Harbinger','Mariner','Pilgrim','Nomad','Marauder','Sentinel','Ward'
];

// add many more nouns to massively increase combinations
_rnNoun.push(
    'Voyage','Harbinger','Galleon','Corsair','Warden','Mast','Keel','Fathom','Grove','Fen','Thicket','Hearth','Forge','Anvil','Ledger','Quill','Banner','Pylon','Spire','Talon',
    'Mantle','Garnet','Obelisk','Citadel','Vanguard','Sentinel','Harbor','Mariner','Arcade','Beacon','Tribune','Cinder','Ember','Ravine','Glacier','Mesa','Cliff','Crag','Tide','Brine'
);

// Extra short fragments to inject between adj and noun for more variety
const _rnExtra = ['Neo','Cryo','Pyro','Aero','Elect','Meta','Hyper','Ultra','Mini','Maxi','Prime','Proto','Omega','Kilo','Mega','Nano','Giga','Lumi','Noct','Solar','Luxe','Rune','Vex','Nox','Vita','Chron','Volt','Arc','Flux','Pulse'];

// Numeric/letter suffix pool used occasionally
const _rnSuffix = ['X','Z','7','9','01','11','77','88','99','XR','AX','NX','Q1','V2','M3','R8'];

// small fragment pool (many short segments) to explode combinations
const _rnFragment = [
    'ax','en','or','li','ro','ka','ta','ze','vo','xi','ra','na','ma','lo','si','di','fi','gi','ha','ju',
    'ki','la','mi','nu','po','qu','re','so','tu','vi','wo','ya','za','bi','co','do','eo','fo','go','ho','io',
    'jo','ko','lu','mo','no','op','pi','qi','ru','su','ti','uo','va','we','xe','ya','zu','al','el','il','ol','ul',
    'an','en','in','on','un','ar','er','ir','or','ur','as','es','is','os','us','ath','eth','ith','oth','uth','ard','end','old','ern','ion','ial','ous','ant','ent','int','olt','ark','ash','aze','ble','cle'
];

// extra pool for deep mixing
const _rnExtra2 = ['zen','zor','lex','ron','dak','mox','ver','tal','syn','rox','gax','lyn','vex','tor','mar','kro','syl','nix','zor','fex','bex','rix','zol','qir','sor','tix','dux','pax','vul','rax','nox','laz','mir','gol','har','bir','cen','dor','eph','fyn','gyn','hal','ior','jol','kal','lor','myn','nor','oph','pry','quin','ryx','sav','tur','uly','vra','wex','xan','yol','zun'];

// Short English syllable segments to massively expand combinations
const _segStart = ['al','ar','bal','bel','cal','cor','dar','del','el','en','fal','gar','hal','il','jar','kel','lor','mar','nor','or','par','quil','ral','sal','tal','ul','var','wel','xel','yor','zel','ax','ix','ox','ux','zen','nic','sol','ver','tri','neo','aero','cy','py','ry','bra','cri','dra','eph','fen','gro','har','ith','jor','kai','lum','mal','nor','oph','pra','que','ryn','sha'];
const _segMid = ['a','e','i','o','u','an','en','in','on','un','ar','er','ir','or','ur','al','el','il','ol','ul','yn','ix','ox','ax','ron','tan','mir','gor','ven','sel','rin','tor','dak','fel','gri','bor','zan','eth','ian','ora','ula','ima','ara','ero','oni','yra','uma'];
const _segEnd = ['on','ar','us','ix','or','en','an','el','in','er','io','is','as','um','ion','arx','eus','ian','eth','os','yx','arv','oth','aid','orn','esk','von','tar','set','lyn','mere','stone','shade','dawn','crest','ward','fall','mere','hold','keep','forge','helm','gore','bane','vein','spark'];

// lazy getter for Chinese character pool (build on first use)
function getChineseChars() {
    if (window.__rnChineseCharsCache && Array.isArray(window.__rnChineseCharsCache) && window.__rnChineseCharsCache.length > 0) {
        return window.__rnChineseCharsCache;
    }
    const set = new Set();
    try {
        if (Array.isArray(_rnChinese)) {
            for (const s of _rnChinese) {
                if (!s) continue;
                for (const ch of s) set.add(ch);
            }
        }
    } catch (e) { }
    window.__rnChineseCharsCache = Array.from(set).filter(c => c.trim().length > 0);
    return window.__rnChineseCharsCache;
}

// Chinese name pool (from Runaway/Assets/character.txt)
const _rnChinese = [
"蝙蝠","烈焰人","蜘蛛","鸡","鸡骑士","牛","爬行者","驴","守卫者","末影龙",
"末影人","末影螨","唤魔者","恶魂","巨人","马","尸壳","幻术师","铁傀儡","兔子",
"羊驼","吉祥物","岩浆怪","哞菇","骡","豹猫","鹦鹉","猪","北极熊","羊",
"潜影贝","蠹虫","骷髅","骷髅马","史莱姆","雪傀儡","守卫","鱿鱼","流髑","恼鬼",
"卫道士","村民","女巫","凋灵","狼","僵尸","僵尸马","鲑鱼","河豚","金枪鱼",
"鲤鱼","黄鳝","电鳗","泥鳅","巫师","弓手","公主","土豪","工程师","程序员",
"服主","阿婆主","妹子","精灵","兽人","矮人","龙","龙骑士","天使","恶魔",
"地狱疣","蘑菇","程序","美术","策划","开发","客服","侍卫","侍从","仆人",
"宅男","炮姐","侏儒","泰坦","血精灵","牛头人","牧师","圣骑士","猎人","德鲁伊",
"法师","术士","战士","盗贼","蜗牛","黑猪","国王","王子","女王","阴阳师",
"宗师","建筑师","特种兵","专家","猪骑士","骑士","狼骑士","君主","郡主","君王",
"骷髅兵","吉吉怪","苦力怕","蜘蛛娘","苦力娘","僵尸娘","末影娘","哞菇娘","凋零娘","搬运工",
"版主","汉化组","字幕君","画师","漫画家","动画师","声优","歌手","唱见","舞见",
"人偶师","主播","编剧","导演","吉他手","监督","贝斯手","主唱","鼓手","房管",
"贝斯","苹果","香蕉","橘子","桃子","荔枝","龙眼","桔子","李子","葡萄",
"青梅","椰子","石榴","草莓","栗子","梨子","樱桃","梨","木瓜","芒果",
"菠萝","柠檬","柿子","柚子","无花果","猕猴桃","西红柿","水蜜桃","西瓜","南瓜",
"甘蔗","小麦","高粱","胡萝卜","马铃薯","可可豆","仙人掌","白菜","黄瓜","豌豆",
"苦瓜","菠菜","冬瓜","茄子","竹笋","蚕豆","萝卜","辣椒","火龙","冰龙",
"野狼","野猪","双头龙","猴子","猎豹","企鹅","青蛙","蝌蚪","猛犸","半兽人",
"亚龙人","半人马","牛头","牛头怪","食人魔","仙女","小仙女","蛇妖","女妖","妖怪",
"地精","霍比特","半身人","巫女","萨满","魔王","魔女","地狱犬","甲虫","罗刹",
"石像","雕塑","飞马","树精","娜迦","狼人","猫人","猫女","巨魔","海豹",
"蜥蜴","三文鱼","希鲮鱼","纸巾","豹子","狮子","狮子王","妖精","英雄","侠客",
"老鼠","猫咪","狼狗","哈士奇","金毛","萨摩","斗牛犬","牧羊犬","猎犬","吉娃娃",
"八哥","腊肠犬","柯基","约克夏","松狮","秋田犬","柴犬","博美","藏獒","牛头梗",
"比熊","二郎神","玉帝","弼马温","波斯猫","英短","布偶","美短","入殓师","清洁工",
"教师","清道夫","律师","医生","码农","猛男","学姐","学长","师兄","大兵",
"网红","帅哥","课代表","班长","组长","跳蛛","蜜柑","痒痒鼠","跳跳鼠","作家",
"维修工","快递员","蝴蝶","瓢虫","蚂蚱","蚂蚁","毛毛虫","屎壳郎","苍蝇","蜜蜂",
"独角仙","飞蛾","天牛","鼻涕虫","金龟子","红蚂蚁","蚜虫","甲壳虫","蛾子","跳蚤",
"兔狲","短毛猫","折耳猫","暹罗猫","无毛猫","卷毛猫","猞猁","云豹","花豹","雪豹",
"灰狼","鬃狼","沙狐","藏狐","北极狐","苍狐","赤狐","大耳狐","貂","画眉鸟",
"麻雀","鸽子","文鸟","珍珠鸟","蜂鸟","火烈鸟","海鸥","猫头鹰","苍鹰","秃鹫",
"布谷鸟","乌鸦","灰鹦鹉","蜡嘴鸟","园丁鸟","孔雀","喜鹊","杜鹃","翠鸟","啄木鸟",
"主管","经理","监工","规划师","药师","护理","护士","会计","咨询师","翻译",
"记者","兽医","测量员","面壁者","破壁人","中介","厨师","老板","掌勺","营养师",
"推销员","司机","售票员","管理","导游","调酒师","美容师","理发师","解说员","交易员",
"保姆","苗圃工","设计师","模特","售货员","保安","警察","消防员","花匠","水电工",
"建筑工","电工","钳工","修护工","铸造工","缝纫工","顾问","白领","公务员","文秘"
,
"炼金师","布衣","侠女","少年","老翁","技师","药童","刀客","剑客","巫师",
"灵狐","玄猫","赤鹿","青龙","白虎","朱雀","玄武","游侠","浪人","术士",
"铸剑师","驯兽师","剑豪","武尊","堂主","掌门","方士","蛮王","铁匠","织女",
"织工","舟子","牧童","樵夫","渔翁","茶师","酒徒","画匠","刻师","雕匠",
"雕刻者","旅人","旅者","守望者","哨兵","信使","快马","驿使","药师长","仲夏",
"寂夜","青梅","暮雪","晨曦","长风","秋水","春晓","夏雨","光年","流年",
"音尘","琴心","棋子","书童","墨客","诗人","酒仙","花间","云游","独行",
"夜影","晨星","星河","苍穹","落霞","孤鸿","断桥","风行","雨落","霜刃"
];

// 额外中文短语（来自用户输入），可与 _rnChinese 混合搭配
const _rnChineseExtras = [
"强硬的","强悍的","强劲的","坚决的","坚信的","坚定的","坚韧的","坚实的","坚贞的","勇敢的",
"勇猛的","刚毅的","决断的","果敢的","果决的","坚强的","坚忍的","决然的","毅然的","断然的",
"泼辣的","断腕的","发誓的","干脆的","爽快的","果断的","真诚的","热诚的","至诚的","赤诚的",
"诚挚的","恳切的","纯真的","率直的","坦率的","笃实的","热忱的","热心的","好客的","客气的",
"殷勤的","和气的","和蔼的","和善的","亲切的","过谦的","谦卑的","谦恭的","谦和的","谦让的",
"谦虚的","谦逊的","虚心的","外向的","开朗的","大方的","主动的","俏皮的","敏捷的","乐观的",
"调皮的","爽脆的","爽朗的","豪爽的","正直的","直率的","直爽的","直言的","爽直的","刚直的",
"憨直的","耿直的","公正的","公道的","公平的","公允的","正派的","开阔的","豁达的","明朗的",
"率真的","怒吼的","恐惧的","胆怯的","畏缩的","发慌的","心慌的","恐慌的","激怒的","恼火的",
"欢乐的","快慰的","开心的","高兴的","愉悦的","微笑的","舒畅的","不适的","欢闹的","欢心的",
"欢欣的","欢悦的","宽慰的","欢舒的","狂欢的","震怒的","气愤的","担忧的","发愁的","犯愁的",
"忧伤的","忧愁的","忧心的","愁闷的","悲痛的","悲惨的","悲凉的","哀伤的","哀怨的","伤感的",
"瘦削的","憔悴的","快乐的","喜悦的","愉快的","畅快的","欢畅的","欢喜的","欢腾的","欢快的",
"欣喜的","今天的","昨天的","明天的","后天的","上午的","下午的","过去的","未来的","去年的",
"前年的","散步的","漫步的","踏步的","信步的","转悠的","闲逛的","徜徉的","踉跄的","蹒跚的",
"小跑的","慢跑的","飞跑的","飞奔的","飞翔的","啜泣的","抽泣的","呜咽的","哀号的","号哭的",
"痛哭的","大笑的","欢笑的","嬉笑的","狂笑的","嗤笑的","憨笑的","傻笑的","哄笑的","苦笑的",
"阴笑的","狞笑的","奸笑的","嘲笑的","冷笑的","哈腰的","猫腰的","挺身的","挺胸的","俯身的",
"躬身的","仰卧的","蜷曲的","倒立的","转体的","屈体的","屈身的","欠身的","纵身的","蹲身的",
"鞠躬的","曲背的","匍匐的","笔挺的","腾跃的","直立的","翻腾的","前倾的","摇摆的","翻跃的",
"扭动的","扭转的","旋转的","好吃的","好看的","好玩的","清白的","凛然的","无私的","刚正的",
"负重的","忠心的","忠贞的","谨慎的","廉洁的","大度的","坦白的","勤奋的","刻苦的","认真的",
"专注的","踏实的","勤恳的","好学的","高尚的","杰出的","超伦的","自爱的","自尊的","自强的",
"宽容的","宽宏的","律己的","朴素的","憨厚的","诚实的","忠诚的","诚恳的","天真的","幼稚的",
"活泼的","聪明的","圆滑的","狡猾的","虚伪的","自私的","任性的","骄傲的","贪婪的","愚蠢的",
"奸诈的","高傲的","害羞的","内向的","孤僻的","可爱的","招烦的","阴险的","双重的","刻薄的",
"宽厚的","仁慈的","仁厚的","尖酸的","阴郁的","肤浅的","浅薄的","胆小的","乐天的","达观的",
"成熟的","稳重的","淘气的","温柔的","体贴的","强硬之","强悍之","强劲之","坚决之","坚信之",
"坚定之","坚韧之","坚实之","坚贞之","勇敢之","勇猛之","刚毅之","决断之","果敢之","果决之",
"坚强之","坚忍之","决然之","毅然之","断然之","泼辣之","断腕之","发誓之","干脆之","爽快之",
"果断之","真诚之","热诚之","至诚之","赤诚之","诚挚之","恳切之","纯真之","率直之","坦率之",
"笃实之","热忱之","热心之","好客之","客气之","殷勤之","和气之","和蔼之","和善之","亲切之",
"过谦之","谦卑之","谦恭之","谦和之","谦让之","谦虚之","谦逊之","虚心之","外向之","开朗之",
"大方之","主动之","俏皮之","敏捷之","乐观之","调皮之","爽脆之","爽朗之","豪爽之","正直之",
"直率之","直爽之","直言之","爽直之","刚直之","憨直之","耿直之","公正之","公道之","公平之",
"公允之","正派之","简捷之","开阔之","豁达之","明朗之","率真之","怒吼之","恐惧之","胆怯之",
"畏缩之","发慌之","心慌之","恐慌之","激怒之","恼火之","欢乐之","快慰之","开心之","高兴之",
"愉悦之","微笑之","舒畅之","笑噱之","欢闹之","欢心之","欢欣之","欢悦之","宽慰之","欢舒之",
"狂欢之","震怒之","气愤之","担忧之","发愁之","犯愁之","忧伤之","忧愁之","忧心之","愁闷之",
"悲痛之","悲惨之","悲凉之","哀伤之","哀怨之","伤感之","瘦削之","憔悴之","快乐之","喜悦之",
"愉快之","畅快之","欢畅之","欢喜之","欢腾之","欢快之","欣喜之","今天之","昨天之","明天之",
"后天之","上午之","下午之","过去之","未来之","去年之","前年之","散步之","漫步之","踏步之",
"信步之","转悠之","闲逛之","徜徉之","踉跄之","蹒跚之","小跑之","慢跑之","飞跑之","飞奔之",
"飞翔之","啜泣之","抽泣之","呜咽之","哀号之","号哭之","痛哭之","大笑之","欢笑之","嬉笑之",
"狂笑之","嗤笑之","憨笑之","傻笑之","哄笑之","苦笑之","阴笑之","狞笑之","奸笑之","嘲笑之",
"冷笑之","哈腰之","猫腰之","挺身之","挺胸之","俯身之","躬身之","仰卧之","蜷曲之","倒立之",
"转体之","屈体之","屈身之","欠身之","纵身之","蹲身之","鞠躬之","曲背之","匍匐之","笔挺之",
"腾跃之","直立之","翻腾之","前倾之","摇摆之","翻跃之","扭动之","扭转之","旋转之","好吃之",
"好看之","好玩之","清白之","凛然之","无私之","刚正之","负重之","忠心之","忠贞之","谨慎之",
"廉洁之","大度之","坦白之","勤奋之","刻苦之","认真之","专注之","踏实之","勤恳之","好学之",
"高尚之","杰出之","超伦之","自爱之","自尊之","自强之","宽容之","宽宏之","律己之","朴素之",
"憨厚之","诚实之","忠诚之","诚恳之","天真之","幼稚之","活泼之","聪明之","圆滑之","狡猾之",
"虚伪之","自私之","任性之","骄傲之","贪婪之","愚蠢之","奸诈之","高傲之","害羞之","内向之",
"孤僻之","可爱之","招烦之","阴险之","双重之","刻薄之","宽厚之","仁慈之","仁厚之","尖酸之",
"阴郁之","肤浅之","浅薄之","胆小之","乐天之","达观之","成熟之","稳重之","淘气之","温柔之",
"体贴之","苹果味","香蕉味","橘子味","桃子味","荔枝味","龙眼味","桔子味","李子味","葡萄味","青梅味",
"椰子味","石榴味","草莓味","栗子味","梨子味","樱桃味","木瓜味","芒果味","菠萝味","柠檬味",
"柿子味","柚子味","西瓜味","南瓜味","甘蔗味","小麦味","蜂蜜味","白菜味","黄瓜味","豌豆味",
"苦瓜味","菠菜味","冬瓜味","茄子味","竹笋味","蚕豆味","萝卜味","辣椒味","鸡肉味","牛肉味",
"烤肉味","炸鸡味","番茄味","芝士味","榴莲味","山楂味","水果味","陈皮味","花椒味","莲雾味",
"杨梅味","泥土味","枇杷味","杨桃味","板栗味","瓜子味","桑葚味","猪蹄味","香瓜味","怪味的",
"塑料味","简单的","枯燥的","仙气的","酸臭味","蜜柑味","火锅味","泡菜味","抹茶味","蓝莓味",
"无味","甜味","苦味","酸甜味"
];

// 行为/动作词池（用户提供），单独分类，生成时可与其他类别有策略地搭配
const _rnActions = [
"奔跑","爬行","蹦极","游行","吃土","剁手","飞行","滑翔","背书","学习","思考","度假","啃书","吃鲸","洗脸","刷牙","登基","诞生","挖矿","下矿","游蝶泳","蹦迪","吃糖","跳舞","吸猫","遛狗","遛娃","烧烤","拔线","狗吠","上学","飞升","探险","观光","抽卡","划水","潜水","赏花","赏月","品茶","化妆","煮饭","做饭","觉醒","长跑","吸气","呼气","练功","熬夜","听歌","开车","上车","下车","飘着","开学","放假","画画","弹琴","砍树","挖坑","寻宝","追寻","下坠","浮沉","劈叉","踏雪","睡觉","发梦","做梦","冲浪","跑酷","哭泣","咆哮","穿越","潜行","复习","追番","搁浅","吟诗","葬花","大笑","苦笑","尬笑","羽化","消亡","爆破","求佛","鸟瞰","发芽","开花","滑行","练发声","扮鬼","出击","闯关","解密","交易","听写","摘星","落泪","祈祷","冥想","吹牛","研究","购物","补牙","拔牙","舞剑","灌篮","预习","考试","卖萌","扮猪","吃狗粮","坐飞机","开赛车","吃橙子","吃苹果","吃菠萝","吃榴莲","吃香蕉","吃枇杷","喝圣水","跳热舞","吹喇叭","开飞机","水上漂","跳芭蕾","穿西装","穿裙子","看日出","建房子","说相声","吹短笛","练吉他","吹长笛","泡温泉","深呼吸","肝游戏","看涨潮","刷副本","喝阔咯","看日落","等吃饭","做作业","看直播","说谢谢","打豆豆","看视频","看大海","做自己","讲笑话","背古诗","写作文","写散文","斗蛐蛐","捉蛐蛐","捉昆虫","搞科研","种太阳","喝可乐","玩魔方","解方程","拿高分","影分身","捏泥人","做好事","喝咖啡","开班会","发通报","发牢骚","使性子","学音乐","学美术","吆喝","吃辣条","笑嘻嘻","扮可爱","吃钙片","吃麦片","吃披萨","上网","进观园","搬音响","解等式","玩卡牌","蹦跳跳","开汽车","作演讲","练书法","画漫画","开火车","做手工","读英语","吃牛肉","喝鸡汤","过马路","数绵羊","打电话","发短信","听广播","看漫画","变魔术","猜字谜","听音乐","学雷锋","切蔬菜","想问题","吃零食","想休息","拍气球","买玩具","开轿车","捏橘子","放鞭炮","吃水果","削水果","削苹果","削菠萝","削梨子","倒垃圾","改错误","吃醋","喝醋","上岸","吃面条","学数学","学语文","学地理","学英语","学历史","学生物","学物理","做装备","修钟表","修水管","修汽车","修桌子","修手机","修冰箱","吹口琴","弹琵琶","弹古筝","弹钢琴","吹口哨","弹吉他","吹笛子","弹三弦","敲排鼓","敲木鱼","敲渔鼓","撞铁钟","敲锣","拉二胡","拉马头琴","唱歌","唱山歌","唱高音","唱低音","听鸟叫","听摇滚","听爵士","听民谣","推箱子","看动漫","玩电脑","玩手机","逛街","拧瓶盖","打扫","擦椅子","削铅笔","背课文","背英语","念单词","念课文","打副本","放技能","躲技能","躲雨","跺脚","提水桶","涨工资","吃稀饭","喝冷饮","打篮球","踢足球","瞪眼睛","去砍树","闻花香","修管道","修电脑","擦桌子","咬铅笔","玩跳棋","看电视","看足球","看电影","看攻略","下象棋","下围棋","下棋","悔棋","上楼梯","坐电梯","下楼梯","看海报","喝稀饭","喝饮料","喝豆浆","喝果汁","喝豆奶","扔东西","扔垃圾","扔废纸","扔飞镖","丢飞机","开会","散步","游泳","健身","锻炼","迫降","变身","混合搭配不要全放在一起"
];

function generateRandomName() {
    const pick = arr => arr[Math.floor(Math.random() * arr.length)];

    const clean = s => (s || '').replace(/[^A-Za-z0-9]/g, '');
    const ucfirst = s => (s && s.length ? s[0].toUpperCase() + s.slice(1).toLowerCase() : s);

    const maybeLeet = (s) => {
        if (Math.random() < 0.08) {
            const map = { a: '4', e: '3', o: '0', i: '1', s: '5', t: '7', l: '1' };
            return s.split('').map(ch => map[ch.toLowerCase()] ?? ch).join('');
        }
        return s;
    };

    // construction modes — combine different pools to explode combinatorics
    const buildSyllable = () => {
        const s1 = pick(_segStart);
        const m = pick(_segMid);
        const s2 = pick(_segEnd);
        return clean(s1 + m + s2);
    };
    const buildAdjFragNoun = (adj, noun) => {
        const frag = Math.random() < 0.4 ? pick(_rnFragment) : '';
        const extra = Math.random() < 0.18 ? pick(_rnExtra) : '';
        return clean(adj + frag + extra + noun);
    };
    const buildFragFragNoun = () => {
        return clean(pick(_rnFragment) + pick(_rnFragment) + pick(_rnNoun));
    };
    const buildAdjExtra2Noun = (adj, noun) => clean(adj + pick(_rnExtra2) + noun);
    const buildThreeFragment = () => clean(pick(_rnFragment) + pick(_rnFragment) + pick(_rnFragment));
    const buildAdjNounSuffix = (adj, noun) => clean(adj + noun + pick(_rnSuffix));

    const strategies = [
        () => buildSyllable(),
        () => buildAdjFragNoun(pick(_rnAdj), pick(_rnNoun)),
        () => buildFragFragNoun(),
        () => buildAdjExtra2Noun(pick(_rnAdj), pick(_rnNoun)),
        () => buildThreeFragment(),
        () => buildAdjNounSuffix(pick(_rnAdj), pick(_rnNoun)),
        () => clean(pick(_rnAdj) + pick(_rnNoun)),
        () => clean(pick(_rnNoun) + pick(_rnFragment)),
        () => clean(pick(_rnFragment) + pick(_rnNoun)),
        () => clean(pick(_rnAdj).slice(0,3) + pick(_rnFragment) + pick(_rnNoun).slice(0,3))
    ];

    for (let i = 0; i < 5000; i++) {
        let cand = strategies[Math.floor(Math.random() * strategies.length)]();

        // random transformations
        if (Math.random() < 0.2) cand = ucfirst(cand);
        else if (Math.random() < 0.5) cand = cand.toLowerCase();
        else cand = cand.toUpperCase();

        cand = maybeLeet(cand).slice(0, 8);

        // ensure length between 6 and 8 by padding/truncating intelligently
        if (cand.length > 8) cand = cand.slice(0, 8);
        if (cand.length < 6) {
            // try to append fragments or suffixes
            let need = 6 - cand.length;
            let ext = '';
            if (Math.random() < 0.6) ext = pick(_rnFragment).slice(0, need);
            else ext = pick(_rnSuffix).slice(0, need);
            cand = (cand + ext).slice(0, 8);
        }

        if (cand.length >= 6 && cand.length <= 8) return cand;
    }

    // final fallback
    const base = clean(pick(_rnAdj).slice(0, 4) + pick(_rnNoun).slice(0, 2));
    const ts = Date.now().toString().slice(-3);
    let fb = (base + ts).slice(0, 8);
    if (fb.length < 6) fb = fb.padEnd(6, 'x');
    return fb;
}
