const $ = id => document.getElementById(id);
const digits = value => new Intl.NumberFormat('ar-EG').format(value);
async function load() {
  const settings = await chrome.storage.local.get({ interval: 5, duration: 30, dhikr: 'سُبْحَانَ اللَّهِ وَبِحَمْدِهِ' });
  $('interval').value = settings.interval; $('duration').value = settings.duration; $('dhikr').value = settings.dhikr; render();
}
function render() { $('interval-out').textContent = digits($('interval').value) + ' دقائق'; $('duration-out').textContent = digits($('duration').value) + ' ثانية'; }
$('interval').addEventListener('input', render); $('duration').addEventListener('input', render);
$('save').addEventListener('click', async () => {
  await chrome.storage.local.set({ interval: Number($('interval').value), duration: Number($('duration').value), dhikr: $('dhikr').value });
  chrome.runtime.sendMessage({ type: 'WIRD_SETTINGS_SAVED' }); $('status').textContent = 'تم حفظ الإعدادات';
});
load();

