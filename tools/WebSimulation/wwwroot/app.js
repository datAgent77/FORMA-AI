'use strict';
let state, busy=false, lastFrame=performance.now(), phase=0, lastSetupHash='', lastClock=performance.now();
const $=id=>document.getElementById(id);
const tr=()=>state?.setup.profile.language==='tr';
const say=(en,turkish)=>tr()?turkish:en;
const put=(id,text)=>$(id).textContent=text;
function show(id,visible){$(id).hidden=!visible;}
function element(tag,text,className){const el=document.createElement(tag);if(text!==undefined)el.textContent=text;if(className)el.className=className;return el;}
async function connect(){
 try{const response=await fetch('/api/state');if(!response.ok)throw new Error('host');state=await response.json();show('connection-error',false);render();}
 catch{show('connection-error',true);}
}
let requestQueue=Promise.resolve();
function act(action,extra={}){
 const result=requestQueue.then(()=>sendAction(action,extra));
 requestQueue=result.catch(()=>false);return result;
}
async function sendAction(action,extra={}){
 busy=true;
 try{
  const response=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify({action,...extra})});
  const result=await response.json();
  if(!response.ok){put('notice',result.error||'Action failed. Retry after checking the local host.');return false;}
  state=result.state;render();show('connection-error',false);return result.accepted;
 }catch{show('connection-error',true);return false;}
 finally{busy=false;}
}
function selectField(field,label,options,hint){
 const wrapper=element('label',undefined,'field');wrapper.append(element('span',label));
 const select=element('select');select.id='field-'+field;select.name=field;
 for(const option of options){const value=typeof option==='string'?option:option.value;select.add(new Option(typeof option==='string'?option:option.label,value));}
 select.value=String(state.setup.profile[field]);select.addEventListener('change',()=>act('edit',{field,value:typeof state.setup.profile[field]==='number'?Number(select.value):select.value}));
 wrapper.append(select);if(hint)wrapper.append(element('small',hint));return wrapper;
}
function checkField(field,label,enabled,action='edit'){
 const wrapper=element('label',undefined,'check');const input=element('input');input.type='checkbox';input.checked=enabled;input.id='field-'+field;
 input.addEventListener('change',()=>act(action,action==='edit'?{field,value:input.checked}:{field,enabled:input.checked}));
 wrapper.append(input,element('span',label));return wrapper;
}
function renderSetup(){
 const setup=state.setup,p=setup.profile,hash=JSON.stringify({setup,coaches:state.coaches,options:state.options});
 if(hash===lastSetupHash)return;lastSetupHash=hash;
 const activeId=document.activeElement?.id;
 const form=$('setup-form');form.replaceChildren();
 if(setup.loadBlocked){form.append(element('p',say('Stored setup could not be read. Reset explicitly to preserve or replace it.','Kayıtlı kurulum okunamadı. Sıfırlamak için açıkça onayla.')));return;}
 switch(setup.step){
  case 'Welcome':{
   form.append(selectField('posture',say('Workout position','Egzersiz pozisyonu'),[{value:'Standing',label:say('Standing','Ayakta')},{value:'Seated',label:say('Seated','Oturarak')}]));
   form.append(selectField('language',say('Setup language','Kurulum dili'),[{value:'en',label:'English'},{value:'tr',label:'Türkçe'}],say('Workout cues remain English.','Egzersiz yönergeleri İngilizce kalır.')));
   const row=element('div',undefined,'two-fields');row.append(checkField('muted',say('Mute audio','Sesi kapat'),p.muted),checkField('reducedMotion',say('Reduce idle motion','Boşta hareketi azalt'),p.reducedMotion));form.append(row);
   form.append(element('small',say('Text guidance only. No audio is synthesized or recorded.','Yalnızca metin. Ses üretilmez veya kaydedilmez.'),'field-hint'));break;
  }
  case 'Coach':{
   const presets=element('div',undefined,'coach-options');
   for(const coach of state.coaches){const button=element('button',coach.displayName,'coach-choice');button.type='button';button.setAttribute('aria-pressed',String(p.coachId===coach.id));button.dataset.coach=coach.id;button.addEventListener('click',()=>act('edit',{field:'coachId',value:coach.id}));presets.append(button);}
   form.append(element('label',say('Choose an original look','Özgün bir görünüm seç'),'field'),presets);
   form.append(selectField('personality',say('Coaching style','Koçluk tarzı'),state.options.personalities));
   form.append(element('p',say('Same safety rules, whichever personality you choose. Voice: text only.','Kişilik seçimin güvenlik kurallarını değiştirmez. Ses: yalnızca metin.'),'field-hint'));break;
  }
  case 'Profile':{
   form.append(selectField('goal',say('Goal · future planning preference','Hedef · gelecek planlama tercihi'),state.options.goals));
   form.append(selectField('experience',say('Experience · planning preference','Deneyim · planlama tercihi'),state.options.experiences));
   const row=element('div',undefined,'two-fields');row.append(selectField('preferredMinutes',say('Preferred minutes','Tercih edilen dakika'),[{value:0,label:say('Skip','Atla')},{value:3,label:'3'},{value:5,label:'5'},{value:10,label:'10'}]),selectField('workoutPreference',say('Movement type','Hareket türü'),state.options.preferences));form.append(row);
   let limits=p.movementLimits;if(typeof limits==='string')limits=limits.includes('AvoidLowerBody')&&limits.includes('AvoidShoulders')?3:limits==='AvoidLowerBody'?1:limits==='AvoidShoulders'?2:0;
   const wrapper=element('label',undefined,'field');wrapper.append(element('span',say('Optional movement exclusions','İsteğe bağlı hareket kısıtları')));const select=element('select');select.id='field-movementLimits';
   [say('Not disclosed','Paylaşılmadı'),say('Avoid lower-body work','Alt beden hareketlerini çıkar'),say('Avoid shoulder work','Omuz hareketlerini çıkar'),say('Avoid both','Her ikisini çıkar')].forEach((text,i)=>select.add(new Option(text,i)));select.value=String(limits);select.addEventListener('change',()=>act('edit',{field:'movementLimits',value:Number(select.value)}));wrapper.append(select);form.append(wrapper);
   form.append(checkField('hasEquipment',say('I own equipment (inventory only; this workout uses none)','Ekipmanım var (yalnızca envanter; bu egzersizde kullanılmaz)'),p.hasEquipment));break;
  }
  case 'Safety':
   form.append(checkField('ack',state.labels.ack,setup.safetyAcknowledged,'safety'),checkField('space',state.labels.space,setup.spaceConfirmed,'safety'));
   form.append(element('p',say('These are simulated confirmations. This browser cannot assess your real room.','Bunlar simüle edilmiş onaylardır. Tarayıcı gerçek odanı değerlendiremez.'),'field-hint'));break;
  case 'Review':{
   const list=element('ul',undefined,'review-list');const personality=state.options.personalities.find(x=>x.value===p.personality)?.label;
   [state.coach.name+' / '+personality,p.posture==='Seated'?say('Seated session','Oturarak seans'):say('Standing session','Ayakta seans'),...state.compatible].forEach(text=>list.append(element('li',text)));form.append(list);
   if(!state.compatible.length)form.append(element('p',say('No compatible movement. You can save setup, but workout start will stay blocked.','Uyumlu hareket yok. Kurulumu kaydedebilirsin; egzersiz başlatılamaz.'),'notice'));break;
  }
 }
 if(activeId)$(activeId)?.focus({preventScroll:true});
}
function render(){
 if(!state)return;
 document.documentElement.lang=state.setup.profile.language;document.body.dataset.coach=state.coach.id;
 const setupDone=state.setup.step==='Complete',running=Boolean(state.workout?.isRunning),paused=state.workout?.state==='Paused';
 put('step-label',setupDone?say('Your equipment-free practice','Ekipmansız egzersizin'):say(`First visit / ${state.setup.index+1} of 5`,`İlk ziyaret / ${state.setup.index+1} / 5`));
 put('page-title',state.closed?say('See you next time.','Bir sonraki seansa.'):setupDone?say('A little movement. A fresh start.','Biraz hareket. Yeni bir başlangıç.'):state.setup.title);
 put('page-body',state.closed?'':state.setup.body);
 show('setup-form',!setupDone&&!state.closed);show('setup-actions',!setupDone&&!state.closed&&!state.setup.loadBlocked);show('workout-view',setupDone&&!state.closed);show('closed-view',state.closed);
 if(!setupDone)renderSetup();
 put('back',state.labels.back);put('next',state.setup.step==='Review'?state.labels.finish:state.labels.next);put('skip',state.labels.skip);
 show('back',state.setup.index>0&&state.setup.index<5);show('skip',state.setup.step==='Profile');$('next').disabled=state.setup.step==='Safety'&&(!state.setup.safetyAcknowledged||!state.setup.spaceConfirmed);
 put('notice',state.notice);
 put('coach-name',state.coach.name);put('coach-style',state.options.personalities.find(x=>x.value===state.setup.profile.personality)?.label||'');put('coach-cue',state.coach.text);put('coach-mode',state.coach.state);
 if(setupDone){
  const exerciseBox=$('exercise-choices');exerciseBox.replaceChildren();
  if(!running&&!paused)for(const exercise of state.exercises){
   const label=element('label',undefined,'check'),input=element('input');input.type='checkbox';input.checked=exercise.selected;
   input.addEventListener('change',()=>act('selectExercise',{field:exercise.id,enabled:input.checked}));label.append(input,element('span',exercise.name));exerciseBox.append(label);
  }
  const w=state.workout,seconds=Math.ceil(w?.remaining||0),terminal=w?.state==='Completed'||w?.state==='Aborted';
  put('timer',`${String(Math.floor(seconds/60)).padStart(2,'0')}:${String(seconds%60).padStart(2,'0')}`);
  put('workout-phase',w?.state||'No compatible movement');put('set-info',w?`Set ${w.set} / ${w.totalSets}`:'');
  put('exercise-name',w?.state==='Completed'?say('Workout complete.','Egzersiz tamamlandı.'):w?.state==='Aborted'?say('Session ended.','Seans sona erdi.'):w?.exerciseName||say('Update your movement preferences.','Hareket tercihlerini güncelle.'));
  put('instructions',terminal?state.coach.text:w?.instructions||'No exercise will start with this profile.');
  put('work-time',w?`${Math.floor(w.activeSeconds)} simulated work seconds / ${w.completedSets} timed sets`:'');
  $('workout-space').checked=state.gate.space;$('workout-space').disabled=running;
  show('start-workout',!running&&!paused);$('start-workout').disabled=!state.gate.ready||!state.compatible.length;
  show('pause-workout',running);show('resume-workout',paused);$('resume-workout').disabled=!state.gate.ready;show('stop-workout',running||paused);
  put('clock-status',running?'running':paused?'paused':'ready');
 }
 for(const flag of ['focus','tracking','passthrough'])$('signal-'+flag).checked=state.gate[flag];$('write-fault').checked=state.writeFault;
 put('debug-setup',state.setup.step);put('debug-workout',state.workout?.state||'Not started');put('debug-gate',state.gate.ready?'Ready':'Not confirmed');put('debug-storage',state.setup.loadStatus);
 $('edit-setup').disabled=running||paused;
 const history=$('history-list');history.replaceChildren();
 if(!state.history.length)history.append(element('p',say('Complete a simulated session to inspect its recorded result.','Kayıtlı sonucu incelemek için simüle edilmiş bir seans tamamla.')));
 for(const record of state.history.slice(0,4)){
  const row=element('div',undefined,'history-row');row.append(element('strong',record.outcome),element('span',`${Math.floor(record.activeSeconds)} test seconds / ${record.completedSets} sets`),element('em',record.startedUtc.slice(0,16).replace('T',' ')));history.append(row);
 }
 if(typeof renderDaily==='function')renderDaily();
 const events=$('event-list');events.replaceChildren();for(const text of state.events.slice(0,5))events.append(element('li',text));
 document.querySelectorAll('[data-advance]').forEach(b=>b.disabled=!running);
}
$('next').addEventListener('click',()=>act(state.setup.step==='Review'?'finish':'next'));
$('back').addEventListener('click',()=>act('back'));$('skip').addEventListener('click',()=>act('skip'));
$('edit-setup').addEventListener('click',()=>act('reopen'));
$('studio-tab').addEventListener('click',()=>$('page-title').scrollIntoView({behavior:'smooth',block:'start'}));
$('lab-tab').addEventListener('click',()=>{const hidden=document.body.classList.toggle('lab-hidden');$('lab-tab').setAttribute('aria-expanded',String(!hidden));});
$('start-workout').addEventListener('click',()=>{lastClock=performance.now();act('start');});
$('pause-workout').addEventListener('click',()=>act('pause'));
$('resume-workout').addEventListener('click',()=>{lastClock=performance.now();act('resume');});
$('stop-workout').addEventListener('click',()=>act('stop'));
$('workout-space').addEventListener('change',()=>act('confirmSpace',{enabled:$('workout-space').checked}));
for(const flag of ['focus','tracking','passthrough'])$('signal-'+flag).addEventListener('change',()=>act('signal',{field:flag,enabled:$('signal-'+flag).checked}));
$('write-fault').addEventListener('change',()=>act('writeFault',{enabled:$('write-fault').checked}));
$('reload-profile').addEventListener('click',()=>act('reload'));
$('reset-data').addEventListener('click',()=>show('confirm-reset',true));
$('confirm-reset').addEventListener('click',async()=>{await act('reset',{enabled:true});show('confirm-reset',false);lastSetupHash='';});
$('exit-app').addEventListener('click',async()=>{await act('exit',{enabled:false});show('confirm-exit',true);});
$('confirm-exit').addEventListener('click',async()=>{await act('exit',{enabled:true});show('confirm-exit',false);});
$('return-studio').addEventListener('click',()=>act('return'));
$('retry-connect').addEventListener('click',connect);
for(const b of document.querySelectorAll('[data-advance]'))b.addEventListener('click',()=>act('advance',{seconds:Number(b.dataset.advance)}));
document.addEventListener('keydown',event=>{if(event.code==='Space'&&!['INPUT','SELECT','BUTTON'].includes(document.activeElement.tagName)){event.preventDefault();act('pause');}});
document.addEventListener('visibilitychange',()=>{if(state)act('signal',{field:'focus',enabled:!document.hidden});});
setInterval(()=>{
 const now=performance.now(),elapsed=(now-lastClock)/1000;lastClock=now;
 if(!state?.workout?.isRunning||document.hidden||busy)return;
 if(elapsed>1.5){act('pause');return;}act('tick',{seconds:elapsed});
},250);
function animate(now){
 const elapsed=(now-lastFrame)/1000;lastFrame=now;
 const reduced=state?.setup.profile.reducedMotion||matchMedia('(prefers-reduced-motion: reduce)').matches;
 const running=state?.workout?.state==='Work',paused=state?.workout?.state==='Paused';
 if(!reduced&&!paused)phase+=Math.min(elapsed,.1)*(running?3:.7);
 const amount=reduced||paused?0:Math.sin(phase)*(running?8:1.2);
 $('arm-left').setAttribute('transform',`rotate(${amount} 168 190)`);$('arm-right').setAttribute('transform',`rotate(${-amount} 252 190)`);
 const march=running&&state?.workout?.exerciseId==='march';
 $('leg-left').setAttribute('transform',`rotate(${march?amount:0} 190 285)`);$('leg-right').setAttribute('transform',`rotate(${march?-amount:0} 231 285)`);
 requestAnimationFrame(animate);
}
requestAnimationFrame(animate);connect();
