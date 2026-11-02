import * as THREE from './vendor/three/three.module.js';
import {inspectAR,requestMR,isTransparentSession} from './xr-session.mjs';
const el=id=>document.getElementById(id),host=el('canvas-host');
let renderer;
try{renderer=new THREE.WebGLRenderer({antialias:true,alpha:true});}catch{el('xr-status').textContent='WebGL açılamadı. Donanım hızlandırmalı tarayıcı kullan.';throw new Error('FORMA_WEBGL_UNAVAILABLE');}
renderer.setPixelRatio(Math.min(devicePixelRatio,1.5));renderer.xr.enabled=true;renderer.xr.setReferenceSpaceType('local-floor');host.append(renderer.domElement);
const scene=new THREE.Scene(),camera=new THREE.PerspectiveCamera(48,1,.05,50);camera.position.set(0,1.65,1.4);camera.lookAt(0,1.15,-2);
scene.add(new THREE.HemisphereLight(0xffffff,0x73858c,2.4));const sun=new THREE.DirectionalLight(0xffead1,2.1);sun.position.set(2,5,3);scene.add(sun);
const preview=new THREE.Group();scene.add(preview);const floor=new THREE.Mesh(new THREE.PlaneGeometry(30,30),new THREE.MeshStandardMaterial({color:0xe2ebed,roughness:1}));floor.rotation.x=-Math.PI/2;preview.add(floor);
const grid=new THREE.GridHelper(12,12,0xb6cbd0,0xd0dfe2);grid.position.y=.005;preview.add(grid);
const content=new THREE.Group();content.position.z=-2;scene.add(content);
const shirt=new THREE.MeshStandardMaterial({color:0x217c83,roughness:.9}),skin=new THREE.MeshStandardMaterial({color:0xd7a77d,roughness:1}),ink=new THREE.MeshStandardMaterial({color:0x203b48,roughness:1});
const coach=new THREE.Group();coach.position.x=-.82;content.add(coach);
function mesh(geometry,material,x,y,z,parent=coach){const m=new THREE.Mesh(geometry,material);m.position.set(x,y,z);parent.add(m);return m;}
mesh(new THREE.CapsuleGeometry(.19,.36,4,10),shirt,0,1.16,0);mesh(new THREE.SphereGeometry(.145,12,10),skin,0,1.66,0);mesh(new THREE.SphereGeometry(.15,12,8,0,Math.PI*2,0,Math.PI*.48),ink,0,1.70,0);
for(const x of [-.05,.05])mesh(new THREE.SphereGeometry(.012,6,6),ink,x,1.68,.135);
const limbs=[];
for(const [x,y,length,material] of [[-.12,.87,.62,ink],[.12,.87,.62,ink],[-.29,1.43,.49,shirt],[.29,1.43,.49,shirt]]){
 const pivot=new THREE.Group();pivot.position.set(x,y,0);coach.add(pivot);mesh(new THREE.CapsuleGeometry(.065,length-.13,4,8),material,0,-length/2,0,pivot);limbs.push(pivot);
 if(y>1)mesh(new THREE.SphereGeometry(.069,8,6),skin,0,-length-.025,0,pivot);
}
function surface(width,height,pixelsW=1024,pixelsH=512){const canvas=document.createElement('canvas');canvas.width=pixelsW;canvas.height=pixelsH;const texture=new THREE.CanvasTexture(canvas);texture.colorSpace=THREE.SRGBColorSpace;const m=new THREE.Mesh(new THREE.PlaneGeometry(width,height),new THREE.MeshBasicMaterial({map:texture,transparent:true}));return {mesh:m,canvas,ctx:canvas.getContext('2d'),texture};}
const panel=surface(1.22,.69,1024,576);panel.mesh.position.set(.57,1.52,0);content.add(panel.mesh);
const buttons=[],rays=[],controllers=[],raycaster=new THREE.Raycaster(),rotation=new THREE.Matrix4();
const pauseButtons=new WeakMap();
let model=null,session=null,queue=Promise.resolve(),offline=false,localPaused=true,lastClock=performance.now(),lastFrame=performance.now(),phase=0,placed=false,lastPanel='',poseTracked=false,capability;
function wrap(ctx,text,x,y,maxWidth,lineHeight){let cursor=y;for(const paragraph of String(text).split('\n')){let line='';for(const word of paragraph.split(' ')){const test=line?line+' '+word:word;if(ctx.measureText(test).width>maxWidth&&line){ctx.fillText(line,x,cursor);cursor+=lineHeight;line=word;}else line=test;}ctx.fillText(line,x,cursor);cursor+=lineHeight;}return cursor;}
function paintPanel(){
 const state=model?.workout,p=model?.setup?.profile;
 el('session-state').textContent=`Seans: ${state?.state||'Hazır değil'} · ${Math.ceil(state?.remaining||0)} sn · ${state?.completedSets||0} set`;
 const text=offline?'BAĞLANTI KESİLDİ — HAREKETİ DURDUR':!model?'Bağlanıyor…':model.setup.step!=='Complete'?'Önce 2B sayfada kurulumu tamamla.':state?.state==='Ready'?model.daily?.greeting||'Alanını kontrol et ve kısa seans seç.':model.coach?.text||'';
 const signature=JSON.stringify({text,state,session:!!session,offline,space:model?.gate?.space});if(signature===lastPanel)return;lastPanel=signature;
 const c=panel.ctx;c.clearRect(0,0,1024,576);c.fillStyle='rgba(18,43,55,.96)';c.fillRect(0,0,1024,576);c.fillStyle='#b8dcd9';c.font='23px sans-serif';c.fillText(session?'FORMA / WebXR AR':'FORMA / 3B masaüstü önizleme',36,48);
 c.fillStyle='#fff';c.font='bold 38px sans-serif';c.fillText(state?.exerciseName||'Bugün, FORMA ile',36,108);
 c.font='bold 69px sans-serif';const seconds=Math.ceil(state?.remaining||0);c.fillText(`${String(Math.floor(seconds/60)).padStart(2,'0')}:${String(seconds%60).padStart(2,'0')}`,36,194);
 c.fillStyle='#b8dcd9';c.font='24px sans-serif';c.fillText(`${state?.state||'Ready'} / ${state?.completedSets||0} set / ${Math.floor(state?.activeSeconds||0)} çalışma saniyesi`,36,239);
 c.fillStyle='#fff';c.font='26px sans-serif';wrap(c,text,36,292,952,37);
 c.fillStyle='#e6ba75';c.font='22px sans-serif';c.fillText(model?.gate?.space?'Alan onaylı · Ekipmansız':'Alanı yeniden onayla · Ekipmansız',36,543);panel.texture.needsUpdate=true;
 if(p){const color=model.coach?.color||'#217c83';shirt.color.set(color);}
}
async function readState(){try{const r=await fetch('/api/state');if(!r.ok)throw new Error();model=await r.json();offline=false;el('connection-status').textContent='Ortak C# motoru bağlı · Veriler yerel';paintPanel();}catch{failure();}}
function failure(){offline=true;localPaused=true;el('connection-status').textContent='Yerel motor bağlantısı kesildi. Hareketi durdur ve bağlantıyı kontrol et.';paintPanel();}
function send(action,extra={}){
 const result=queue.then(async()=>{try{const r=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify({action,...extra})});const data=await r.json();if(!r.ok)throw new Error();model=data.state;offline=false;if(model.workout?.state==='Paused')localPaused=true;if((action==='resume'||action==='start')&&data.accepted){localPaused=false;lastClock=performance.now();}el('connection-status').textContent=model.notice||'Ortak C# motoru bağlı · Veriler yerel';paintPanel();return data.accepted;}catch{failure();return false;}});
 queue=result.catch(()=>false);return result;
}
function pause(){localPaused=true;return send('pause');}
async function start(){if(model?.setup.step!=='Complete'){el('xr-status').textContent='Önce Bugüne dön bağlantısından kurulumu tamamla.';return;}if(model?.workout?.state==='Paused'){await send('resume');return;}if(!model?.daily?.enabled||model.daily.stage!=='Recommendation')await send('dailyQuick');await send('start');}
function button(label,x,y,action,color='#217c83'){
 const b=surface(.56,.115,768,160);b.mesh.position.set(x,y,.012);const c=b.ctx;c.fillStyle=color;c.fillRect(0,0,768,160);c.fillStyle='#fff';c.font='bold 40px sans-serif';c.textAlign='center';c.fillText(label,384,96);b.texture.needsUpdate=true;b.mesh.userData.action=action;content.add(b.mesh);buttons.push(b.mesh);return b;
}
button('Alanı onayla',.25,1.08,()=>send('confirmSpace',{enabled:true}));button('Seans öner',.9,1.08,()=>send('dailyQuick'));
button('Başlat / Devam',.25,.91,start);button('DURAKLAT',.9,.91,pause,'#a13b36');
button('Seansı bitir',.25,.74,()=>send('stop'));button('XR çıkış',.9,.74,async()=>{await pause();if(session)await session.end();});
button('Önüme yerleştir',.57,.57,()=>{place();send('pause');send('confirmSpace',{enabled:false});});
function choose(origin,direction){raycaster.set(origin,direction);const hit=raycaster.intersectObjects(buttons,false)[0];if(hit)hit.object.userData.action();}
for(let i=0;i<2;i++){
 const controller=renderer.xr.getController(i);scene.add(controller);controllers.push(controller);
 const geometry=new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(),new THREE.Vector3(0,0,-1)]);const line=new THREE.Line(geometry,new THREE.LineBasicMaterial({color:0x74c8c7}));line.scale.z=3;controller.add(line);rays.push(line);
 controller.addEventListener('select',()=>{rotation.identity().extractRotation(controller.matrixWorld);choose(new THREE.Vector3().setFromMatrixPosition(controller.matrixWorld),new THREE.Vector3(0,0,-1).applyMatrix4(rotation));});
}
function place(){const cam=renderer.xr.isPresenting?renderer.xr.getCamera():camera;const position=new THREE.Vector3();cam.getWorldPosition(position);const forward=new THREE.Vector3();cam.getWorldDirection(forward);forward.y=0;if(forward.lengthSq()<.01)forward.set(0,0,-1);forward.normalize();content.position.copy(position).addScaledVector(forward,2.3);content.position.y=position.y-1.5;content.rotation.y=Math.atan2(-forward.x,-forward.z);placed=true;}
function resize(){renderer.setSize(host.clientWidth,host.clientHeight);camera.aspect=host.clientWidth/host.clientHeight;camera.updateProjectionMatrix();}window.addEventListener('resize',resize);resize();
let drag=null;
renderer.domElement.addEventListener('pointerdown',e=>{drag={x:e.clientX,y:e.clientY,moved:false};});
renderer.domElement.addEventListener('pointermove',e=>{if(!drag||session)return;const dx=e.clientX-drag.x,dy=e.clientY-drag.y;if(Math.abs(dx)+Math.abs(dy)>3)drag.moved=true;if(drag.moved){camera.rotation.y-=dx*.001;camera.rotation.x-=dy*.001;drag.x=e.clientX;drag.y=e.clientY;}});
renderer.domElement.addEventListener('pointerup',e=>{if(drag&&!drag.moved&&!session){const rect=renderer.domElement.getBoundingClientRect();raycaster.setFromCamera(new THREE.Vector2((e.clientX-rect.left)/rect.width*2-1,-(e.clientY-rect.top)/rect.height*2+1),camera);const hit=raycaster.intersectObjects(buttons,false)[0];hit?.object.userData.action();}drag=null;});
el('confirm-space').onclick=()=>send('confirmSpace',{enabled:true});el('prepare').onclick=()=>send('dailyQuick');el('start').onclick=start;el('pause').onclick=pause;el('stop').onclick=()=>send('stop');el('reposition').onclick=()=>{place();pause();send('confirmSpace',{enabled:false});};
el('leave-xr').onclick=async()=>{await pause();await session?.end();};
el('space-confirm').onchange=()=>{el('enter-xr').disabled=!capability?.supported||!el('space-confirm').checked;};
el('enter-xr').onclick=async()=>{
 let requested;
 try{
  requested=await requestMR(navigator.xr,{secure:isSecureContext,confirmed:el('space-confirm').checked,noEquipment:el('space-confirm').checked});
  if(!isTransparentSession(requested)){await requested.end();throw new Error('Şeffaf AR bileşimi doğrulanamadı. Opaque VR moduna geçilmedi.');}
  session=requested;await pause();placed=false;poseTracked=false;preview.visible=false;scene.background=null;renderer.setClearColor(0x000000,0);
  session.addEventListener('visibilitychange',()=>{const visible=session?.visibilityState==='visible';if(!visible)pause();send('signal',{field:'focus',enabled:visible});});
  session.addEventListener('end',()=>{session=null;preview.visible=true;scene.background=new THREE.Color(0xe9f0f2);el('leave-xr').hidden=true;el('enter-xr').hidden=false;el('space-confirm').checked=false;el('enter-xr').disabled=true;pause();el('xr-status').textContent='XR oturumu kapandı. Egzersiz duraklatıldı; otomatik devam yok.';paintPanel();});
  await renderer.xr.setSession(session);await send('signal',{field:'passthrough',enabled:true});await send('signal',{field:'focus',enabled:session.visibilityState==='visible'});el('leave-xr').hidden=false;el('enter-xr').hidden=true;el('xr-status').textContent='Gerçek WebXR immersive-ar oturumu açık. Çevreni kontrol et; görünürlük kaybında dur.';
 }catch(error){if(requested)await requested.end().catch(()=>{});session=null;preview.visible=true;scene.background=new THREE.Color(0xe9f0f2);el('xr-status').textContent=error.message||'WebXR AR oturumu açılamadı.';}
};
window.addEventListener('keydown',e=>{if(e.code==='Space'&&document.activeElement?.tagName!=='INPUT'){e.preventDefault();pause();}});
document.addEventListener('visibilitychange',()=>{if(!session){if(document.hidden)pause();send('signal',{field:'focus',enabled:!document.hidden});}});
scene.background=new THREE.Color(0xe9f0f2);
renderer.setAnimationLoop((now,frame)=>{
 const dt=Math.min((now-lastFrame)/1000,.1);lastFrame=now;
 if(session&&frame){const reference=renderer.xr.getReferenceSpace();const tracked=!!reference&&!!frame.getViewerPose(reference);if(tracked!==poseTracked){poseTracked=tracked;if(!tracked)pause();send('signal',{field:'tracking',enabled:tracked});}if(tracked&&!placed)place();for(const source of session.inputSources){const pressed=!!source.gamepad?.buttons[5]?.pressed;if(pressed&&!pauseButtons.get(source))pause();pauseButtons.set(source,pressed);}}
 const demonstrating=model?.workout?.state==='Introduction',moving=(model?.workout?.state==='Work'||demonstrating)&&!localPaused&&!offline;
 if(moving)phase+=dt*3;
 const a=moving?Math.sin(phase)*.13:0;limbs[0].rotation.x=model?.workout?.exerciseId==='march'?a:0;limbs[1].rotation.x=-limbs[0].rotation.x;limbs[2].rotation.z=a;limbs[3].rotation.z=-a;
 if(now-lastClock>=250){const elapsed=(now-lastClock)/1000;lastClock=now;if(model?.workout?.isRunning&&!localPaused&&!offline&&(session?session.visibilityState==='visible':!document.hidden)){if(elapsed>1.5)pause();else send('tick',{seconds:elapsed});}}
 renderer.render(scene,camera);
});
await readState();if(model?.workout?.isRunning)await pause();await send('signal',{field:'focus',enabled:!document.hidden});capability=await inspectAR(navigator.xr,isSecureContext);el('xr-status').textContent=capability.reason;el('enter-xr').disabled=!capability.supported||!el('space-confirm').checked;paintPanel();
