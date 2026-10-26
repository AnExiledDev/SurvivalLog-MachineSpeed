// Machine Speed's web half. Runs in Root.html, the document every game panel sits in as an iframe. The brewing barrel,
// dye vat and shredder panels draw their times in whole game hours or minutes, which reads wrong for a sped-up machine.
// This wraps each panel's own draw function and, after the game has drawn, puts in the exact times the plugin sends
// through window.__slmachinespeed.set(). A machine the plugin sends null for is left exactly as the game draws it.
(function () {
  if (window.__slmachinespeed) return;

  var POLL_MS = 500;
  var MARK = '__slmachinespeedPatched';
  var state = { shredder: null, brew: null, dye: null };
  var lastJson = '';

  // Game seconds as "H:MM", with days in front when there are any. Whole minutes are counted the way the timer over
  // the machine counts them, so the panel and that timer always read the same.
  function clock(seconds, unitDay) {
    var minutes = Math.floor(Math.max(0, seconds) / 60);
    var days = Math.floor(minutes / 1440);
    var hours = Math.floor((minutes % 1440) / 60);
    var rest = minutes % 60;
    var hm = hours + ':' + (rest < 10 ? '0' : '') + rest;

    return days ? days + (unitDay || 'd') + ' ' + hm : hm;
  }

  function has(value) {
    return value !== null && value !== undefined;
  }

  // ===== Brewing barrel =====

  function fixBrew(w) {
    var brew = state.brew;
    var S = w.State;
    var text = w.i18nText;
    var num = w.document.getElementById('timeNum');

    if (!brew || !S || !text || !num) return;

    if (S.brewState === 1 && has(brew.remain)) {
      var template = text.remainTemplate || '';
      var at = template.indexOf('{0}');

      num.textContent = (at > 0 ? template.slice(0, at) : '') + clock(brew.remain, text.unitDay);
    } else if (S.brewState === 0 && has(brew.preview) && num.textContent !== '\u2014') {
      num.textContent = clock(brew.preview, text.unitDay);
    }
  }

  // ===== Dye vat =====

  function fixDye(w) {
    var dye = state.dye;
    var S = w.State;
    var text = w.i18nText;
    var doc = w.document;
    var label = doc.getElementById('tLabel');
    var num = doc.getElementById('tNum');
    var note = doc.getElementById('note');

    if (!dye || !S || !text || !label || !num || !note) return;

    if (S.state === w.ST_RUNNING) {
      if (has(dye.bucketRemain)) num.textContent = clock(dye.bucketRemain, text.unitDay);
      if (has(dye.totalRemain)) note.innerHTML = w.esc(w.tpl(text.totalRemainNote, clock(dye.totalRemain, text.unitDay))) + '<br>' + w.esc(text.lockedNote);

      return;
    }

    if (S.state === w.ST_DONE) return;

    if (label.textContent === text.timeEstimate) {
      if (has(dye.preview)) num.textContent = clock(dye.preview, text.unitDay);

      return;
    }

    if (has(dye.perBucket)) label.textContent = w.tpl(text.timePerBucket, clock(dye.perBucket, text.unitDay));
  }

  // ===== Shredder =====

  // The shredder's clock is drawn as game minutes and seconds, so a sped-up shred read like minutes of waiting and
  // jumped from 2:24 to 0:24 in a second. It is drawn as game hours and minutes instead, like the timer over the
  // machine and the other panels.
  function shredderClock(w, original) {
    return function (seconds) {
      if (!state.shredder) return original.apply(this, arguments);

      return clock(seconds);
    };
  }

  // The item tooltip's time is the game's minutes for that item; a sped-up job takes its seconds divided by the speed.
  function shredderTip(w, original) {
    return function (minutes) {
      if (!state.shredder || !(minutes > 0)) return original.apply(this, arguments);

      return clock(minutes * 60 / state.shredder);
    };
  }

  // ===== Pages =====

  var PAGES = [
    { key: 'brew', path: '/BrewPanel/', draw: 'renderFerment', fix: fixBrew },
    { key: 'dye', path: '/DyePanel/', draw: 'renderProd', fix: fixDye },
    { key: 'shredder', path: '/Shredder/', draw: 'renderProc', wraps: { mmss: shredderClock, fmtDur: shredderTip } }
  ];

  var KEYS = ['shredder', 'brew', 'dye'];
  var SEEN = '__slmachinespeedSeen';
  var patched = [];

  function patchPages() {
    var frames = document.getElementsByTagName('iframe');

    for (var i = 0; i < frames.length; i++) {
      var w = null;
      var path = null;

      try {
        w = frames[i].contentWindow;
        path = w ? String(w.location.pathname) : null;
      } catch (e) {}

      // A loaded page that is none of the panels is passed over until it goes to another address.
      if (path === null || w[MARK] || w[SEEN] === path) continue;

      var isPanel = false;

      for (var p = 0; p < PAGES.length; p++) {
        if (path.indexOf(PAGES[p].path) < 0) continue;

        isPanel = true;

        try {
          if (typeof w[PAGES[p].draw] === 'function') patch(w, PAGES[p]);
        } catch (e) {}
      }

      try {
        if (!isPanel && w.document && w.document.readyState === 'complete') w[SEEN] = path;
      } catch (e) {}
    }
  }

  function patch(w, page) {
    var original = w[page.draw];

    w[MARK] = true;

    if (page.wraps) {
      wrapAll(w, page);

      return;
    }

    w[page.draw] = function () {
      var result = original.apply(this, arguments);

      try {
        page.fix(w);
      } catch (e) {}

      return result;
    };
    patched.push({ win: w, draw: page.draw, key: page.key });

    // The game may have drawn the panel before it was wrapped.
    try {
      page.fix(w);
    } catch (e) {}
  }

  // A page whose helpers are wrapped is drawn again by its own draw function, which now goes through them.
  function wrapAll(w, page) {
    for (var name in page.wraps) {
      if (typeof w[name] === 'function') w[name] = page.wraps[name](w, w[name]);
    }

    patched.push({ win: w, draw: page.draw, key: page.key });

    try {
      w[page.draw]();
    } catch (e) {}
  }

  // Panels already open whose times changed are drawn again by the game's own function, which now ends with the fix.
  // A panel whose machine's times are the same is left alone.
  function redraw(changed) {
    var open = [];

    for (var i = 0; i < patched.length; i++) {
      var entry = patched[i];

      try {
        if (entry.win.closed || !entry.win.document) continue;

        open.push(entry);
        if (changed[entry.key]) entry.win[entry.draw]();
      } catch (e) {}
    }

    patched = open;
  }

  function changedKeys(before, after) {
    var changed = {};

    for (var i = 0; i < KEYS.length; i++) {
      changed[KEYS[i]] = JSON.stringify(before[KEYS[i]]) !== JSON.stringify(after[KEYS[i]]);
    }

    return changed;
  }

  function isAllOff() {
    return !state.shredder && !state.brew && !state.dye;
  }

  function set(next) {
    var json = JSON.stringify(next);

    if (json === lastJson) return;

    var before = state;

    lastJson = json;
    state = next || { shredder: null, brew: null, dye: null };
    patchPages();
    redraw(changedKeys(before, state));
  }

  // With every machine at the game's pace there is nothing to put in; set() looks for panels when that changes.
  setInterval(function () {
    if (isAllOff()) return;

    try {
      patchPages();
    } catch (e) {}
  }, POLL_MS);

  window.__slmachinespeed = { set: set };
})();
