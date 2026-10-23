// Machine Speed's Machines window. Runs in Root.html, the document every game page iframe sits in. Draws the window
// over all of them in the gold style the other hub windows share, and puts a button with a
// count badge in the HUD's tool column, built like the game's own buttons there. Every word and number comes from the
// plugin (OverviewView.cs); this script only draws them. Talks to the plugin through
// vuplex.postMessage("slmod|machinespeed|<verb>") and is driven by window.__slmachineoverview.
(function () {
  if (window.__slmachineoverview) return;

  var PREFIX = 'slmod|machinespeed|';
  var HUD_CLASS = 'mso-hud-btn';
  var HUD_STYLE_ID = 'mso-hud-style';
  // The Machines button's place in the HUD's tool column when Exiled's Toolkit lays it out in columns of four.
  var DOCK_ORDER = '50';
  var BUSY_TIMEOUT_MS = 5000;
  var TOAST_MS = 3500;
  var SVG_NS = 'http://www.w3.org/2000/svg';
  var CLOSE_MARK = String.fromCharCode(215);
  // A gear, drawn on the 24-unit grid the game's HUD icons use.
  var GEAR_PATH = 'M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z';

  var state = { data: emptyData(), isOpen: false, isLive: false, title: 'Machines', tip: '', badge: 0 };
  var ui = null;
  var shownKeys = '';
  var toastTimer = 0;
  var isBusy = false;
  var busyTimer = 0;
  var hudVersion = 0;
  var hasReportedPageAccess = false;
  var hasReportedHudPlaced = false;

  function emptyData() {
    return { title: 'Machines', atHome: '', needYou: '', points: '', pointsLabel: '', needsTitle: '', workingTitle: '', toClose: '', note: '', rows: [] };
  }

  function send(message) {
    try {
      if (window.vuplex) window.vuplex.postMessage(PREFIX + message);
    } catch (e) {}
  }

  // One press at a time: a double click would otherwise rush twice. Every press is answered by a status line, which
  // frees the buttons; the timer frees them anyway if that answer never comes.
  function sendAction(message) {
    if (isBusy) return;

    isBusy = true;
    clearTimeout(busyTimer);
    busyTimer = setTimeout(clearBusy, BUSY_TIMEOUT_MS);
    send(message);
  }

  function clearBusy() {
    isBusy = false;
    clearTimeout(busyTimer);
  }

  function el(tag, className, text) {
    var node = document.createElement(tag);

    if (className) node.className = className;
    if (text != null) node.textContent = text;

    return node;
  }

  function button(text, action, args, isEnabled, extraClass) {
    var node = el('button', 'mso-btn' + (extraClass ? ' ' + extraClass : ''), text);
    node.setAttribute('data-act', action);

    if (args != null) node.setAttribute('data-args', String(args));
    if (!isEnabled) node.disabled = true;

    return node;
  }

  // The gold hub look the other mod windows share: warm brown plate, gold hairline on top, gold section heads.
  var CSS = [
    '#mso-root{position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;background:rgba(10,6,2,.4);font-family:"Microsoft YaHei","PingFang SC",sans-serif;color:#f0e8d8;user-select:none}',
    '#mso-root *{box-sizing:border-box}',
    '.mso-plate{position:relative;width:min(1000px,94vw);height:min(830px,90vh);display:flex;flex-direction:column;gap:14px;padding:20px 24px;border-radius:4px;border:1px solid rgba(220,188,120,.28);background:radial-gradient(ellipse at 15% 0%,rgba(150,100,40,.3),transparent 55%),linear-gradient(180deg,#3a3129 0%,#2b241d 55%,#241e18 100%);box-shadow:0 30px 90px rgba(0,0,0,.7),inset 0 1px 0 rgba(255,230,180,.08)}',
    '.mso-plate::before{content:"";position:absolute;top:0;left:0;right:0;height:2px;background:linear-gradient(90deg,transparent 10%,#FFC83A 50%,transparent 90%);opacity:.6}',
    '.mso-head{display:flex;align-items:center;gap:16px}',
    '.mso-logo{flex:none;width:40px;height:40px;border-radius:4px;display:flex;align-items:center;justify-content:center;color:#2a1f10;background:linear-gradient(180deg,#ffd25a,#e8a91c);border:1px solid #ffd36a;box-shadow:0 0 14px rgba(255,200,58,.25)}',
    '.mso-logo svg{width:26px;height:26px;fill:currentColor}',
    '.mso-title{flex:1;min-width:0;font-size:26px;letter-spacing:6px;text-transform:uppercase;color:#fff;line-height:1}',
    '.mso-sub{display:block;font-size:12px;letter-spacing:1px;text-transform:none;color:#a89a82;margin-top:6px;min-height:14px}',
    '.mso-sub b{color:#FFC83A;font-weight:600}',
    '.mso-have{display:flex;align-items:center;gap:7px;font-size:13px;color:#a89a82;padding:6px 12px;border-radius:4px;background:rgba(20,14,8,.45);border:1px solid rgba(220,188,120,.2);white-space:nowrap}',
    '.mso-have b{font-size:17px;color:#FFC83A}',
    '.mso-pt{flex:none;width:11px;height:11px;fill:#FFC83A}',
    '.mso-btn{font:inherit;font-size:13px;color:#f0e8d8;padding:6px 12px;border-radius:4px;border:1px solid rgba(220,188,120,.2);background:linear-gradient(180deg,rgba(92,78,60,.9),rgba(66,55,42,.92));cursor:pointer;white-space:nowrap}',
    '.mso-btn:hover:not(:disabled){border-color:#FFC83A;background:linear-gradient(180deg,rgba(110,90,58,.95),rgba(80,64,40,.95))}',
    '.mso-btn.mso-x{width:30px;height:30px;padding:0;font-size:18px;line-height:28px;text-align:center}',
    '.mso-list{flex:1;min-height:0;overflow-y:auto;padding:4px 12px 12px;border-radius:4px;border:1px solid rgba(220,188,120,.2);background:linear-gradient(180deg,rgba(58,48,38,.75),rgba(44,36,28,.75))}',
    '.mso-list::-webkit-scrollbar{width:6px}',
    '.mso-list::-webkit-scrollbar-thumb{background:rgba(255,200,58,.35);border-radius:3px}',
    '.mso-sect{display:flex;align-items:center;gap:10px;padding:8px 4px 6px;font-size:15px;font-weight:700;letter-spacing:3px;text-transform:uppercase;color:#FFC83A;text-shadow:0 0 12px rgba(255,200,58,.35)}',
    '.mso-sect::before{content:"";flex:none;width:4px;height:16px;background:#FFC83A;box-shadow:0 0 8px rgba(255,200,58,.35)}',
    '.mso-sect::after{content:"";flex:1;height:1px;background:linear-gradient(90deg,rgba(255,200,58,.25),transparent)}',
    '.mso-sect span{order:2;font-size:12px;font-weight:400;letter-spacing:0;color:#7a6e5a;text-shadow:none}',
    '.mso-rows{display:flex;flex-direction:column;gap:5px}',
    '.mso-row{position:relative;display:grid;grid-template-columns:38px minmax(0,1fr) 110px 170px 118px;align-items:center;gap:14px;min-height:54px;padding:6px 12px 6px 14px;border-radius:4px;background:rgba(70,58,44,.55);border:1px solid rgba(220,188,120,.11)}',
    '.mso-row::before{content:"";position:absolute;left:-1px;top:-1px;bottom:-1px;width:3px;border-radius:4px 0 0 4px;background:transparent}',
    '.mso-row:hover{background:rgba(84,70,52,.7)}',
    '.mso-ic{width:38px;height:38px;border-radius:4px;background:rgba(20,14,8,.4);border:1px solid rgba(220,188,120,.11);padding:3px;display:flex;align-items:center;justify-content:center;color:rgba(255,200,58,.45)}',
    '.mso-ic img{width:100%;height:100%;object-fit:contain}',
    '.mso-ic svg{width:22px;height:22px;fill:currentColor}',
    '.mso-what{min-width:0;display:flex;flex-direction:column;gap:3px}',
    '.mso-name{font-size:15px;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.mso-product{font-size:12px;color:#a89a82;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.mso-prog{height:3px;margin-top:4px;border-radius:2px;background:rgba(255,240,210,.08);overflow:hidden;max-width:260px}',
    '.mso-prog i{display:block;height:100%;background:linear-gradient(90deg,#c99a2e,#FFC83A)}',
    '.mso-paused .mso-prog i{background:#7a6e5a}',
    '.mso-state{display:flex;align-items:center;gap:7px;font-size:13px;color:#cbbfa8;white-space:nowrap}',
    '.mso-state i{flex:none;width:8px;height:8px;border-radius:50%;background:#cbbfa8}',
    '.mso-finished .mso-state{color:#8cc56a}.mso-finished .mso-state i{background:#8cc56a;box-shadow:0 0 8px #8cc56a}.mso-finished::before{background:#8cc56a}',
    '.mso-outoffuel .mso-state{color:#e5654f}.mso-outoffuel .mso-state i{background:#e5654f;box-shadow:0 0 8px #e5654f}.mso-outoffuel::before{background:#e5654f}',
    '.mso-idle .mso-state{color:#FFC83A}.mso-idle .mso-state i{background:#FFC83A;box-shadow:0 0 8px rgba(255,200,58,.35)}.mso-idle::before{background:#FFC83A}',
    '.mso-running .mso-state i{background:#FFC83A;animation:mso-pulse 1.6s ease-in-out infinite}',
    '.mso-paused{opacity:.72}.mso-paused .mso-state{color:#7a6e5a}.mso-paused .mso-state i{background:#7a6e5a}',
    '@keyframes mso-pulse{50%{opacity:.35}}',
    '.mso-time{min-width:0;display:flex;flex-direction:column;gap:2px;white-space:nowrap;overflow:hidden}',
    '.mso-time b{font-size:17px;font-weight:600;color:#f0e8d8}',
    '.mso-time i{font-style:normal;font-size:12px;color:#a89a82;margin-left:6px}',
    '.mso-time .mso-alt{font-size:12px;color:#a89a82}',
    '.mso-time .mso-alt b{font-size:12px;font-weight:400;color:#a89a82}',
    '.mso-time .mso-alt i{color:#7a6e5a;margin-left:4px}',
    '.mso-time.mso-hint{font-size:13px;color:#cbbfa8}',
    '.mso-rush{display:flex;flex-direction:column;align-items:flex-end;gap:3px}',
    '.mso-btn.mso-rb{display:inline-flex;align-items:center;gap:8px;color:#FFC83A;font-weight:600;border-color:rgba(255,200,58,.55);background:rgba(255,200,58,.1)}',
    '.mso-btn.mso-rb:hover:not(:disabled){background:rgba(255,200,58,.2);border-color:#FFC83A}',
    '.mso-rb .mso-cost{display:inline-flex;align-items:center;gap:3px;padding-left:8px;border-left:1px solid rgba(255,200,58,.35)}',
    '.mso-btn.mso-rb:disabled{color:#a89a82;border-color:rgba(220,188,120,.2);background:rgba(255,240,210,.04);cursor:default}',
    '.mso-rb:disabled .mso-pt{fill:#7a6e5a}',
    '.mso-rb:disabled .mso-cost{border-left-color:rgba(220,188,120,.2)}',
    '.mso-why{font-size:11px;color:#7a6e5a;text-align:right;white-space:nowrap}',
    '.mso-empty{height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:14px;color:#a89a82;font-size:15px;text-align:center}',
    '.mso-empty svg{width:54px;height:54px;fill:rgba(255,200,58,.25)}',
    '.mso-foot{display:flex;align-items:center;gap:8px;font-size:13px;color:#a89a82;border-top:1px solid rgba(220,188,120,.11);padding-top:10px;min-height:18px}',
    '.mso-status{flex:1;min-width:0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;color:#b5e09a}',
    '.mso-kbd{font-size:12px;color:#cbbfa8;border:1px solid rgba(220,188,120,.4);border-bottom-width:2px;border-radius:4px;padding:0 6px}',
    '#mso-toast{position:fixed;left:50%;bottom:120px;transform:translateX(-50%);z-index:2147483001;pointer-events:none;display:none;max-width:640px;text-align:center;padding:10px 18px;border-radius:4px;border:1px solid rgba(255,200,58,.6);background:linear-gradient(180deg,rgba(64,52,40,.97),rgba(44,36,28,.97));color:#f0e8d8;font-family:"Microsoft YaHei","PingFang SC",sans-serif;font-size:15px;box-shadow:0 8px 30px rgba(0,0,0,.6)}'
  ].join('\n');

  function build() {
    if (ui && document.body.contains(ui.root)) return;

    addStyle();
    shownKeys = '';

    var root = el('div');
    root.id = 'mso-root';
    var plate = el('div', 'mso-plate');
    var head = el('div', 'mso-head');
    var logo = el('div', 'mso-logo');
    var title = el('div', 'mso-title');
    var titleText = el('span');
    var sub = el('span', 'mso-sub');
    var have = el('div', 'mso-have');
    var points = el('b');
    var pointsLabel = el('span');
    logo.appendChild(gear(document));
    title.appendChild(titleText);
    title.appendChild(sub);
    have.appendChild(diamond());
    have.appendChild(points);
    have.appendChild(pointsLabel);
    head.appendChild(logo);
    head.appendChild(title);
    head.appendChild(have);
    head.appendChild(button(CLOSE_MARK, 'close', null, true, 'mso-x'));

    var list = el('div', 'mso-list');
    var foot = el('div', 'mso-foot');
    var status = el('div', 'mso-status');
    var toClose = el('span');
    foot.appendChild(status);
    foot.appendChild(el('span', 'mso-kbd', 'Esc'));
    foot.appendChild(toClose);

    plate.appendChild(head);
    plate.appendChild(list);
    plate.appendChild(foot);
    root.appendChild(plate);

    var toast = el('div');
    toast.id = 'mso-toast';

    document.body.appendChild(root);
    document.body.appendChild(toast);

    root.addEventListener('click', onClick);
    // Clicks inside the window must not reach the game pages underneath; the mask itself closes nothing on purpose.
    root.addEventListener('mousedown', function (e) { e.stopPropagation(); });

    ui = { root: root, title: titleText, sub: sub, have: have, points: points, pointsLabel: pointsLabel, list: list, status: status, toClose: toClose, toast: toast };
  }

  // The style outlives a lost window root, so a rebuild must not stack another copy of it.
  function addStyle() {
    if (document.getElementById('mso-style')) return;

    var style = el('style');
    style.id = 'mso-style';
    style.textContent = CSS;
    document.head.appendChild(style);
  }

  function onClick(e) {
    var target = e.target.closest('[data-act]');

    if (!target) return;

    e.stopPropagation();

    if (target.disabled) return;

    var act = target.getAttribute('data-act');
    var args = target.getAttribute('data-args');

    // Shutting the window gets no status line back, so it must not hold the buttons busy.
    if (act === 'close') {
      send(act);

      return;
    }

    sendAction(args ? act + '|' + args : act);
  }

  function render() {
    var data = state.data;
    setText(ui.title, data.title || '');
    renderSub(data.atHome || '', data.needYou || '');
    setText(ui.points, data.points || '');
    setText(ui.pointsLabel, data.pointsLabel || '');
    ui.have.style.display = data.points ? '' : 'none';
    setText(ui.toClose, data.toClose || '');
    renderList(data);
  }

  // "10 at home, 4 need you" with a middle dot between, the second half in gold.
  function renderSub(atHome, needYou) {
    var shown = atHome + '|' + needYou;

    if (ui.sub.getAttribute('data-shown') === shown) return;

    ui.sub.setAttribute('data-shown', shown);
    ui.sub.textContent = atHome;

    if (!needYou) return;

    ui.sub.appendChild(document.createTextNode(atHome ? ' ' + String.fromCharCode(183) + ' ' : ''));
    ui.sub.appendChild(el('b', '', needYou));
  }

  // When the same machines are listed in the same places, each row is updated where it stands, so the list does not
  // flicker or lose the pointer's hover every second. Any other change draws the list afresh.
  function renderList(data) {
    var rows = data.rows || [];
    var keys = rows.map(function (r) { return r.group + ':' + r.key; }).join(',') + '|' + (data.needsTitle || '') + '|' + (data.workingTitle || '');

    if (rows.length > 0 && keys === shownKeys) {
      var nodes = ui.list.querySelectorAll('.mso-row');

      for (var i = 0; i < rows.length; i++) updateRow(nodes[i], rows[i]);

      return;
    }

    var scroll = ui.list.scrollTop;
    shownKeys = keys;
    ui.list.textContent = '';

    if (rows.length === 0) {
      var empty = el('div', 'mso-empty');
      empty.appendChild(gear(document));
      empty.appendChild(el('div', '', data.note || ''));
      ui.list.appendChild(empty);

      return;
    }

    var fragment = document.createDocumentFragment();

    addSection(fragment, data.needsTitle, rows.filter(function (r) { return r.group === 'needs'; }));
    addSection(fragment, data.workingTitle, rows.filter(function (r) { return r.group !== 'needs'; }));
    ui.list.appendChild(fragment);
    ui.list.scrollTop = scroll;
  }

  function addSection(parent, title, rows) {
    if (rows.length === 0) return;

    var head = el('div', 'mso-sect', title || '');
    var list = el('div', 'mso-rows');
    head.appendChild(el('span', '', String(rows.length)));
    rows.forEach(function (r) { list.appendChild(newRow(r)); });
    parent.appendChild(head);
    parent.appendChild(list);
  }

  function newRow(r) {
    var node = el('div');
    var what = el('div', 'mso-what');
    var stateCell = el('div', 'mso-state');
    var bar = el('div', 'mso-prog');
    bar.appendChild(el('i'));
    what.appendChild(el('div', 'mso-name'));
    what.appendChild(el('div', 'mso-product'));
    what.appendChild(bar);
    stateCell.appendChild(el('i'));
    stateCell.appendChild(el('span'));
    node.appendChild(el('div', 'mso-ic'));
    node.appendChild(what);
    node.appendChild(stateCell);
    node.appendChild(el('div', 'mso-time'));
    node.appendChild(el('div', 'mso-rush'));
    updateRow(node, r);

    return node;
  }

  function updateRow(node, r) {
    var what = node.children[1];
    var bar = what.children[2];
    var hasBar = typeof r.progress === 'number';
    node.className = 'mso-row mso-' + r.state;
    updateIcon(node.children[0], r.icon || '');
    setText(what.children[0], r.name);
    setText(what.children[1], r.product);
    what.children[1].style.display = r.product ? '' : 'none';
    bar.style.display = hasBar ? '' : 'none';

    if (hasBar) bar.firstChild.style.width = Math.round(Math.max(0, Math.min(1, r.progress)) * 100) + '%';

    setText(node.children[2].children[1], r.stateText);
    updateTime(node.children[3], r);
    updateRush(node.children[4], r);
  }

  // A missing picture falls back to the gear rather than a broken image.
  function updateIcon(cell, icon) {
    if (cell.getAttribute('data-shown') === icon) return;

    cell.setAttribute('data-shown', icon);
    cell.textContent = '';

    if (!icon) {
      cell.appendChild(gear(document));

      return;
    }

    var img = el('img');
    img.alt = '';
    img.onerror = function () {
      if (img.parentNode !== cell) return;

      cell.textContent = '';
      cell.appendChild(gear(document));
    };
    img.src = fromRoot(icon);
    cell.appendChild(img);
  }

  // Icon paths are relative to Root.html's folder (WebUI); made absolute so they never resolve against anything else.
  function fromRoot(path) {
    var href = String(location.href || '');

    if (/^[a-z]+:|^\//i.test(path) || href.indexOf('/') < 0) return path;

    return href.slice(0, href.lastIndexOf('/') + 1) + path;
  }

  // The big time with its word, the other time small under it; a row that needs the player says what to do instead.
  function updateTime(cell, r) {
    var time = r.time;
    var shown = time ? timeKey(time.main) + '|' + timeKey(time.other) + '|' + (r.hint || '') : 'hint|' + (r.hint || '');

    if (cell.getAttribute('data-shown') === shown) return;

    cell.setAttribute('data-shown', shown);
    cell.textContent = '';
    cell.className = 'mso-time' + (time ? '' : ' mso-hint');

    if (!time) {
      cell.textContent = r.hint || '';

      return;
    }

    cell.appendChild(timeLine('', time.main));

    if (time.other) cell.appendChild(timeLine('mso-alt', time.other));
    else if (r.hint) cell.appendChild(el('span', 'mso-alt', r.hint));
  }

  function timeKey(t) {
    return t ? t.value + ' ' + t.word : '';
  }

  function timeLine(className, t) {
    var line = el('span', className);
    line.appendChild(el('b', '', t.value));
    line.appendChild(el('i', '', t.word));

    return line;
  }

  // The web view draws no hover tips, so a Rush the player cannot afford says why underneath it.
  function updateRush(cell, r) {
    var rush = r.rush;
    var shown = rush ? r.key + '|' + rush.label + '|' + rush.cost + '|' + rush.ok + '|' + rush.tip + '|' + rush.why : '';

    if (cell.getAttribute('data-shown') === shown) return;

    cell.setAttribute('data-shown', shown);
    cell.textContent = '';

    if (!rush) return;

    var node = button(rush.label, 'rush', r.kind + '|' + r.id, rush.ok, 'mso-rb');
    var cost = el('span', 'mso-cost');
    cost.appendChild(diamond());
    cost.appendChild(document.createTextNode(rush.cost));
    node.appendChild(cost);
    node.title = rush.tip;
    cell.appendChild(node);

    if (!rush.ok && rush.why) cell.appendChild(el('div', 'mso-why', rush.why));
  }

  function setText(node, text) {
    if (node.textContent !== text) node.textContent = text;
  }

  // The survival points mark: a small gold diamond.
  function diamond() {
    var svg = document.createElementNS(SVG_NS, 'svg');
    var path = document.createElementNS(SVG_NS, 'path');

    svg.setAttribute('class', 'mso-pt');
    svg.setAttribute('viewBox', '0 0 12 12');
    path.setAttribute('d', 'M6 0l6 6-6 6-6-6z');
    svg.appendChild(path);

    return svg;
  }

  function open(data) {
    build();
    state.data = data || state.data;
    state.isOpen = true;
    ui.status.textContent = '';
    ui.toast.style.display = 'none';
    ui.root.style.display = 'flex';
    render();
  }

  function setData(data) {
    state.data = data || state.data;

    if (state.isOpen && ui) render();
  }

  function close() {
    state.isOpen = false;
    clearBusy();

    if (ui) ui.root.style.display = 'none';
  }

  function status(text) {
    build();
    clearBusy();

    if (state.isOpen) {
      ui.status.textContent = text;

      return;
    }

    // An empty status only clears the line; as a toast it would be an empty box.
    if (!text) {
      ui.toast.style.display = 'none';

      return;
    }

    ui.toast.textContent = text;
    ui.toast.style.display = 'block';
    clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { ui.toast.style.display = 'none'; }, TOAST_MS);
  }

  // Whether a game is loaded (the HUD button is only shown then), and the button's tip in the game's language.
  function live(isLive, title, tip) {
    state.isLive = !!isLive;
    state.title = title || 'Machines';
    state.tip = tip || '';
    hudVersion++;
    patchPagesSafely();
  }

  // The number on the HUD button: machines that are idle, finished or out of fuel.
  function badge(count) {
    state.badge = Math.max(0, count | 0);
    hudVersion++;
    patchPagesSafely();
  }

  // ---------- HUD button ----------

  var HUD_CSS = [
    '.mso-hud-btn{position:relative}',
    '.mso-hud-btn .mso-hud-badge{position:absolute;top:-6px;right:-6px;min-width:18px;height:18px;padding:0 5px;border-radius:9px;background:#e53935;color:#fff;font-size:11px;font-weight:700;line-height:18px;text-align:center;pointer-events:none;box-shadow:0 1px 4px rgba(0,0,0,.6)}'
  ].join('\n');

  // The HUD is a same-origin iframe, so its document can be reached from here; polled because the game creates and
  // drops that iframe whenever it likes.
  function patchPages() {
    var frames = document.querySelectorAll('iframe');

    for (var i = 0; i < frames.length; i++) {
      var src = frames[i].src || '';

      if (!/CoreUI1\.html/.test(src)) continue;

      var doc = null;

      try {
        doc = frames[i].contentDocument;
      } catch (e) {}

      if (!doc) {
        if (!hasReportedPageAccess) send('diag|cannot reach the HUD document (root ' + location.protocol + ', iframe ' + src + ')');
        hasReportedPageAccess = true;
        continue;
      }

      placeHudButton(doc);
    }
  }

  function patchPagesSafely() {
    try {
      patchPages();
    } catch (e) {}
  }

  // First in the tool column: the column is anchored at the bottom, so the first child sits on top and the game's own
  // buttons do not move. The column hides itself in build mode, and the button with it.
  function placeHudButton(doc) {
    var column = doc.querySelector('.vertical-tools');
    var existing = doc.querySelector('.' + HUD_CLASS);

    if (!column || !state.isLive) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.parentNode === column && existing.getAttribute('data-version') === String(hudVersion)) return;
    if (existing) existing.remove();

    addHudStyle(doc);
    var node = hudButton(doc);

    node.setAttribute('data-etk-dock', DOCK_ORDER);
    column.insertBefore(node, column.firstChild);

    if (!hasReportedHudPlaced) send('diag|HUD button placed in the tool column');
    hasReportedHudPlaced = true;
  }

  function addHudStyle(doc) {
    if (doc.getElementById(HUD_STYLE_ID)) return;

    var style = doc.createElement('style');
    style.id = HUD_STYLE_ID;
    style.textContent = HUD_CSS;
    (doc.head || doc.body).appendChild(style);
  }

  // The same markup as the game's bag button: its classes, an svg icon, and the hover tip its stylesheet shows. The
  // HUD page watches its body for new buttons and tells the game where they are, so a click on this one reaches it.
  function hudButton(doc) {
    var node = doc.createElement('button');
    node.className = 'ui-btn icon-sq interactive-btn has-entry-tip ' + HUD_CLASS;
    node.setAttribute('data-interactive', '');
    node.setAttribute('data-version', String(hudVersion));
    node.appendChild(gear(doc));

    if (state.badge > 0) {
      var count = doc.createElement('span');
      count.className = 'mso-hud-badge';
      count.textContent = state.badge > 99 ? '99+' : String(state.badge);
      node.appendChild(count);
    }

    node.appendChild(entryTip(doc));
    node.addEventListener('click', function (e) {
      e.stopPropagation();
      send('toggle');
    });

    return node;
  }

  function gear(doc) {
    var svg = doc.createElementNS(SVG_NS, 'svg');
    var path = doc.createElementNS(SVG_NS, 'path');

    svg.setAttribute('class', 'icon-svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    path.setAttribute('d', GEAR_PATH);
    svg.appendChild(path);

    return svg;
  }

  function entryTip(doc) {
    var tip = doc.createElement('div');
    var title = doc.createElement('div');
    var body = doc.createElement('div');

    tip.className = 'entry-tip';
    title.className = 'entry-tip-title';
    title.textContent = state.title;
    body.className = 'entry-tip-body';
    body.textContent = state.tip;
    tip.appendChild(title);
    tip.appendChild(body);

    return tip;
  }

  setInterval(patchPagesSafely, 400);

  window.__slmachineoverview = { open: open, setData: setData, close: close, status: status, live: live, badge: badge };
  send('ready');
})();
