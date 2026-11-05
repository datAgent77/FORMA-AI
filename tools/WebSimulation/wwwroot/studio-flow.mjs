// View mapping only: authoritative state, timing and records stay in the shared C# engine.
export function studioView(model,offline=false){
 const state=model?.workout?.state,terminal=state==='Completed'||state==='Aborted';
 if(offline)return {label:'Yeniden bağlan',action:'connect',terminal};
 if(!model)return {label:'Yükleniyor…',action:null,terminal};
 if(model.setup?.step!=='Complete')return {label:'Kurulumu tamamla',action:'setup',terminal};
 if(terminal)return {label:'Yeni seans hazırla',action:'new',terminal};
 if(model.workout?.isRunning)return {label:'Duraklat',action:'pause',terminal};
 if(!model.gate?.space)return {label:'Alanım açık, çevrem görünür',action:'confirm',terminal};
 return {label:state==='Paused'?'Devam et':'Antrenmanı başlat',action:'start',terminal};
}
export function sessionSummary(model){
 const s=model?.daily?.summary;
 if(!s||!['Completed','Aborted'].includes(s.outcome))return null;
 return {title:s.outcome==='Completed'?'Antrenmanı tamamladın':'Seans sonlandırıldı',completed:s.outcome==='Completed',workSeconds:Math.floor(s.activeSeconds||0),sets:s.sets||0,completedSessions:s.completedSessions||0,plan:(model.plan||[]).map(e=>e.id==='march'?'Yerinde yürüyüş':e.id==='shoulders'?'Omuz çevirme':e.name)};
}
export function canConfirmSpace({immersive=false,tracked=false,visible=false}){return !immersive||(tracked&&visible);}

export function workoutLabel(state){return {Ready:'Hazır',Introduction:'Tanışma',Work:'Hareket',Rest:'Dinlenme',Paused:'Duraklatıldı',Completed:'Tamamlandı',Aborted:'Sonlandırıldı'}[state]||'Hazır';}
