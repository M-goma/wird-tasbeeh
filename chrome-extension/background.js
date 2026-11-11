const DEFAULTS = { interval: 5, duration: 30, dhikr: 'سُبْحَانَ اللَّهِ وَبِحَمْدِهِ', activeFrom: '08:00', activeUntil: '22:00', darkMode: false };
const ALARM = 'wird-reminder';
const ADHKAR = ['سُبْحَانَ اللَّهِ وَبِحَمْدِهِ','سُبْحَانَ اللَّهِ','الْحَمْدُ لِلَّهِ','اللَّهُ أَكْبَرُ','لَا إِلَهَ إِلَّا اللَّهُ','أَسْتَغْفِرُ اللَّهَ','لَا حَوْلَ وَلَا قُوَّةَ إِلَّا بِاللَّهِ','سُبْحَانَ اللَّهِ وَالْحَمْدُ لِلَّهِ وَلَا إِلَهَ إِلَّا اللَّهُ وَاللَّهُ أَكْبَرُ','اللَّهُمَّ صَلِّ عَلَى مُحَمَّدٍ','سُبْحَانَ اللَّهِ الْعَظِيمِ وَبِحَمْدِهِ','لَا إِلَهَ إِلَّا اللَّهُ وَحْدَهُ لَا شَرِيكَ لَهُ، لَهُ الْمُلْكُ وَلَهُ الْحَمْدُ، وَهُوَ عَلَى كُلِّ شَيْءٍ قَدِيرٌ','أَسْتَغْفِرُ اللَّهَ الْعَظِيمَ وَأَتُوبُ إِلَيْهِ','حَسْبِيَ اللَّهُ لَا إِلَهَ إِلَّا هُوَ، عَلَيْهِ تَوَكَّلْتُ وَهُوَ رَبُّ الْعَرْشِ الْعَظِيمِ','اللَّهُمَّ أَعِنِّي عَلَى ذِكْرِكَ وَشُكْرِكَ وَحُسْنِ عِبَادَتِكَ'];

async function settings() {
  return { ...DEFAULTS, ...await chrome.storage.local.get(DEFAULTS) };
}

async function schedule() {
  const { interval } = await settings();
  await chrome.alarms.clear(ALARM);
  chrome.alarms.create(ALARM, { delayInMinutes: interval, periodInMinutes: interval });
}

async function remindActiveTab() {
  const current = await settings();
  const now = new Date().getHours() * 60 + new Date().getMinutes();
  const toMinutes = value => { const [hours, minutes] = String(value || '00:00').split(':').map(Number); return hours * 60 + minutes; };
  const from = toMinutes(current.activeFrom), until = toMinutes(current.activeUntil);
  const active = from <= until ? now >= from && now < until : now >= from || now < until;
  if (!active) return;
  const currentIndex = ADHKAR.indexOf(current.dhikr);
  const nextDhikr = currentIndex < 0 ? ADHKAR[0] : ADHKAR[(currentIndex + 1) % ADHKAR.length];
  await chrome.storage.local.set({ dhikr: nextDhikr });
  const [tab] = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
  if (!tab?.id || !/^https?:\/\//i.test(tab.url || '')) return;
  const message = { type: 'WIRD_REMINDER', dhikr: nextDhikr, duration: current.duration, darkMode: current.darkMode };
  try {
    await chrome.tabs.sendMessage(tab.id, message);
  } catch {
    try {
      await chrome.scripting.executeScript({ target: { tabId: tab.id }, files: ['content.js'] });
      await chrome.tabs.sendMessage(tab.id, message);
    } catch { /* Browser-internal and restricted pages cannot host extensions. */ }
  }
}

chrome.runtime.onInstalled.addListener(async () => {
  await chrome.storage.local.set(await settings());
  await schedule();
});
chrome.runtime.onStartup.addListener(schedule);
chrome.alarms.onAlarm.addListener(alarm => {
  if (alarm.name === ALARM) remindActiveTab();
});
chrome.runtime.onMessage.addListener(message => {
  if (message?.type === 'WIRD_SETTINGS_SAVED') schedule();
});