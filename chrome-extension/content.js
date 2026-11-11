(() => {
  if (globalThis.__wirdReminderReady) return;
  globalThis.__wirdReminderReady = true;

  chrome.runtime.onMessage.addListener(message => {
    if (message?.type !== 'WIRD_REMINDER') return;
    showReminder(message);
  });

  function showReminder({ dhikr, duration }) {
    document.getElementById('__wird-reminder-host')?.remove();
    const host = document.createElement('div');
    host.id = '__wird-reminder-host';
    host.style.cssText = 'all:initial;position:fixed;inset:0;z-index:2147483647;pointer-events:none;direction:rtl;';
    const root = host.attachShadow({ mode: 'open' });
    const style = document.createElement('style');
    style.textContent = `
      *{box-sizing:border-box}
      .notice{position:fixed;top:18px;right:18px;width:min(390px,calc(100vw - 36px));padding:18px 20px 16px;border:1px solid #e6eadf;border-radius:18px;background:#fffdf7;color:#17372f;box-shadow:0 16px 48px #10292338;font:14px/1.6 'Segoe UI',Tahoma,sans-serif;pointer-events:auto;animation:arrive .28s ease-out}
      .top{display:flex;align-items:center;gap:11px}.mark{display:grid;place-items:center;flex:none;width:38px;height:38px;border-radius:13px;background:#174c3d;color:#fff;font-size:23px}.label{color:#829186;font-size:11px}.close{margin-right:auto;border:0;background:transparent;color:#78877e;font-size:22px;line-height:1;cursor:pointer;padding:5px}
      h2{margin:13px 0 4px;font-family:Georgia,'Times New Roman',serif;font-size:20px;font-weight:600;color:#214b39}.dhikr{margin:0;color:#537957;font-family:Georgia,'Times New Roman',serif;font-size:23px;font-weight:700}.note{display:block;margin-top:10px;color:#9aa49a;font-size:10px}
      @keyframes arrive{from{opacity:0;transform:translateY(-9px)}to{opacity:1;transform:translateY(0)}}
      @media(prefers-reduced-motion:reduce){.notice{animation:none}}
    `;
    const card = document.createElement('section');
    card.className = 'notice';
    card.setAttribute('role', 'status');
    const top = document.createElement('div'); top.className = 'top';
    const mark = document.createElement('span'); mark.className = 'mark'; mark.textContent = 'و';
    const label = document.createElement('span'); label.className = 'label'; label.textContent = 'وِرد · لحظةُ ذِكر';
    const close = document.createElement('button'); close.className = 'close'; close.type = 'button'; close.setAttribute('aria-label', 'إغلاق التذكير'); close.textContent = '×';
    close.addEventListener('click', () => host.remove());
    const title = document.createElement('h2'); title.textContent = 'ألا بذكر الله تطمئن القلوب';
    const text = document.createElement('p'); text.className = 'dhikr'; text.textContent = dhikr || 'سُبْحَانَ اللَّهِ وَبِحَمْدِهِ';
    const note = document.createElement('span'); note.className = 'note'; note.textContent = 'سيختفي التذكير تلقائيًا';
    top.append(mark, label, close); card.append(top, title, text, note); root.append(style, card); document.documentElement.append(host);
    window.setTimeout(() => host.remove(), Math.max(5, Number(duration) || 30) * 1000);
  }
})();

