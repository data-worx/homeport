/* SPDX-License-Identifier: AGPL-3.0-or-later */
/* Design kit preview frame: theme from the URL hash or the kit page, height reported to the kit page. */
(function () {
  var root = document.documentElement;
  function setTheme(t) {
    if (t === 'light' || t === 'dark') root.setAttribute('data-theme', t);
    else root.setAttribute('data-theme', window.matchMedia && matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  }
  var m = location.hash.match(/theme=(light|dark)/);
  setTheme(m ? m[1] : 'system');
  window.addEventListener('message', function (e) { if (e.data && e.data.hpTheme) setTheme(e.data.hpTheme); });
  function report() {
    if (window.parent === window) return;
    var h = Math.max(document.documentElement.scrollHeight, document.body ? document.body.scrollHeight : 0);
    window.parent.postMessage({ hpHeight: h, hpFrame: window.name }, '*');
  }
  window.addEventListener('load', function () {
    report(); setTimeout(report, 250); setTimeout(report, 1000);
    if (window.ResizeObserver) new ResizeObserver(report).observe(document.body);
  });
})();
