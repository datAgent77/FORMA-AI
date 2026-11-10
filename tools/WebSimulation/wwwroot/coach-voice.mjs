// Optional local-device Web Speech only. Never silently use network-backed voices.
export function createCoachVoice({synth=globalThis.speechSynthesis,Utterance=globalThis.SpeechSynthesisUtterance,onStatus=()=>{}}={}){
 let enabled=false,lastKey='',generation=0,active=null,lastInstruction=null,waitingForVoice=false;
 const supported=!!synth&&!!Utterance;
 function cancel(){generation++;synth?.cancel();active=null;}
 function speak(instruction,force=false){
  lastInstruction=instruction;if(!instruction)return;
  if(!force&&instruction.key===lastKey)return;lastKey=instruction.key;cancel();
  if(!supported||!enabled||instruction.interrupted){onStatus(!supported?'unsupported':!enabled?'muted':'interrupted');return;}
  const voices=synth.getVoices().filter(v=>v.localService&&v.lang.toLowerCase().startsWith(instruction.language));
  if(!voices.length){waitingForVoice=true;onStatus('no-local-voice');return;}waitingForVoice=false;
  const token=generation,u=new Utterance(instruction.text);u.lang=instruction.language==='tr'?'tr-TR':'en-US';u.voice=voices[0];u.rate=.94;active=u;
  u.onstart=()=>{if(token===generation)onStatus('speaking');};u.onend=()=>{if(token===generation){active=null;onStatus('ready');}};u.onerror=()=>{if(token===generation){active=null;onStatus('blocked');}};
  try{synth.speak(u);onStatus('requested');}catch{onStatus('blocked');}
 }
 const voicesChanged=()=>{if(enabled&&waitingForVoice&&lastInstruction&&!active)speak(lastInstruction,true);};synth?.addEventListener?.('voiceschanged',voicesChanged);
 return {supported,get enabled(){return enabled;},setEnabled(value){enabled=!!value;cancel();lastKey='';onStatus(enabled?'ready':'muted');if(enabled)speak(lastInstruction,true);},update:speak,cancel,dispose(){cancel();synth?.removeEventListener?.('voiceschanged',voicesChanged);}};
}
export function livingInstruction(model,pose,{offline=false,visible=true,localPaused=false}={}){
 if(!pose)return null;const w=model.workout,s=model.personal?.session,tr=model.athlete?.profile?.preferences?.language!=='en';
 const T=(a,b)=>tr?a:b;
 let text=model.coach?.text||'';
 if(pose.state==='demonstrating'||pose.state==='exercising'){
 const scripts={march:T('Yerinde yürüyüşle başlayalım. Küçük adımlar at; ritmi rahat bulduğun hızda izle.','Let’s march in place. Take small steps and follow at a comfortable pace.'),shoulders:T('Omuzlarını rahat bırak. Küçük, kontrollü daireler çiz. Doğal nefes al.','Relax your shoulders. Make small, controlled circles. Keep breathing naturally.'),knees:T('Şimdi dizlerini sırayla rahat bir yüksekliğe kaldır. Dengeni koru ve kendi hızında ilerle.','Now raise one knee at a time to a comfortable height. Stay balanced and move at your own pace.'),mobility:T('Ayakların yerde kalsın. Gövdeni ve kollarını yavaşça, küçük bir açıklıkta hareket ettir. Zorlama.','Keep your feet grounded. Gently move your torso and arms through a small, comfortable range. Do not force it.')};text=scripts[w.exerciseId]||w.instructions;
 }else if(pose.state==='cooldown')text=T('Yavaşlayalım. Ayakların yerde kalsın, omuzlarını gevşet ve rahat nefes al.','Let’s slow down. Keep your feet grounded, relax your shoulders and breathe comfortably.');
 if(s?.awaitingFeedback)text=T('Aralık bitti. Dinlen. Hareketi tamamladığını, kısmen yaptığını veya atladığını sen bildir. Tekrar sayısı ölçülmedi.','Interval finished. Rest. Tell me whether you completed, partially completed or skipped it. Repetitions were not measured.');
 if(offline||!visible||localPaused&&w.isRunning)text=T('Duraklatıldı. Hareketi bırak ve çevreni kontrol et. Hazır olduğunda açıkça devam et.','Paused. Stop moving and check your surroundings. Explicitly resume when ready.');
 const kind=['demonstrating','exercising'].includes(pose.state)?'movement':pose.state;
 return {text,language:tr?'tr':'en',key:[s?.id,s?.index,tr?'tr':'en',kind,s?.awaitingFeedback,offline,visible,localPaused,model.personal?.savePending].join(':'),interrupted:offline||!visible||pose.state==='paused'};
}
