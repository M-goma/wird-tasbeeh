const DEFAULTS = { interval: 5, duration: 30, dhikr: 'سُبْحَانَ اللَّهِ وَبِحَمْدِهِ' };
const ALARM = 'wird-reminder';

async function settings() {
  return { ...DEFAULTS, ...await chrome.storage.local.get(DEFAULTS) };
}

async function schedule() {
  const { interval } = await settings();
  await chrome.alarms.clear(ALARM);
  chrome.alarms.create(ALARM, { delayInMinutes: interval, periodInMinutes: interval });
}

async function remindActiveTab() {
  const [tab] = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
  if (!tab?.id || !/^https?:\/\//i.test(tab.url || '')) return;
  const current = await settings();
  const message = { type: 'WIRD_REMINDER', dhikr: current.dhikr, duration: current.duration };
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

