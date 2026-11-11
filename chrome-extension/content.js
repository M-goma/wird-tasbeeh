(() => {
  if (globalThis.__wirdReminderReady) return;
  globalThis.__wirdReminderReady = true;

  chrome.runtime.onMessage.addListener(message => {
    if (message?.type !== 'WIRD_REMINDER') return;
    showReminder(message);
  });

  function showReminder({ dhikr, duration, darkMode }) {
    document.getElementById('__wird-reminder-host')?.remove();
    const host = document.createElement('div');
    host.id = '__wird-reminder-host';
    host.style.cssText = 'all:initial;position:fixed;inset:0;z-index:2147483647;pointer-events:none;direction:rtl;';
    const root = host.attachShadow({ mode: 'open' });
    const style = document.createElement('style');
    style.textContent = `
      *{box-sizing:border-box}
      .notice{position:fixed;top:12px;right:12px;width:min(320px,calc(100vw - 24px));padding:12px 14px;border:1px solid #e6eadf;border-radius:15px;background:#fffdf7;color:#17372f;box-shadow:0 12px 32px #10292338;font:13px/1.5 'Segoe UI',Tahoma,sans-serif;pointer-events:auto;animation:arrive .28s ease-out}.notice.dark{background:#1b2a24;border-color:#34463b;color:#e5eee8}.notice.dark h2{color:#dcebe0}.notice.dark .dhikr{color:#b1d3b5}.notice.dark .label,.notice.dark .note{color:#b2c3b6}
      .top{display:flex;align-items:center;gap:9px}.mark{display:grid;place-items:center;flex:none;width:30px;height:30px;border-radius:10px;background:#174c3d;color:#fff;font-size:19px}.label{color:#829186;font-size:10px}.close{margin-right:auto;border:0;background:transparent;color:#78877e;font-size:20px;line-height:1;cursor:pointer;padding:3px}
      .dhikr{display:block;width:100%;margin:8px 0 0;color:#537957;font-family:Georgia,'Times New Roman',serif;font-size:18px;font-weight:700;line-height:1.6;white-space:normal;overflow-wrap:anywhere}.note{display:block;margin-top:6px;color:#9aa49a;font-size:9px}
      @keyframes arrive{from{opacity:0;transform:translateY(-9px)}to{opacity:1;transform:translateY(0)}}
      @media(prefers-reduced-motion:reduce){.notice{animation:none}}
    `;
    const card = document.createElement('section');
    card.className = 'notice';
    if (darkMode) card.classList.add('dark');
    card.setAttribute('role', 'status');
    const top = document.createElement('div'); top.className = 'top';
    const mark = document.createElement('span'); mark.className = 'mark'; mark.textContent = 'و';
    const label = document.createElement('span'); label.className = 'label'; label.textContent = 'وِرد · لحظةُ ذِكر';
    const close = document.createElement('button'); close.className = 'close'; close.type = 'button'; close.setAttribute('aria-label', 'إغلاق التذكير'); close.textContent = '×';
    close.addEventListener('click', () => host.remove());
    const text = document.createElement('p'); text.className = 'dhikr'; text.textContent = dhikr || 'سُبْحَانَ اللَّهِ وَبِحَمْدِهِ';
    const note = document.createElement('span'); note.className = 'note'; note.textContent = 'سيختفي التذكير تلقائيًا';
    top.append(mark, label, close); card.append(top, text, note); root.append(style, card); document.documentElement.append(host);
    window.setTimeout(() => host.remove(), Math.max(5, Number(duration) || 30) * 1000);
  }
})();