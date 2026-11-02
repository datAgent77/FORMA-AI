'use strict';
let dailyFormHash='';
const dailyText=(en,turkish)=>say(en,turkish);
function daySelect(field,label,options,current){
 const wrapper=element('label',undefined,'field');wrapper.append(element('span',label));const select=element('select');select.id='day-'+field;
 for(const option of options)select.add(new Option(option[1],option[0]));select.value=String(current);
 select.addEventListener('change',()=>act('dailyEdit',{field,value:field==='availableMinutes'?Number(select.value):select.value}));wrapper.append(select);return wrapper;
}
function dayButton(text,action,classes='primary',id){const button=element('button',text,classes);if(id)button.id=id;button.addEventListener('click',action);return button;}
function renderDaily(){
 const done=state.setup.step==='Complete',w=state.workout,active=w?.isRunning||w?.state==='Paused',terminal=w?.state==='Completed'||w?.state==='Aborted';
 const isDaily=done&&!state.closed&&!active&&!terminal;
 show('daily-view',isDaily);show('workout-view',done&&!state.closed&&(active||terminal));
 if(done&&!state.closed){put('page-title',active?dailyText('Move at your pace.','Kendi temponda hareket et.'):terminal?dailyText('A moment to reflect.','Düşünmek için bir an.'):dailyText('Today, with FORMA.','Bugün, FORMA ile.'));put('step-label',active?'FORMA Studio':dailyText('Your daily companion','Günlük egzersiz arkadaşın'));}
 if(isDaily){put('page-body',state.daily.greeting);put('coach-cue',state.daily.greeting);put('coach-mode',state.daily.stage==='Recommendation'?'Recommending':'Check-in');}
 if(terminal){
  const summary=state.daily.summary;
  put('page-body',summary?.recordSaved?dailyText('Your session is recorded locally. Return whenever you are ready.','Seansın yerel olarak kaydedildi. Hazır olduğunda geri dön.'):dailyText('This result is not saved yet. Retry before leaving.','Bu sonuç henüz kaydedilmedi. Ayrılmadan yeniden dene.'));
  put('instructions',summary?.comparison||state.coach.text);
  show('start-workout',false);show('workout-space',false);$('workout-space').closest('label').hidden=true;
  const actions=$('workout-view').querySelector('.workout-actions');let again=$('daily-again');
  if(!again){again=dayButton(dailyText('Choose another session','Başka seans seç'),()=>act('dailyBack'),'primary','daily-again');actions.append(again);}again.hidden=false;
 }else{if($('daily-again'))show('daily-again',false);if($('workout-space'))$('workout-space').closest('label').hidden=false;}
 if(!isDaily)return;
 const day=state.daily,p=state.setup.profile;
 const hash=JSON.stringify({day,space:state.gate.space,profile:p});if(hash===dailyFormHash)return;dailyFormHash=hash;
 const focus=document.activeElement?.id,root=$('daily-view');root.replaceChildren();
 if(day.stage==='CheckIn'){
  const row=element('div',undefined,'two-fields');row.append(daySelect('energy',dailyText('How is your energy?','Enerjin nasıl?'),[['Low',dailyText('Low','Düşük')],['Normal',dailyText('Normal','Normal')],['High',dailyText('High','Yüksek')],['Unspecified',dailyText('Not shared','Paylaşılmadı')]],day.readiness.energy),daySelect('availableMinutes',dailyText('Time available','Ayırabileceğin süre'),[[5,'5 min'],[10,'10 min'],[20,'20 min'],[30,'30 min'],[0,dailyText('Saved preference','Kayıtlı tercih')]],day.readiness.availableMinutes));root.append(row);
  root.append(daySelect('activity',dailyText('What feels right today?','Bugün sana ne uygun?'),[['Choose',dailyText('Let FORMA choose','FORMA seçsin')],['Strength',dailyText('Strength','Kuvvet')],['Cardio',dailyText('Cardio','Kardiyo')],['Mobility',dailyText('Mobility','Hareketlilik')],['Recovery',dailyText('Recovery','Dinlenme odaklı')]],day.readiness.activity));
  const details=element('details',undefined,'space-details');details.append(element('summary',dailyText('Space, equipment & safety','Alan, ekipman ve güvenlik')));
  details.append(daySelect('space',dailyText('Manually configured area','Elle yapılandırılan alan'),[['StandingInPlace',dailyText('Standing in place','Yerinde ayakta')],['SeatedArea',dailyText('Seated area only','Yalnızca oturma alanı')]],day.space.kind));
  details.append(daySelect('concern',dailyText('Anything that means stopping today?','Bugün durmanı gerektiren bir durum var mı?'),[['NoneReported',dailyText('No concern reported','Endişe bildirilmedi')],['Pain',dailyText('Pain','Ağrı')],['Dizziness',dailyText('Dizziness','Baş dönmesi')],['InjuryConcern',dailyText('Injury concern','Yaralanma endişesi')],['OtherConcern',dailyText('Another safety concern','Başka güvenlik endişesi')]],day.readiness.concern));
  const equipment=element('label',undefined,'check'),einput=element('input');einput.type='checkbox';einput.checked=day.space.equipmentReported;einput.id='day-equipment';einput.addEventListener('change',()=>act('dailyEdit',{field:'equipment',value:einput.checked}));equipment.append(einput,element('span',dailyText('Equipment available (inventory only)','Ekipman var (yalnızca envanter)')));details.append(equipment);root.append(details);
  root.append(spaceConfirmation());
  root.append(element('p',day.scope,'daily-scope'));
  const actions=element('div',undefined,'daily-actions');actions.append(dayButton(dailyText('Find today’s session','Bugünün seansını bul'),()=>act('dailyRecommend'),'primary','daily-recommend'),dayButton(dailyText('Skip check-in / quick choice','Check-in atla / hızlı seçim'),()=>act('dailyQuick'),'text-button','daily-quick'));root.append(actions);
 }else{
  if(day.status!=='Ready'||!day.primary){root.append(element('h2',dailyText('Let’s pause and check.','Durup kontrol edelim.')),element('p',day.message,'daily-blocked'));if(day.status==='ConfirmSpace')root.append(spaceConfirmation());root.append(dayButton(dailyText('Adjust check-in','Check-in düzenle'),()=>act('dailyBack'),'secondary'));}
  else{
   const card=element('section',undefined,'recommendation-card');card.setAttribute('aria-label','Daily workout recommendation');
   card.append(element('p',dailyText('Your workout today','Bugünkü egzersizin'),'step-label'),element('h2',day.primary.name));
   const seconds=day.primary.durationSeconds;card.append(element('p',`${Math.floor(seconds/60)} min ${seconds%60} s / ${dailyText('Gentle','Hafif')} / ${dailyText('No equipment','Ekipmansız')}`,'recommendation-meta'));
   card.append(element('p',day.primary.exercises.join(' + '),'expected-activity'));
   if(day.unsupportedStrength)card.append(element('p',dailyText('Strength is not available in the reviewed catalog. This is an optional gentle alternative, not a strength session.','İncelenmiş katalogda kuvvet seansı yok. Bu isteğe bağlı hafif alternatiftir; kuvvet seansı değildir.'),'fallback-notice'));
   const why=element('details',undefined,'recommendation-reasons');why.open=true;why.append(element('summary',dailyText('Why this session?','Neden bu seans?')));const list=element('ul');for(const reason of day.reasons)list.append(element('li',reason));why.append(list);card.append(why);
   card.append(dayButton(dailyText('Start this session','Bu seansı başlat'),()=>{lastClock=performance.now();act('start');},'primary','daily-start'));root.append(card);
   if(day.alternatives.length){const alternatives=element('details',undefined,'alternative-list');alternatives.append(element('summary',dailyText('Change workout','Egzersizi değiştir')));for(const other of day.alternatives)alternatives.append(dayButton(other.name+` / ${Math.floor(other.durationSeconds/60)}:${String(other.durationSeconds%60).padStart(2,'0')}`,()=>act('dailyChoose',{value:other.id}),'secondary'));root.append(alternatives);}
   root.append(dayButton(dailyText('Edit today’s check-in','Bugünün check-in bilgisini düzenle'),()=>act('dailyBack'),'text-button'));
  }
 }
 if(focus)$(focus)?.focus({preventScroll:true});
}
function spaceConfirmation(){
 const label=element('label',undefined,'check daily-space'),input=element('input');input.type='checkbox';input.id='day-confirm-space';input.checked=state.gate.space;
 input.addEventListener('change',()=>act('confirmSpace',{enabled:input.checked}));label.append(input,element('span',dailyText('I have checked a clear space for comfortable, equipment-free movement.','Rahat, ekipmansız hareket için açık alanımı kontrol ettim.')));return label;
}
