import * as THREE from './vendor/three/three.module.js';
import {createPreviewRoom,createHumanCoach} from './studio-scene.mjs';
import {inspectAR,requestMR,isTransparentSession} from './xr-session.mjs';
const el=id=>document.getElementById(id),host=el('canvas-host');
let renderer;
try{renderer=new THREE.WebGLRenderer({antialias:true,alpha:true});}catch{el('xr-status').textContent='WebGL açılamadı. Donanım hızlandırmalı tarayıcı kullan.';throw new Error('FORMA_WEBGL_UNAVAILABLE');}
renderer.setPixelRatio(Math.min(devicePixelRatio,1.5));renderer.xr.enabled=true;renderer.xr.setReferenceSpaceType('local-floor');host.append(renderer.domElement);
const scene=new THREE.Scene(),camera=new THREE.PerspectiveCamera(48,1,.05,50);camera.position.set(0,1.52,2.45);camera.lookAt(0,1.05,-1.2);
scene.add(new THREE.HemisphereLight(0xffffff,0x73858c,2.4));const sun=new THREE.DirectionalLight(0xffead1,2.1);sun.position.set(2,5,3);scene.add(sun);
const preview=createPreviewRoom();scene.add(preview);
const content=new THREE.Group();content.position.z=-1.2;scene.add(content);
const coach=new THREE.Group();coach.position.x=-.16;content.add(coach);
let human=null;
createHumanCoach(coach,text=>el('asset-status').textContent=text).then(value=>{human=value;}).catch(error=>{el('asset-status').textContent='3B koç yüklenemedi. Yeniden yükle.';console.error(error);});
function surface(width,height,pixelsW=1024,pixelsH=512){const canvas=document.createElement('canvas');canvas.width=pixelsW;canvas.height=pixelsH;const texture=new THREE.CanvasTexture(canvas);texture.colorSpace=THREE.SRGBColorSpace;const m=new THREE.Mesh(new THREE.PlaneGeometry(width,height),new THREE.MeshBasicMaterial({map:texture,transparent:true}));return {mesh:m,canvas,ctx:canvas.getContext('2d'),texture};}
const panel=surface(.68,1.1,640,1024);panel.mesh.position.set(1.02,1.2,0);content.add(panel.mesh);
const cue=surface(1.55,.23,1536,230);cue.mesh.position.set(-.16,1.99,0);content.add(cue.mesh);
const timeline=surface(1.72,.22,1536,200);timeline.mesh.position.set(-.16,.18,.18);content.add(timeline.mesh);
const buttons=[],rays=[],controllers=[],raycaster=new THREE.Raycaster(),rotation=new THREE.Matrix4();
const pauseButtons=new WeakMap();
let model=null,session=null,queue=Promise.resolve(),offline=false,localPaused=true,lastClock=performance.now(),lastFrame=performance.now(),phase=0,placed=false,lastPanel='',poseTracked=false,capability;
function wrap(ctx,text,x,y,maxWidth,lineHeight){let cursor=y;for(const paragraph of String(text).split('\n')){let line='';for(const word of paragraph.split(' ')){const test=line?line+' '+word:word;if(ctx.measureText(test).width>maxWidth&&line){ctx.fillText(line,x,cursor);cursor+=lineHeight;line=word;}else line=test;}ctx.fillText(line,x,cursor);cursor+=lineHeight;}return cursor;}
function paintPanel(){
 const state=model?.workout;
 el('session-state').textContent=`${state?.state||'Hazır'} · ${Math.ceil(state?.remaining||0)} sn · ${state?.completedSets||0} set`;
 const text=offline?'Bağlantı kesildi. Hareketi durdur.':!model?'Koçuna bağlanılıyor…':model.setup.step!=='Complete'?'Başlamadan önce kısa kurulumu tamamla.':state?.state==='Ready'?'Alanını kontrol et. Birlikte başlayalım.':model.coach?.text||'';
 const signature=JSON.stringify({text,state,setup:model?.setup?.step,language:model?.setup?.profile?.language,session:!!session,offline,space:model?.gate?.space});if(signature===lastPanel)return;lastPanel=signature;
 const c=panel.ctx;c.clearRect(0,0,640,1024);
 c.fillStyle='rgba(28,32,31,.87)';c.beginPath();c.roundRect(12,12,616,1000,270);c.fill();
 const seconds=Math.ceil(state?.remaining||0),duration=state?.intervalSeconds||1;
 const progress=Math.max(0,Math.min(1,seconds/duration));
 c.lineWidth=18;c.lineCap='round';c.strokeStyle='rgba(255,255,255,.13)';c.beginPath();c.arc(320,330,268,-Math.PI*.9,Math.PI*.9);c.stroke();
 c.strokeStyle='#edcf63';c.beginPath();c.arc(320,330,268,-Math.PI*.9,-Math.PI*.9+Math.PI*1.8*progress);c.stroke();
 c.fillStyle='#f6f3e7';c.textAlign='center';c.font='500 112px sans-serif';c.fillText(`${Math.floor(seconds/60)}:${String(seconds%60).padStart(2,'0')}`,320,254);
 c.fillStyle='#d5d6cc';c.font='28px sans-serif';c.fillText(state?.state==='Rest'?'Dinlenme':state?.state==='Paused'?'Duraklatıldı':state?.state==='Completed'?'Tamamlandı':'Kalan süre',320,302);
 // Explicit illustration, not a claim of tracked body or measured muscle activation.
 c.strokeStyle='#d3d4cc';c.fillStyle='#d3d4cc';c.lineWidth=24;
 c.beginPath();c.arc(320,414,35,0,Math.PI*2);c.fill();
 c.beginPath();c.moveTo(320,466);c.lineTo(320,660);c.moveTo(320,494);c.lineTo(242,590);c.moveTo(320,494);c.lineTo(398,590);c.moveTo(320,654);c.lineTo(265,814);c.moveTo(320,654);c.lineTo(375,814);c.stroke();
 c.strokeStyle='#edcf63';c.lineWidth=32;c.beginPath();if(state?.exerciseId==='march'){c.moveTo(320,659);c.lineTo(280,755);c.moveTo(320,659);c.lineTo(360,755);}else{c.moveTo(274,496);c.lineTo(366,496);}c.stroke();
 c.fillStyle='#fff';c.font='32px sans-serif';c.fillText(`Set ${state?.set||1} / ${state?.totalSets||2}`,320,887);c.fillStyle='#bfc2b8';c.font='23px sans-serif';c.fillText('Hareket rehberi · Ölçüm değil',320,940);panel.texture.needsUpdate=true;
 const q=cue.ctx;q.clearRect(0,0,1536,230);q.fillStyle='rgba(28,32,31,.85)';q.beginPath();q.roundRect(0,0,1536,230,26);q.fill();q.textAlign='center';q.fillStyle='#edcf63';q.font='30px sans-serif';q.fillText(state?.exerciseId==='march'?'Hafif yerinde yürüyüş':'Rahat omuz çevirme',768,54);q.fillStyle='#fff';q.font='35px sans-serif';wrap(q,text,768,112,1400,44);cue.texture.needsUpdate=true;
 const t=timeline.ctx;t.clearRect(0,0,1536,200);t.fillStyle='rgba(28,32,31,.9)';t.beginPath();t.roundRect(0,0,1536,200,24);t.fill();t.textAlign='center';
 const entries=[...(model?.plan||[]).map(e=>[e.id,e.id==='march'?'Yerinde yürüyüş':e.id==='shoulders'?'Omuz çevirme':e.name,`${e.sets} × ${e.workSeconds} saniye`]),['summary','Seans özeti','Yerel geçmiş']];const tileWidth=1536/entries.length;
 entries.forEach(([id,name,dose],i)=>{const active=id===state?.exerciseId||(id==='summary'&&state?.state==='Completed');t.fillStyle=active?'#edcf63':'#e5e4dc';t.font='34px sans-serif';t.fillText(name,tileWidth*(i+.5),91);t.fillStyle='#afb4aa';t.font='25px sans-serif';t.fillText(dose,tileWidth*(i+.5),139);if(active){t.fillStyle='#edcf63';t.fillRect(i*tileWidth+30,176,tileWidth-60,5);}});timeline.texture.needsUpdate=true;
 el('workout-title').textContent=state?.state==='Completed'?'Seans tamamlandı':state?.exerciseId==='march'?'Yerinde yürüyüş':'Omuz çevirme';
 el('space-state').textContent=model?.gate?.space?'Alan onaylı':'Başlamak için alanını onayla';
 updateSetup();
}
async function readState(){try{const r=await fetch('/api/state');if(!r.ok)throw new Error();model=await r.json();offline=false;el('connection-status').textContent='Ortak C# motoru bağlı · Veriler yerel';paintPanel();}catch{failure();}}
function failure(){offline=true;localPaused=true;el('connection-status').textContent='Yerel motor bağlantısı kesildi. Hareketi durdur ve bağlantıyı kontrol et.';paintPanel();}
function send(action,extra={}){
 const result=queue.then(async()=>{try{const r=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify({action,...extra})});const data=await r.json();if(!r.ok)throw new Error();model=data.state;offline=false;if(model.workout?.state==='Paused')localPaused=true;if((action==='resume'||action==='start')&&data.accepted){localPaused=false;lastClock=performance.now();}el('connection-status').textContent=model.notice||'Ortak C# motoru bağlı · Veriler yerel';paintPanel();return data.accepted;}catch{failure();return false;}});
 queue=result.catch(()=>false);return result;
}
function pause(){localPaused=true;return send('pause');}
async function start(){if(model?.setup.step!=='Complete'){el('setup-panel').hidden=false;updateSetup();return;}if(model?.workout?.state==='Paused'){await send('resume');return;}if(!model?.daily?.enabled||model.daily.stage!=='Recommendation')await send('dailyQuick');await send('start');}
function button(label,x,y,action,color='#313b3a'){
 const b=surface(.56,.115,768,160);b.mesh.position.set(x,y,.012);const c=b.ctx;c.fillStyle=color;c.fillRect(0,0,768,160);c.fillStyle='#fff';c.font='500 40px sans-serif';c.textAlign='center';c.fillText(label,384,96);b.texture.needsUpdate=true;b.mesh.userData.action=action;content.add(b.mesh);buttons.push(b.mesh);return b;
}
button('Alanı onayla',-1.12,.64,()=>send('confirmSpace',{enabled:true}));button('Seans öner',-1.12,.47,()=>send('dailyQuick'));
button('Başlat / Devam',-1.12,.30,start,'#71652f');button('Ⅱ  DURAKLAT',1.02,1.94,pause,'#6c3a32');
button('Seansı bitir',1.02,.47,()=>send('stop'));button('XR çıkış',1.02,.30,async()=>{await pause();if(session)await session.end();});
button('Koçu yerleştir',-1.12,.13,()=>{place();pause();send('confirmSpace',{enabled:false});});
function choose(origin,direction){raycaster.set(origin,direction);const hit=raycaster.intersectObjects(buttons,false)[0];if(hit)hit.object.userData.action();}
for(let i=0;i<2;i++){
 const controller=renderer.xr.getController(i);scene.add(controller);controllers.push(controller);
 const geometry=new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(),new THREE.Vector3(0,0,-1)]);const line=new THREE.Line(geometry,new THREE.LineBasicMaterial({color:0x74c8c7}));line.scale.z=3;controller.add(line);rays.push(line);
 controller.addEventListener('select',()=>{rotation.identity().extractRotation(controller.matrixWorld);choose(new THREE.Vector3().setFromMatrixPosition(controller.matrixWorld),new THREE.Vector3(0,0,-1).applyMatrix4(rotation));});
}
function place(){const cam=renderer.xr.isPresenting?renderer.xr.getCamera():camera;const position=new THREE.Vector3();cam.getWorldPosition(position);const forward=new THREE.Vector3();cam.getWorldDirection(forward);forward.y=0;if(forward.lengthSq()<.01)forward.set(0,0,-1);forward.normalize();content.position.copy(position).addScaledVector(forward,2.3);content.position.y=0;content.rotation.y=Math.atan2(-forward.x,-forward.z);placed=true;}
function resize(){renderer.setSize(host.clientWidth,host.clientHeight);camera.aspect=host.clientWidth/host.clientHeight;camera.fov=camera.aspect<1?62:48;camera.updateProjectionMatrix();}window.addEventListener('resize',resize);resize();
let drag=null;
renderer.domElement.addEventListener('pointerdown',e=>{drag={x:e.clientX,y:e.clientY,moved:false};});
renderer.domElement.addEventListener('pointermove',e=>{if(!drag||session)return;const dx=e.clientX-drag.x,dy=e.clientY-drag.y;if(Math.abs(dx)+Math.abs(dy)>3)drag.moved=true;if(drag.moved){camera.rotation.y-=dx*.001;camera.rotation.x-=dy*.001;drag.x=e.clientX;drag.y=e.clientY;}});
renderer.domElement.addEventListener('pointerup',e=>{if(drag&&!drag.moved&&!session){const rect=renderer.domElement.getBoundingClientRect();raycaster.setFromCamera(new THREE.Vector2((e.clientX-rect.left)/rect.width*2-1,-(e.clientY-rect.top)/rect.height*2+1),camera);const hit=raycaster.intersectObjects(buttons,false)[0];hit?.object.userData.action();}drag=null;});
el('confirm-space').onclick=()=>send('confirmSpace',{enabled:true});el('prepare').onclick=()=>send('dailyQuick');el('start').onclick=start;el('pause').onclick=pause;el('stop').onclick=()=>send('stop');el('reposition').onclick=()=>{place();pause();send('confirmSpace',{enabled:false});};
el('leave-xr').onclick=async()=>{await pause();await session?.end();};
el('space-confirm').onchange=()=>{el('enter-xr').disabled=!capability?.supported||!el('space-confirm').checked;};
el('enter-xr').onclick=async()=>{
 let requested;
 if(model?.setup?.step!=='Complete'){el('setup-panel').hidden=false;updateSetup();return;}
 try{
  requested=await requestMR(navigator.xr,{secure:isSecureContext,confirmed:el('space-confirm').checked,noEquipment:el('space-confirm').checked});
  if(!isTransparentSession(requested)){await requested.end();throw new Error('Şeffaf AR bileşimi doğrulanamadı. Opaque VR moduna geçilmedi.');}
  session=requested;await pause();placed=false;poseTracked=false;preview.visible=false;scene.background=null;renderer.setClearColor(0x000000,0);
  session.addEventListener('visibilitychange',()=>{const visible=session?.visibilityState==='visible';if(!visible)pause();send('signal',{field:'focus',enabled:visible});});
  session.addEventListener('end',()=>{session=null;preview.visible=true;scene.background=new THREE.Color(0xe1e0d5);el('leave-xr').hidden=true;el('enter-xr').hidden=false;el('space-confirm').checked=false;el('enter-xr').disabled=true;pause();el('xr-status').textContent='XR oturumu kapandı. Egzersiz duraklatıldı; otomatik devam yok.';paintPanel();});
  await renderer.xr.setSession(session);await send('signal',{field:'passthrough',enabled:true});await send('signal',{field:'focus',enabled:session.visibilityState==='visible'});el('leave-xr').hidden=false;el('enter-xr').hidden=true;el('xr-status').textContent='Gerçek WebXR immersive-ar oturumu açık. Çevreni kontrol et; görünürlük kaybında dur.';
 }catch(error){if(requested)await requested.end().catch(()=>{});session=null;preview.visible=true;scene.background=new THREE.Color(0xe1e0d5);el('xr-status').textContent=error.message||'WebXR AR oturumu açılamadı.';}
};
function updateSetup(){
 if(!model)return;const setup=model.setup;el('setup-step').textContent=setup.step==='Complete'?'Kurulum tamamlandı':setup.title;
 el('setup-copy').textContent=setup.step==='Safety'?'Çevrende insan, evcil hayvan veya engel olmadığını kontrol et. Rahatsızlık hissedersen dur. Bu demo ekipmansızdır.':setup.body;
 el('safety-checks').hidden=setup.step!=='Safety';el('setup-next').textContent=setup.step==='Review'?'Kaydet ve koçuma dön':setup.step==='Complete'?'Koçuma dön':'Devam et';
}
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
scene.background=new THREE.Color(0xe1e0d5);
renderer.setAnimationLoop((now,frame)=>{
 const dt=Math.min((now-lastFrame)/1000,.1);lastFrame=now;
 if(session&&frame){const reference=renderer.xr.getReferenceSpace();const tracked=!!reference&&!!frame.getViewerPose(reference);if(tracked!==poseTracked){poseTracked=tracked;if(!tracked)pause();send('signal',{field:'tracking',enabled:tracked});}if(tracked&&!placed)place();for(const source of session.inputSources){const pressed=!!source.gamepad?.buttons[5]?.pressed;if(pressed&&!pauseButtons.get(source))pause();pauseButtons.set(source,pressed);}}
 const demonstrating=model?.workout?.state==='Introduction',moving=(model?.workout?.state==='Work'||demonstrating)&&!localPaused&&!offline;
 if(moving)phase+=dt*2.6;
 human?.update(phase,moving,model?.workout?.exerciseId,model?.setup?.profile?.reducedMotion||matchMedia('(prefers-reduced-motion: reduce)').matches);
 if(now-lastClock>=250){const elapsed=(now-lastClock)/1000;lastClock=now;if(model?.workout?.isRunning&&!localPaused&&!offline&&(session?session.visibilityState==='visible':!document.hidden)){if(elapsed>1.5)pause();else send('tick',{seconds:elapsed});}}
 renderer.render(scene,camera);
});
await readState();if(model?.workout?.isRunning)await pause();await send('signal',{field:'focus',enabled:!document.hidden});capability=await inspectAR(navigator.xr,isSecureContext);el('xr-status').textContent=capability.reason;el('enter-xr').disabled=!capability.supported||!el('space-confirm').checked;paintPanel();
