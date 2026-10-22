// Machine Speed's Machines window. Runs in Root.html, the document every game page iframe sits in. Draws the window
// over all of them, styled like the other mod panels with the game's own colours and fonts, and puts a button with a
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
    return { title: 'Machines', cols: ['', '', '', ''], points: '', note: '', rows: [] };
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

  var CSS = [
    '#mso-root{position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;background:rgba(0,0,0,.4);font-family:"Microsoft YaHei","PingFang SC",sans-serif;color:#eee;user-select:none}',
    '#mso-root *{box-sizing:border-box}',
    '.mso-plate{width:min(900px,92vw);height:min(680px,86vh);display:flex;flex-direction:column;gap:12px;padding:18px 20px;border-radius:10px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(30,30,33,.92),rgba(18,18,20,.94));box-shadow:0 10px 40px rgba(0,0,0,.6)}',
    '.mso-head{display:flex;align-items:baseline;gap:14px}',
    '.mso-title{font-size:18px;font-weight:600;letter-spacing:1px;text-shadow:0 2px 6px rgba(0,0,0,.6)}',
    '.mso-sub{font-size:13px;color:#EBA308;flex:1}',
    '.mso-btn.mso-x{width:30px;height:30px;padding:0;font-size:18px;line-height:28px}',
    '.mso-grid{display:grid;grid-template-columns:minmax(0,2.2fr) minmax(0,1fr) minmax(0,1.3fr) 200px;align-items:center;gap:12px}',
    '.mso-cols{padding:0 16px;font-size:12px;color:rgba(238,238,238,.55)}',
    '.mso-list{flex:1;min-height:0;overflow-y:auto;display:flex;flex-direction:column;gap:6px;padding:6px;background:rgba(10,10,12,.72);border:1px solid rgba(255,255,255,.14);border-radius:8px}',
    '.mso-row{padding:8px 10px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid transparent}',
    '.mso-row:hover{background:rgba(255,255,255,.06)}',
    '.mso-row.mso-finished{border-color:rgba(124,179,66,.45)}',
    '.mso-row.mso-outoffuel{border-color:rgba(229,57,53,.45)}',
    '.mso-row.mso-idle{border-color:rgba(235,163,8,.35)}',
    '.mso-what{min-width:0;display:flex;flex-direction:column;gap:3px}',
    '.mso-name{font-size:14px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}',
    '.mso-product{font-size:12px;color:rgba(238,238,238,.65);overflow:hidden;text-overflow:ellipsis;white-space:nowrap}',
    '.mso-state{justify-self:start;font-size:11px;padding:2px 7px;border-radius:5px;background:rgba(255,255,255,.1);color:rgba(238,238,238,.85);white-space:nowrap}',
    '.mso-finished .mso-state{background:rgba(124,179,66,.24);color:#aed581}',
    '.mso-outoffuel .mso-state{background:rgba(229,57,53,.28);color:#ff8a80}',
    '.mso-idle .mso-state{background:rgba(255,193,7,.24);color:#EBA308}',
    '.mso-paused .mso-state{background:rgba(255,255,255,.06);color:rgba(238,238,238,.55)}',
    '.mso-time{font-size:13px;color:rgba(238,238,238,.8);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.mso-rush{display:flex;flex-direction:column;align-items:flex-end;gap:3px}',
    '.mso-why{font-size:11px;color:rgba(238,238,238,.55);text-align:right}',
    '.mso-empty{padding:20px;color:rgba(238,238,238,.55);font-size:14px;text-align:center}',
    '.mso-btn{font:inherit;font-size:13px;color:#dde2f0;padding:6px 11px;border-radius:7px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(58,58,60,.9),rgba(38,38,40,.92));cursor:pointer;white-space:nowrap}',
    '.mso-btn:hover:not(:disabled){background:linear-gradient(180deg,rgba(72,66,48,.92),rgba(50,46,32,.95));border-color:rgba(255,193,7,.75)}',
    '.mso-btn:disabled{opacity:.4;cursor:default}',
    '.mso-foot{display:flex;font-size:13px;color:rgba(238,238,238,.7);min-height:18px}',
    '.mso-status{flex:1}',
    '#mso-toast{position:fixed;left:50%;bottom:120px;transform:translateX(-50%);z-index:2147483001;pointer-events:none;display:none;max-width:640px;text-align:center;padding:10px 18px;border-radius:8px;border:1px solid rgba(255,193,7,.75);background:linear-gradient(180deg,rgba(30,30,33,.94),rgba(18,18,20,.96));color:#eee;font-family:"Microsoft YaHei","PingFang SC",sans-serif;font-size:14px;box-shadow:0 6px 20px rgba(0,0,0,.6)}'
  ].join('\n');

  function build() {
    if (ui && document.body.contains(ui.root)) return;

    addStyle();
    shownKeys = '';

    var root = el('div');
    root.id = 'mso-root';
    var plate = el('div', 'mso-plate');
    var head = el('div', 'mso-head');
    var title = el('div', 'mso-title');
    var sub = el('div', 'mso-sub');
    head.appendChild(title);
    head.appendChild(sub);
    head.appendChild(button(CLOSE_MARK, 'close', null, true, 'mso-x'));

    var cols = el('div', 'mso-grid mso-cols');
    var list = el('div', 'mso-list');
    var foot = el('div', 'mso-foot');
    var status = el('div', 'mso-status');
    foot.appendChild(status);

    plate.appendChild(head);
    plate.appendChild(cols);
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

    ui = { root: root, title: title, sub: sub, cols: cols, list: list, status: status, toast: toast };
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
    ui.title.textContent = data.title;
    ui.sub.textContent = data.points || '';
    renderCols(data.cols || []);
    renderList(data);
  }

  function renderCols(cols) {
    ui.cols.textContent = '';

    for (var i = 0; i < 4; i++) ui.cols.appendChild(el('div', '', cols[i] || ''));
  }

  // When the same machines are listed in the same order, each row is updated where it stands, so the list does not
  // flicker or lose the pointer's hover every second. Any other change draws the list afresh.
  function renderList(data) {
    var rows = data.rows || [];
    var keys = rows.map(function (r) { return r.key; }).join(',');

    if (rows.length > 0 && keys === shownKeys) {
      for (var i = 0; i < rows.length; i++) updateRow(ui.list.children[i], rows[i]);

      return;
    }

    var scroll = ui.list.scrollTop;
    shownKeys = keys;
    ui.list.textContent = '';

    if (rows.length === 0) {
      ui.list.appendChild(el('div', 'mso-empty', data.note || ''));

      return;
    }

    var fragment = document.createDocumentFragment();

    rows.forEach(function (r) { fragment.appendChild(newRow(r)); });
    ui.list.appendChild(fragment);
    ui.list.scrollTop = scroll;
  }

  function newRow(r) {
    var node = el('div', 'mso-row mso-grid');
    var what = el('div', 'mso-what');
    what.appendChild(el('div', 'mso-name'));
    what.appendChild(el('div', 'mso-product'));
    node.appendChild(what);
    node.appendChild(el('div', 'mso-state'));
    node.appendChild(el('div', 'mso-time'));
    node.appendChild(el('div', 'mso-rush'));
    updateRow(node, r);

    return node;
  }

  function updateRow(node, r) {
    var what = node.children[0];
    node.className = 'mso-row mso-grid mso-' + r.state;
    setText(what.children[0], r.name);
    setText(what.children[1], r.product);
    what.children[1].style.display = r.product ? '' : 'none';
    setText(node.children[1], r.stateText);
    setText(node.children[2], r.time);
    updateRush(node.children[3], r);
  }

  function setText(node, text) {
    if (node.textContent !== text) node.textContent = text;
  }

  // The web view draws no hover tips, so a Rush the player cannot afford says why underneath it.
  function updateRush(cell, r) {
    var rush = r.rush;
    var shown = rush ? r.key + '|' + rush.label + '|' + rush.ok + '|' + rush.tip : '';

    if (cell.getAttribute('data-shown') === shown) return;

    cell.setAttribute('data-shown', shown);
    cell.textContent = '';

    if (!rush) return;

    var node = button(rush.label, 'rush', r.kind + '|' + r.id, rush.ok, '');
    node.title = rush.tip;
    cell.appendChild(node);

    if (!rush.ok) cell.appendChild(el('div', 'mso-why', rush.tip));
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
