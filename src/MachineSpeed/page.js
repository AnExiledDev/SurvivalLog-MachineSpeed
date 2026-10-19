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

  // Game seconds as "H:MM", with days in front when there are any, rounded up so a running job never reads 0:00.
  function clock(seconds, unitDay) {
    var minutes = Math.ceil(Math.max(0, seconds) / 60);
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

  // The item tooltip's time is the game's minutes for that item; a sped-up job takes its seconds divided by the speed.
  function shredderTime(w, original) {
    return function (minutes) {
      if (!state.shredder || !(minutes > 0)) return original.apply(this, arguments);

      return w.mmss(Math.max(1, Math.round(minutes * 60 / state.shredder)));
    };
  }

  // ===== Pages =====

  var PAGES = [
    { path: '/BrewPanel/', draw: 'renderFerment', fix: fixBrew },
    { path: '/DyePanel/', draw: 'renderProd', fix: fixDye },
    { path: '/Shredder/', draw: 'fmtDur', wrap: shredderTime }
  ];

  var patched = [];

  function patchPages() {
    var frames = document.getElementsByTagName('iframe');

    for (var i = 0; i < frames.length; i++) {
      var w = null;

      try {
        w = frames[i].contentWindow;
      } catch (e) {}

      if (!w || w[MARK]) continue;

      for (var p = 0; p < PAGES.length; p++) {
        try {
          if (String(w.location.pathname).indexOf(PAGES[p].path) >= 0 && typeof w[PAGES[p].draw] === 'function') patch(w, PAGES[p]);
        } catch (e) {}
      }
    }
  }

  function patch(w, page) {
    var original = w[page.draw];

    w[MARK] = true;

    if (page.wrap) {
      w[page.draw] = page.wrap(w, original);

      return;
    }

    w[page.draw] = function () {
      var result = original.apply(this, arguments);

      try {
        page.fix(w);
      } catch (e) {}

      return result;
    };
    patched.push({ win: w, draw: page.draw });

    // The game may have drawn the panel before it was wrapped.
    try {
      page.fix(w);
    } catch (e) {}
  }

  // Panels already open are drawn again by the game's own function, which now ends with the fix.
  function redraw() {
    var open = [];

    for (var i = 0; i < patched.length; i++) {
      var entry = patched[i];

      try {
        if (entry.win.closed || !entry.win.document) continue;

        open.push(entry);
        entry.win[entry.draw]();
      } catch (e) {}
    }

    patched = open;
  }

  function set(next) {
    var json = JSON.stringify(next);

    if (json === lastJson) return;

    lastJson = json;
    state = next || { shredder: null, brew: null, dye: null };
    patchPages();
    redraw();
  }

  setInterval(function () {
    try {
      patchPages();
    } catch (e) {}
  }, POLL_MS);

  window.__slmachinespeed = { set: set };
})();
