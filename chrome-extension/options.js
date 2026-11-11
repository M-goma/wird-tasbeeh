const $ = id => document.getElementById(id);
const digits = value => new Intl.NumberFormat('ar-EG').format(value);
async function load() {
  const settings = await chrome.storage.local.get({interval:5,duration:30,dhikr:'سُبْحَانَ اللَّهِ وَبِحَمْدِهِ',activeFrom:'08:00',activeUntil:'22:00',darkMode:false});
  $('interval').value=settings.interval;$('duration').value=settings.duration;$('dhikr').value=settings.dhikr;$('activeFrom').value=settings.activeFrom;$('activeUntil').value=settings.activeUntil;$('darkMode').checked=settings.darkMode;document.body.classList.toggle('dark',settings.darkMode);render();
}
function render(){$('interval-out').textContent=digits($('interval').value)+' دقائق';$('duration-out').textContent=digits($('duration').value)+' ثانية'}
$('interval').addEventListener('input',render);$('duration').addEventListener('input',render);$('darkMode').addEventListener('change',e=>document.body.classList.toggle('dark',e.target.checked));
$('save').addEventListener('click',async()=>{await chrome.storage.local.set({interval:Number($('interval').value),duration:Number($('duration').value),dhikr:$('dhikr').value.trim()||'سُبْحَانَ اللَّهِ وَبِحَمْدِهِ',activeFrom:$('activeFrom').value,activeUntil:$('activeUntil').value,darkMode:$('darkMode').checked});chrome.runtime.sendMessage({type:'WIRD_SETTINGS_SAVED'});$('status').textContent='تم حفظ الإعدادات'});
load();