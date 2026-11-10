const el=id=>document.getElementById(id);
const texts={tr:{plan:'Bugünkü planın',close:'Planı kapat',duration:n=>`${n} saniye zamanlayıcı süresi. Geri bildirim ve duraklamalar eklenir.`,start:'Bu planla başla',consent:'Alanım açık, çevrem görünür; ekipman kullanmıyorum.',needConsent:'Başlamadan önce alanını kontrol edip onayla.',error:'İşlem uygulanamadı. Açık seansı, alan onayını ve yerel depolamayı kontrol et.',storage:'Geçmiş okunamıyor. Orijinal kayıtlar korundu; depolamayı düzelt veya dışa aktar.',profile:'Önce Profil bölümünde sporcu profilini oluştur.',feedback:'Bu aralık nasıldı?',outcome:'Tamamlanma durumu',select:'Seç',completed:'Tamamladım',partial:'Kısmen yaptım',skipped:'Atladım',difficulty:'Zorluk · 1 kolay, 5 çok zor',reps:'Yaptığın tekrar · isteğe bağlı, elle giriş',discomfort:'Rahatsızlık hissettim · seansı durdur',submit:'Kaydet ve devam et',warm:'Isınma',exercise:'Hareket',cool:'Soğuma',rest:'dinlenme',fallback:'Bu hareket için güvenilir animasyon yok; yazılı talimatı izle. Rahatsızlıkta dur.',giveFeedback:'Geri bildirim ver',retry:'Kaydetmeyi yeniden dene'},en:{plan:'Today’s plan',close:'Close plan',duration:n=>`${n} timer seconds. Feedback and pauses add time.`,start:'Start this plan',consent:'My space is clear, surroundings visible; no equipment.',needConsent:'Check and confirm your space before starting.',error:'Action did not complete. Check the active session, space confirmation and local storage.',storage:'History is unreadable. Original records were preserved; restore storage access or export your data.',profile:'Create your athlete profile in Profile first.',feedback:'How was this interval?',outcome:'Completion status',select:'Choose',completed:'Completed',partial:'Partially completed',skipped:'Skipped',difficulty:'Difficulty · 1 easy, 5 very hard',reps:'Repetitions performed · optional manual entry',discomfort:'I felt discomfort · stop the session',submit:'Save and continue',warm:'Warm-up',exercise:'Exercise',cool:'Cooldown',rest:'rest',fallback:'No reliable animation is available for this movement; follow the written instructions. Stop for discomfort.',giveFeedback:'Report this interval',retry:'Retry saving'}};
export function createPersonalCoachUI({send,confirmSpace,getModel}){
 let model=null,feedbackKey='';const copy=()=>texts[model?.athlete?.profile?.preferences?.language==='en'?'en':'tr'];
 const openPlan=()=>{el('personal-plan-panel').hidden=false;el('personal-start-consent').checked=false;};
 const openFeedback=()=>{el('personal-feedback-panel').hidden=false;};
 el('personal-plan-close').onclick=()=>{el('personal-plan-panel').hidden=true;};el('personal-feedback-close').onclick=()=>{el('personal-feedback-panel').hidden=true;};
 el('personal-plan-open').onclick=async()=>{
  if(!getModel()?.athlete?.profile){el('athlete-open').click();return;}
  if(getModel()?.workout?.isRunning||getModel()?.personal?.session?.state==='PAUSED'){openPlan();return;}
  if(await send('coachRecommend'))openPlan();
 };
 el('personal-start').onclick=async()=>{if(!el('personal-start-consent').checked){el('personal-plan-error').textContent=copy().needConsent;return;}if(!await confirmSpace())return;if(await send('coachStart'))el('personal-plan-panel').hidden=true;else el('personal-plan-error').textContent=copy().error;};
 el('feedback-outcome').onchange=()=>{el('feedback-difficulty').required=el('feedback-outcome').value!=='Skipped';};
 el('personal-feedback-form').onsubmit=async event=>{
  event.preventDefault();const s=model?.personal?.session;if(!s)return;const outcome=el('feedback-outcome').value;
  const ok=await send('coachFeedback',{value:{blockIndex:s.index,exerciseId:s.record.blocks[s.index].exerciseId,outcome,difficulty:el('feedback-difficulty').value?Number(el('feedback-difficulty').value):null,repetitions:el('feedback-reps').value===''?null:Number(el('feedback-reps').value),discomfort:el('feedback-discomfort').checked}});
  if(ok){el('personal-feedback-panel').hidden=true;el('personal-feedback-form').reset();}else el('feedback-error').textContent=copy().error;
 };
 el('coach-skip').onclick=()=>send('coachSkip');
 return {openPlan,openFeedback,copy,render(next,immersive){
  model=next;const c=copy(),p=model?.personal,r=p?.recommendation,s=p?.session;
  if(p?.status==='Unavailable')el('personal-plan-error').textContent=c.storage;else if(Object.values(texts).some(x=>x.storage===el('personal-plan-error').textContent))el('personal-plan-error').textContent='';
  el('personal-plan-title').textContent=c.plan;el('personal-plan-open').textContent=model?.athlete?.profile?.preferences?.language==='en'?'My plan':'Planım';
  el('personal-plan-close').setAttribute('aria-label',c.close);el('personal-greeting').textContent=p?.greeting||(model?.athlete?.profile?'':c.profile);
  el('personal-plan-duration').textContent=r?c.duration(r.durationSeconds):'';
  const reasons=el('personal-reasons');reasons.replaceChildren();for(const text of r?.reasons||[]){const line=document.createElement('p');line.textContent=text;reasons.append(line);}
  const blocks=el('personal-blocks');blocks.replaceChildren();for(const b of r?.blocks||[]){const line=document.createElement('li');line.textContent=`${b.stage==='WarmUp'?c.warm:b.stage==='Cooldown'?c.cool:c.exercise}: ${b.exercise.name} · ${b.workSeconds} s${b.restSeconds?` / ${b.restSeconds} s ${c.rest}`:''}`;blocks.append(line);}
  el('personal-start').textContent=c.start;el('personal-start').disabled=r?.status!=='Ready'||!!s&&s.state!=='COMPLETED'&&s.state!=='CANCELLED'&&s.state!=='SKIPPED';el('personal-start-consent-text').textContent=c.consent;
  el('personal-feedback-title').textContent=c.feedback;el('personal-feedback-close').setAttribute('aria-label',model?.athlete?.profile?.preferences?.language==='en'?'Close feedback':'Geri bildirimi kapat');el('feedback-outcome-label').textContent=c.outcome;el('feedback-difficulty-label').textContent=c.difficulty;el('feedback-reps-label').textContent=c.reps;el('feedback-discomfort-label').textContent=c.discomfort;el('personal-feedback-submit').textContent=c.submit;
  const options=el('feedback-outcome').options;[c.select,c.completed,c.partial,c.skipped].forEach((text,i)=>options[i].textContent=text);el('feedback-difficulty').options[0].textContent=c.select;
  el('coach-skip').hidden=!p?.enabled;el('coach-skip').disabled=!s||s.awaitingFeedback||s.state!=='ACTIVE'&&s.state!=='PAUSED';
  if(s?.awaitingFeedback){
   const b=s.record.blocks[s.index];el('personal-feedback-exercise').textContent=model.workout?.exerciseName||b.exerciseId;
   const key=s.id+':'+s.index;if(key!==feedbackKey){feedbackKey=key;el('personal-feedback-form').reset();el('feedback-error').textContent='';el('feedback-difficulty').required=true;if(!immersive)openFeedback();}
  }else el('personal-feedback-panel').hidden=true;
 }};
}
