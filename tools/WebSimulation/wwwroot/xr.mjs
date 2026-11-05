import * as THREE from './vendor/three/three.module.js';
import {createVideoCoach} from './video-coach.mjs';
import {studioView,sessionSummary,canConfirmSpace,workoutLabel} from './studio-flow.mjs';
import {shouldPlayCoach} from './coach-media-policy.mjs';
import {inspectAR,requestMR,isTransparentSession} from './xr-session.mjs';
const el=id=>document.getElementById(id),host=el('canvas-host');
let renderer;
try{renderer=new THREE.WebGLRenderer({antialias:true,alpha:true});}catch{el('xr-status').textContent='WebGL açılamadı. Donanım hızlandırmalı tarayıcı kullan.';throw new Error('FORMA_WEBGL_UNAVAILABLE');}
renderer.setPixelRatio(Math.min(devicePixelRatio,1.5));renderer.xr.enabled=true;renderer.xr.setReferenceSpaceType('local-floor');host.append(renderer.domElement);
const scene=new THREE.Scene(),camera=new THREE.PerspectiveCamera(48,1,.05,50);camera.position.set(0,1.52,2.45);camera.lookAt(0,1.35,-1.2);
scene.add(new THREE.HemisphereLight(0xffffff,0x73858c,2.4));const sun=new THREE.DirectionalLight(0xffead1,2.1);sun.position.set(2,5,3);scene.add(sun);
const preview=new THREE.Group();scene.add(preview);renderer.setClearColor(0x000000,0);
const content=new THREE.Group();content.position.z=-1.2;scene.add(content);
const coach=new THREE.Group();coach.position.x=.06;content.add(coach);
let human=null,previewPaused=false;
createVideoCoach(coach,text=>el('asset-status').textContent=text).then(value=>{human=value;human.diagram.position.set(1.16,1.13,.025);content.add(human.diagram);}).catch(error=>{el('asset-status').textContent='Koç videosu yüklenemedi. Yeniden yükle.';console.error(error);});
function surface(width,height,pixelsW=1024,pixelsH=512){const canvas=document.createElement('canvas');canvas.width=pixelsW;canvas.height=pixelsH;const texture=new THREE.CanvasTexture(canvas);texture.colorSpace=THREE.SRGBColorSpace;const m=new THREE.Mesh(new THREE.PlaneGeometry(width,height),new THREE.MeshBasicMaterial({map:texture,transparent:true}));return {mesh:m,canvas,ctx:canvas.getContext('2d'),texture};}
const panel=surface(.75,1.25,640,1024);panel.mesh.position.set(1.16,1.24,0);content.add(panel.mesh);
const cue=surface(1.55,.23,1536,230);cue.mesh.position.set(-.16,2.02,0);cue.mesh.visible=false;content.add(cue.mesh);
const timeline=surface(2.6,.23,1536,200);timeline.mesh.position.set(.06,.12,.18);content.add(timeline.mesh);
const buttons=[],rays=[],controllers=[],raycaster=new THREE.Raycaster(),rotation=new THREE.Matrix4();
const pauseButtons=new WeakMap();
let model=null,session=null,queue=Promise.resolve(),offline=false,localPaused=true,lastClock=performance.now(),lastFrame=performance.now(),phase=0,placed=false,lastPanel='',poseTracked=false,capability,lastTerminal=null;
function wrap(ctx,text,x,y,maxWidth,lineHeight){let cursor=y;for(const paragraph of String(text).split('\n')){let line='';for(const word of paragraph.split(' ')){const test=line?line+' '+word:word;if(ctx.measureText(test).width>maxWidth&&line){ctx.fillText(line,x,cursor);cursor+=lineHeight;line=word;}else line=test;}ctx.fillText(line,x,cursor);cursor+=lineHeight;}return cursor;}
function paintPanel(){
 const state=model?.workout;
 el('session-state').textContent=`${workoutLabel(state?.state)} · ${Math.ceil(state?.remaining||0)} sn · ${state?.completedSets||0} set`;
 const text=offline?'Bağlantı kesildi. Hareketi durdur.':!model?'Koçuna bağlanılıyor…':model.setup.step!=='Complete'?'Başlamadan önce kısa kurulumu tamamla.':state?.state==='Ready'?'Alanını kontrol et. Birlikte başlayalım.':model.coach?.text||'';
 const signature=JSON.stringify({text,state,setup:model?.setup?.step,language:model?.setup?.profile?.language,session:!!session,offline,space:model?.gate?.space});if(signature===lastPanel)return;lastPanel=signature;
 const c=panel.ctx;c.clearRect(0,0,640,1024);
 c.fillStyle='rgba(19,22,21,.47)';c.beginPath();c.roundRect(12,12,616,1000,270);c.fill();
 const seconds=Math.ceil(state?.remaining||0),duration=state?.intervalSeconds||1;
 const progress=!state||state.state==='Ready'?1:Math.max(0,Math.min(1,seconds/duration));
 c.lineWidth=18;c.lineCap='round';c.strokeStyle='rgba(255,255,255,.13)';c.beginPath();c.arc(320,330,268,-Math.PI*.9,Math.PI*.9);c.stroke();
 c.strokeStyle='#edcf63';c.beginPath();c.arc(320,330,268,-Math.PI*.9,-Math.PI*.9+Math.PI*1.8*progress);c.stroke();
 c.fillStyle='#f6f3e7';c.textAlign='center';c.font='500 112px sans-serif';c.fillText(state?.state&&state.state!=='Ready'?`${Math.floor(seconds/60)}:${String(seconds%60).padStart(2,'0')}`:'Hazır',320,254);
 c.fillStyle='#d5d6cc';c.font='28px sans-serif';c.fillText(state?.state==='Rest'?'Dinlenme':state?.state==='Paused'?'Duraklatıldı':state?.state==='Completed'?'Tamamlandı':'Kalan süre',320,302);
 const terminalSummary=sessionSummary(model);
 if(human)human.diagram.visible=!terminalSummary;
 if(terminalSummary){
  c.fillStyle='#fff';c.font='72px sans-serif';c.fillText(String(terminalSummary.workSeconds),320,472);
  c.font='30px sans-serif';c.fillText('saniye çalışma',320,518);
  c.font='64px sans-serif';c.fillText(String(terminalSummary.sets),320,638);
  c.font='30px sans-serif';c.fillText('tamamlanan set',320,680);
  c.fillStyle='#edcf63';c.font='27px sans-serif';c.fillText(terminalSummary.completed?'Kendine zaman ayırdın.':'İstediğin anda durabilirsin.',320,766);
 }
 // The silhouette is the same recorded video rendered as a diagram, never body tracking.
 c.fillStyle='#fff';c.font='32px sans-serif';c.fillText(`Set ${state?.set||1} / ${state?.totalSets||2}`,320,887);c.fillStyle='#bfc2b8';c.font='23px sans-serif';c.fillText('Hareket rehberi · Ölçüm değil',320,940);panel.texture.needsUpdate=true;
 const q=cue.ctx;q.clearRect(0,0,1536,230);q.fillStyle='rgba(28,32,31,.85)';q.beginPath();q.roundRect(0,0,1536,230,26);q.fill();q.textAlign='center';q.fillStyle='#edcf63';q.font='30px sans-serif';q.fillText(state?.exerciseId==='march'?'Hafif yerinde yürüyüş':'Rahat omuz çevirme',768,54);q.fillStyle='#fff';q.font='35px sans-serif';wrap(q,text,768,112,1400,44);cue.texture.needsUpdate=true;
 const t=timeline.ctx;t.clearRect(0,0,1536,200);t.fillStyle='rgba(18,20,19,.69)';t.beginPath();t.roundRect(0,0,1536,200,24);t.fill();t.textAlign='center';
 const entries=[...(model?.plan?.length?model.plan:model?.exercises||[]).map(e=>[e.id,e.id==='march'?'Yerinde yürüyüş':e.id==='shoulders'?'Omuz çevirme':e.name,e.sets?`${e.sets} × ${e.workSeconds} saniye`:"Seans seç"]),['summary','Seans özeti','Yerel geçmiş']];const tileWidth=1536/entries.length;
 entries.forEach(([id,name,dose],i)=>{const active=id===state?.exerciseId||(id==='summary'&&state?.state==='Completed');t.fillStyle=active?'#edcf63':'#e5e4dc';t.font='34px sans-serif';t.fillText(name,tileWidth*(i+.5),91);t.fillStyle='#afb4aa';t.font='25px sans-serif';t.fillText(dose,tileWidth*(i+.5),139);if(active){t.fillStyle='#edcf63';t.fillRect(i*tileWidth+30,176,tileWidth-60,5);}});timeline.texture.needsUpdate=true;
 el('coach-cue').textContent=!state||state.state==='Ready'?'Kayıtlı video önizlemesi · Ekipmansız seans':text;

 el('workout-title').textContent=!state||state.state==='Ready'?'Koçunla aynı alanda.':state?.state==='Completed'?'Seans tamamlandı':state?.state==='Aborted'?'Seans sonlandırıldı':state?.exerciseId==='march'?'Yerinde yürüyüş':'Omuz çevirme';
 el('space-state').textContent=model?.gate?.space?'Alan onaylı':'Başlamak için alanını onayla';
 updateSetup();updateFlow();
}
async function readState(){try{const r=await fetch('/api/state');if(!r.ok)throw new Error();model=await r.json();offline=false;el('connection-status').textContent='Çevrimdışı koç · Geçmiş bu cihazda';paintPanel();}catch{failure();}}
function failure(){offline=true;localPaused=true;el('connection-status').textContent='Yerel motor bağlantısı kesildi. Hareketi durdur ve bağlantıyı kontrol et.';paintPanel();}
function send(action,extra={}){
 const result=queue.then(async()=>{try{const r=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify({action,...extra})});const data=await r.json();if(!r.ok)throw new Error();model=data.state;offline=false;if(model.workout?.state==='Paused')localPaused=true;if((action==='resume'||action==='start')&&data.accepted){localPaused=false;lastClock=performance.now();}el('connection-status').textContent=model.notice||'Çevrimdışı koç · Geçmiş bu cihazda';paintPanel();return data.accepted;}catch{failure();return false;}});
 queue=result.catch(()=>false);return result;
}
function pause(){localPaused=true;previewPaused=true;human?.pause();return send('pause');}
async function start(){previewPaused=false;if(model?.setup.step!=='Complete'){el('setup-panel').hidden=false;updateSetup();return;}if(model?.workout?.state==='Paused'){await send('resume');return;}if(!model?.daily?.enabled||model.daily.stage!=='Recommendation')await send('dailyQuick');await send('start');}
function button(label,x,y,action,color='#313b3a'){
 const b=surface(.56,.115,768,160);b.mesh.position.set(x,y,.012);const c=b.ctx;c.fillStyle=color;c.fillRect(0,0,768,160);c.fillStyle='#fff';c.font='500 40px sans-serif';c.textAlign='center';c.fillText(label,384,96);b.texture.needsUpdate=true;b.mesh.userData.action=action;content.add(b.mesh);buttons.push(b.mesh);b.mesh.visible=false;return b;
}
button('Alanı onayla',-1.12,.64,confirmSpace);button('Seans öner',-1.12,.47,()=>send('dailyQuick'));
button('Başlat / Devam',-1.12,.30,start,'#71652f');button('Ⅱ  DURAKLAT',1.02,1.94,pause,'#6c3a32');
button('Seansı bitir',1.02,.47,()=>send('stop'));button('XR çıkış',1.02,.30,async()=>{await pause();if(session)await session.end();});
button('Koçu yerleştir',-1.12,.13,()=>{place();pause();send('confirmSpace',{enabled:false});});
function choose(origin,direction){raycaster.set(origin,direction);const hit=raycaster.intersectObjects(buttons.filter(b=>b.visible),false)[0];if(hit)hit.object.userData.action();}
for(let i=0;i<2;i++){
 const controller=renderer.xr.getController(i);scene.add(controller);controllers.push(controller);
 const geometry=new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(),new THREE.Vector3(0,0,-1)]);const line=new THREE.Line(geometry,new THREE.LineBasicMaterial({color:0x74c8c7}));line.scale.z=3;controller.add(line);rays.push(line);
 controller.addEventListener('select',()=>{rotation.identity().extractRotation(controller.matrixWorld);choose(new THREE.Vector3().setFromMatrixPosition(controller.matrixWorld),new THREE.Vector3(0,0,-1).applyMatrix4(rotation));});
}
function place(){const cam=renderer.xr.isPresenting?renderer.xr.getCamera():camera;const position=new THREE.Vector3();cam.getWorldPosition(position);const forward=new THREE.Vector3();cam.getWorldDirection(forward);forward.y=0;if(forward.lengthSq()<.01)forward.set(0,0,-1);forward.normalize();content.position.copy(position).addScaledVector(forward,2.3);content.position.y=0;content.rotation.y=Math.atan2(-forward.x,-forward.z);placed=true;}
function resize(){renderer.setSize(host.clientWidth,host.clientHeight);camera.aspect=host.clientWidth/host.clientHeight;camera.fov=camera.aspect<1?62:48;camera.updateProjectionMatrix();}window.addEventListener('resize',resize);resize();
let drag=null;
renderer.domElement.addEventListener('pointerdown',e=>{drag={x:e.clientX,y:e.clientY,moved:false};});
renderer.domElement.addEventListener('pointerup',e=>{if(drag&&!drag.moved&&!session){const rect=renderer.domElement.getBoundingClientRect();raycaster.setFromCamera(new THREE.Vector2((e.clientX-rect.left)/rect.width*2-1,-(e.clientY-rect.top)/rect.height*2+1),camera);const hit=raycaster.intersectObjects(buttons.filter(b=>b.visible),false)[0];hit?.object.userData.action();}drag=null;});
el('confirm-space').onclick=confirmSpace;el('prepare').onclick=()=>send('dailyQuick');el('start').onclick=start;el('pause').onclick=pause;el('stop').onclick=()=>send('stop');el('reposition').onclick=()=>{place();pause();send('confirmSpace',{enabled:false});};
el('leave-xr').onclick=async()=>{await pause();await session?.end();};
el('space-confirm').onchange=()=>{el('enter-xr').disabled=!capability?.supported||!el('space-confirm').checked;};
el('enter-xr').onclick=async()=>{
 let requested;
 if(model?.setup?.step!=='Complete'){el('setup-panel').hidden=false;updateSetup();return;}
 try{
  requested=await requestMR(navigator.xr,{secure:isSecureContext,confirmed:el('space-confirm').checked,noEquipment:el('space-confirm').checked});
  if(!isTransparentSession(requested)){await requested.end();throw new Error('Şeffaf AR bileşimi doğrulanamadı. Opaque VR moduna geçilmedi.');}
  session=requested;el('xr-mode').textContent='Quest WebXR · Ekipmansız';buttons.forEach(b=>b.visible=true);cue.mesh.visible=true;await pause();await send('signal',{field:'tracking',enabled:false});placed=false;poseTracked=false;preview.visible=false;scene.background=null;renderer.setClearColor(0x000000,0);
  session.addEventListener('visibilitychange',()=>{const visible=session?.visibilityState==='visible';if(!visible)pause();send('signal',{field:'focus',enabled:visible});});
  session.addEventListener('end',()=>{session=null;lastTerminal=null;el('xr-mode').textContent='Masaüstü önizleme · Ekipmansız';buttons.forEach(b=>b.visible=false);cue.mesh.visible=false;preview.visible=true;scene.background=null;el('leave-xr').hidden=true;el('enter-xr').hidden=false;el('space-confirm').checked=false;el('enter-xr').disabled=true;pause();el('xr-status').textContent='XR oturumu kapandı. Egzersiz duraklatıldı; otomatik devam yok.';paintPanel();});
  await renderer.xr.setSession(session);await send('signal',{field:'passthrough',enabled:true});await send('signal',{field:'focus',enabled:session.visibilityState==='visible'});el('leave-xr').hidden=false;el('enter-xr').hidden=true;el('xr-status').textContent='Gerçek WebXR immersive-ar oturumu açık. Çevreni kontrol et; görünürlük kaybında dur.';
 }catch(error){if(requested)await requested.end().catch(()=>{});session=null;el('xr-mode').textContent='Masaüstü önizleme · Ekipmansız';buttons.forEach(b=>b.visible=false);cue.mesh.visible=false;preview.visible=true;scene.background=null;el('xr-status').textContent=error.message||'WebXR AR oturumu açılamadı.';}
};
async function confirmSpace(){
 if(!canConfirmSpace({immersive:!!session,tracked:poseTracked,visible:session?.visibilityState==='visible'})){el('xr-status').textContent='Takip ve görünürlük hazır değil. Dur ve yeniden kontrol et.';return false;}
 // Desktop observations are explicitly synthetic. A headset's tracking signals come only from XRFrame.
 if(!session){await send('signal',{field:'tracking',enabled:true});await send('signal',{field:'passthrough',enabled:true});await send('signal',{field:'focus',enabled:!document.hidden});}
 await send('confirmSpace',{enabled:true});
}
async function newSession(){
 if(await send('dailyBack')){await send('confirmSpace',{enabled:false});el('summary-panel').hidden=true;previewPaused=false;}
}
function updateFlow(){
 const view=studioView(model,offline);el('primary-action').textContent=view.label;el('primary-action').disabled=!view.action;
 el('pause').disabled=offline;
 el('preview-play').disabled=!!model?.workout?.state&&model.workout.state!=='Ready';
 el('stop').disabled=!model?.workout?.isRunning&&model?.workout?.state!=='Paused';
 const summary=sessionSummary(model);
 if(summary){
  el('summary-title').textContent=summary.title;
  el('summary-copy').textContent=`${summary.workSeconds} saniye çalışma · ${summary.sets} tamamlanan set.\nBu cihazda ${summary.completedSessions} tamamlanan seans.`;
  el('summary-plan').textContent='Seans planı: '+summary.plan.join(' / ');
  if(lastTerminal!==model.workout.state){el('summary-panel').hidden=!!session;lastTerminal=model.workout.state;}
 }else{lastTerminal=null;el('summary-panel').hidden=true;}
 const list=el('history-list');list.replaceChildren();
 const rows=model?.history||[];
 if(!rows.length){const p=document.createElement('p');p.textContent='Henüz seans yok. Tamamlanan ve yarıda bitirilen seansların burada görünür.';list.append(p);}
 for(const row of rows.slice(0,8)){
  const item=document.createElement('article'),title=document.createElement('strong'),detail=document.createElement('p');
  title.textContent=row.outcome==='Completed'?'Tamamlandı':row.outcome==='Aborted'?'Sonlandırıldı':'Yarıda kaldı';
  const date=new Date(row.startedUtc);detail.textContent=`${Number.isNaN(date.getTime())?'Tarih yok':date.toLocaleDateString('tr-TR')} · ${Math.floor(row.activeSeconds||0)} sn çalışma · ${row.completedSets||0} set`;
  item.append(title,detail);list.append(item);
 }
}
el('primary-action').onclick=async()=>{
 const action=studioView(model,offline).action;
 if(action==='connect'){await readState();if(!offline)await pause();}
 else if(action==='setup'){el('setup-panel').hidden=false;updateSetup();}
 else if(action==='confirm')await confirmSpace();
 else if(action==='pause')await pause();
 else if(action==='start')await start();
 else if(action==='new')await newSession();
};
el('summary-new').onclick=newSession;
el('summary-close').onclick=()=>{el('summary-panel').hidden=true;};
el('history-open').onclick=()=>{updateFlow();el('history-panel').hidden=false;};
el('history-close').onclick=()=>{el('history-panel').hidden=true;};
function updateSetup(){
 if(!model)return;const setup=model.setup;el('setup-step').textContent=setup.step==='Complete'?'Kurulum tamamlandı':setup.title;
 el('setup-copy').textContent=setup.step==='Safety'?'Çevrende insan, evcil hayvan veya engel olmadığını kontrol et. Rahatsızlık hissedersen dur. Bu demo ekipmansızdır.':setup.body;
 el('safety-checks').hidden=setup.step!=='Safety';el('setup-next').textContent=setup.step==='Review'?'Kaydet ve koçuma dön':setup.step==='Complete'?'Koçuma dön':'Devam et';
}
el('controls-open').onclick=()=>{el('control-drawer').hidden=!el('control-drawer').hidden;};
el('preview-play').onclick=()=>{previewPaused=false;};
el('setup-open').onclick=()=>{el('setup-panel').hidden=false;updateSetup();};
el('setup-close').onclick=()=>{el('setup-panel').hidden=true;};
el('setup-next').onclick=async()=>{
 if(model?.setup?.step==='Complete'){el('setup-panel').hidden=true;return;}
 if(model?.setup?.step==='Welcome')await send('edit',{field:'language',value:'tr'});
 if(model?.setup?.step==='Safety'){
  if(!el('setup-ack').checked||!el('setup-space').checked){el('setup-copy').textContent='Devam etmek için güvenlik ve açık alan kontrollerini kendin onayla.';return;}
  await send('safety',{field:'ack',enabled:true});await send('safety',{field:'space',enabled:true});
 }
 await send(model?.setup?.step==='Review'?'finish':'next');if(model?.setup?.step==='Complete')el('setup-panel').hidden=true;
};
window.addEventListener('keydown',e=>{if(e.code==='Space'&&document.activeElement?.tagName!=='INPUT'){e.preventDefault();pause();}});
document.addEventListener('visibilitychange',()=>{if(!session){if(document.hidden)pause();send('signal',{field:'focus',enabled:!document.hidden});}});
scene.background=null;
renderer.setAnimationLoop((now,frame)=>{
 const dt=Math.min((now-lastFrame)/1000,.1);lastFrame=now;
 if(session&&frame){const reference=renderer.xr.getReferenceSpace();const tracked=!!reference&&!!frame.getViewerPose(reference);if(tracked!==poseTracked){poseTracked=tracked;if(!tracked)pause();send('signal',{field:'tracking',enabled:tracked});}if(tracked&&!placed)place();for(const source of session.inputSources){const pressed=!!source.gamepad?.buttons[5]?.pressed;if(pressed&&!pauseButtons.get(source))pause();pauseButtons.set(source,pressed);}}
 human?.setPlaying(shouldPlayCoach({state:model?.workout?.state,exerciseId:model?.workout?.exerciseId,offline,localPaused,previewPaused,visible:session?session.visibilityState==='visible':!document.hidden,reducedMotion:model?.setup?.profile?.reducedMotion||matchMedia('(prefers-reduced-motion: reduce)').matches}));
 if(now-lastClock>=250){const elapsed=(now-lastClock)/1000;lastClock=now;if(model?.workout?.isRunning&&!localPaused&&!offline&&(session?session.visibilityState==='visible':!document.hidden)){if(elapsed>1.5)pause();else send('tick',{seconds:elapsed});}}
 renderer.render(scene,camera);
});
await readState();if(model?.workout?.isRunning)await pause();await send('signal',{field:'focus',enabled:!document.hidden});capability=await inspectAR(navigator.xr,isSecureContext);el('xr-status').textContent=capability.reason;el('enter-xr').disabled=!capability.supported||!el('space-confirm').checked;paintPanel();
