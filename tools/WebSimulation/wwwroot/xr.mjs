import {createMotionDiagnostics} from './motion-diagnostics.mjs';
import {CoachingOrchestrator} from './motion-events.mjs';
import {livingCoach,trackingCapabilities} from './living-coach.mjs';
import {createCoachVoice,livingInstruction} from './coach-voice.mjs';
import * as THREE from './vendor/three/three.module.js';
import {createHumanCoach} from './studio-scene.mjs';
import {createHolographicStage,applyHologram} from './holographic-studio.mjs';
import {createEnvironmentManager} from './environment-manager.mjs';
import {createStudioComposer} from './studio-composer.mjs';
import {createProgressUI} from './progress-ui.mjs';
import {createPersonalCoachUI} from './personal-coach-ui.mjs';
import {coachPresentation} from './coach-presentation.mjs';
import {studioCopy,applyStudioCopy} from './studio-copy.mjs';
import {studioView,sessionSummary,canConfirmSpace} from './studio-flow.mjs';
import {inspectAR,requestMR,isTransparentSession,inspectVR,requestWorldVR} from './xr-session.mjs';
const el=id=>document.getElementById(id),host=el('canvas-host');
let renderer;
try{renderer=new THREE.WebGLRenderer({antialias:true,alpha:true});}catch{el('xr-status').textContent='WebGL açılamadı. Donanım hızlandırmalı tarayıcı kullan.';throw new Error('FORMA_WEBGL_UNAVAILABLE');}
renderer.toneMapping=THREE.ACESFilmicToneMapping;renderer.toneMappingExposure=1.25;
renderer.info.autoReset=false;
renderer.setPixelRatio(Math.min(devicePixelRatio,1.5));renderer.xr.enabled=true;renderer.xr.setReferenceSpaceType('local-floor');host.append(renderer.domElement);
const scene=new THREE.Scene(),camera=new THREE.PerspectiveCamera(48,1,.05,50);camera.position.set(2.65,2.25,4.8);camera.lookAt(0,1.1,-1.2);
scene.add(new THREE.HemisphereLight(0x91c6ff,0x05091b,.9));const sun=new THREE.DirectionalLight(0x9cdeff,2.8);sun.position.set(1,4,4);scene.add(sun);scene.fog=new THREE.FogExp2(0x080f21,.035);
const environment=createEnvironmentManager();environment.select('HolographicStudio');const preview=environment.root;scene.add(preview);renderer.setClearColor(0x000000,0);
const content=new THREE.Group();content.position.z=-1.2;scene.add(content);
const holographicStage=createHolographicStage();content.add(holographicStage);
const coach=new THREE.Group();coach.position.set(-.24,0,0);content.add(coach);
const composer=createStudioComposer(renderer);
let human=null,previewPaused=false;
createHumanCoach(coach,text=>el('asset-status').textContent=text).then(value=>{human=value;if(human.motionError){el('motion-asset-error').hidden=false;el('motion-asset-error').textContent='Diz animasyonu yüklenemedi / Knee animation unavailable: '+human.motionError;pause();}applyHologram(human.avatar);el('asset-status').textContent='Canlı 3B rehber · Temsili hareketler';}).catch(error=>{el('asset-status').textContent='3B koç yüklenemedi; seansı başlatma.';console.error(error);});
function surface(width,height,pixelsW=1024,pixelsH=512){const canvas=document.createElement('canvas');canvas.width=pixelsW;canvas.height=pixelsH;const texture=new THREE.CanvasTexture(canvas);texture.colorSpace=THREE.SRGBColorSpace;const m=new THREE.Mesh(new THREE.PlaneGeometry(width,height),new THREE.MeshBasicMaterial({map:texture,transparent:true}));return {mesh:m,canvas,ctx:canvas.getContext('2d'),texture};}
const panel=surface(.83,1.36,640,1024);panel.mesh.position.set(.78,1.38,-.20);content.add(panel.mesh);
const cue=surface(1.55,.38,1536,360);cue.mesh.position.set(0,1.9,0);cue.mesh.visible=false;content.add(cue.mesh);
const timeline=surface(2.6,.23,1536,200);timeline.mesh.position.set(.06,.14,1.15);timeline.mesh.rotation.x=-.55;content.add(timeline.mesh);
const buttons=[],rays=[],controllers=[],raycaster=new THREE.Raycaster(),rotation=new THREE.Matrix4();
const pauseButtons=new WeakMap();
let modelAt=performance.now();
let model=null,session=null,queue=Promise.resolve(),offline=false,localPaused=true,lastClock=performance.now(),lastFrame=performance.now(),phase=0,placed=false,lastPanel='',poseTracked=false,capability,lastTerminal=null,sessionMode='ar',vrCapability,showXRProgress=false,xrFeedbackOutcome='Completed',xrFeedbackKey='';
const personalUI=createPersonalCoachUI({send,confirmSpace,getModel:()=>model});
let motionCaption=null;
let orchestrator=null,orchestratorDefinition=null,motionVoicePaused=false;
let wasLivingVoice=false;let voiceStatus='muted';const voice=createCoachVoice({onStatus:status=>{voiceStatus=status;}});
let capabilities=trackingCapabilities();
const progressUI=createProgressUI({send,readState,getModel:()=>model});
const diagnostics=createMotionDiagnostics({camera,scene,coach,getHuman:()=>human,onEnter:()=>pause(),getCanvas:()=>renderer.domElement});
document.addEventListener('forma-athlete-updated',()=>readState());
function wrap(ctx,text,x,y,maxWidth,lineHeight){let cursor=y;for(const paragraph of String(text).split('\n')){let line='';for(const word of paragraph.split(' ')){const test=line?line+' '+word:word;if(ctx.measureText(test).width>maxWidth&&line){ctx.fillText(line,x,cursor);cursor+=lineHeight;line=word;}else line=test;}ctx.fillText(line,x,cursor);cursor+=lineHeight;}return cursor;}
function paintPanel(){
 const l=studioCopy(model);applyStudioCopy(l,model,session,sessionMode);if(human)el('asset-status').textContent=l.asset;
 if(model?.athlete?.profile&&model?.personal?.greeting)el('athlete-welcome').textContent=model.personal.greeting;
 progressUI.render(model);
 const chosenEnvironment=model?.athlete?.profile?.preferences?.preferredEnvironment==='Forest'?'Forest':'HolographicStudio';
 if(environment.select(chosenEnvironment)&&(!session||sessionMode==='vr')){scene.background=new THREE.Color(environment.background);scene.fog=new THREE.FogExp2(environment.background,environment.fogDensity);}
 el('environment-select').value=chosenEnvironment;el('environment-select').disabled=!model?.athlete?.profile||!!model?.workout?.isRunning||model?.workout?.state==='Paused'||model?.personal?.session?.record?.outcome==='Interrupted';
 el('living-qa').hidden=!model?.fixtureData;
 const living= model?.workout?.workoutId==='forma-start-5-v1';document.body.classList.toggle('living-session',living);
 el('living-hud').hidden=!living;const liveSeconds=Math.ceil(model?.workout?.remaining||0);el('living-timer').textContent=model?.personal?.session?.awaitingFeedback?'—':`${Math.floor(liveSeconds/60)}:${String(liveSeconds%60).padStart(2,'0')}`;el('living-next').textContent=l.next+': '+(model?.workout?.nextExercise||'—');el('living-skip').textContent=l.skip;el('living-stop').textContent=l.stop;el('living-skip').disabled=model?.workout?.state!=='Work'||!!model?.personal?.session?.awaitingFeedback;el('living-stop').disabled=!model?.workout?.isRunning&&model?.workout?.state!=='Paused';
 const state=model?.workout?.state==='Ready'&&!model?.personal?.enabled?null:model?.workout;
 el('session-state').textContent=state?`${l.states[state.state]||l.states.Ready} · ${Math.ceil(state.remaining||0)} s · ${state.completedSets||0}`:l.states.Ready;
 let text=offline?l.offline:!model?l.loading:model.setup.step!=='Complete'?l.setupFirst:(!state&&!model?.personal?.enabled)||state?.state==='Ready'?l.ready:model.coach?.text||l.ready;
 if(model?.personal?.enabled && state?.state==='Work' && !model.personal.catalog?.find(e=>e.id===state.exerciseId)?.animation)text=personalUI.copy().fallback+' '+text;
 const liveInstruction=livingInstruction(model,livingCoach(model,{offline,localPaused,visible:session?session.visibilityState==='visible':!document.hidden}),{offline,localPaused,visible:session?session.visibilityState==='visible':!document.hidden});if(liveInstruction)text=liveInstruction.text;if(motionCaption&&state?.exerciseId==='knees'&&state.state==='Work'&&!offline&&!localPaused)text=motionCaption;
 const signature=JSON.stringify({text,state,setup:model?.setup?.step,language:model?.setup?.profile?.language,session:!!session,offline,space:model?.gate?.space,historyAvailable:model?.historyAvailable,athlete:model?.athlete?.revision,personal:model?.personal?{status:model.personal.status,id:model.personal.recommendation?.id,state:model.personal.session?.state,feedback:model.personal.session?.record?.feedback?.length,save:model.personal.savePending}:null});if(signature===lastPanel)return;lastPanel=signature;
 const c=panel.ctx;c.clearRect(0,0,640,1024);
 c.fillStyle='rgba(7,22,46,.46)';c.beginPath();c.roundRect(4,4,632,1016,24);c.fill();
 c.strokeStyle='rgba(126,226,255,.45)';c.lineWidth=2;c.stroke();
 c.textAlign='left';c.fillStyle='#8deeff';c.font='30px sans-serif';c.fillText('FORMA / COACH',44,60);
 c.fillStyle='#d3e7f4';c.font='32px sans-serif';c.fillText(model?.personal?.enabled?l.plan:l.headline,44,104);
 const seconds=Math.ceil(state?.remaining||0),duration=state?.intervalSeconds||1;
 const progress=!state||state.state==='Ready'?1:Math.max(0,Math.min(1,seconds/duration));
 c.lineWidth=5;c.lineCap='round';c.strokeStyle='rgba(113,231,255,.13)';c.beginPath();c.arc(320,324,148,0,Math.PI*2);c.stroke();
 c.strokeStyle='#79f4ff';c.beginPath();c.arc(320,324,148,-Math.PI/2,-Math.PI/2+Math.PI*2*progress);c.stroke();
 c.fillStyle='#ebfcff';c.textAlign='center';c.font='500 78px sans-serif';c.fillText(state?.state&&state.state!=='Ready'?`${Math.floor(seconds/60)}:${String(seconds%60).padStart(2,'0')}`:model?.personal?.recommendation?.status==='Ready'?`${Math.floor(model.personal.recommendation.durationSeconds/60)}:${String(model.personal.recommendation.durationSeconds%60).padStart(2,'0')}`:'—',320,328);
 c.fillStyle='#9cb9d5';c.font='30px sans-serif';c.fillText(state?.state==='Rest'?l.rest:state?.state==='Paused'?l.states.Paused:state?.state==='Completed'?l.states.Completed:!state||state.state==='Ready'?l.sessionDuration:l.timer,320,372);
 c.textAlign='left';c.fillStyle='#8deeff';c.font='25px sans-serif';c.fillText(state?.state==='Work'?(state.stage==='WarmUp'?l.warm:state.stage==='Cooldown'?l.cool:l.exercise):l.states[state?.state]||l.states.Ready,44,555);
 c.fillStyle='#edfaff';c.font='36px sans-serif';wrap(c,state?.exerciseName||l.plan,44,609,540,43);
 c.save();c.beginPath();c.rect(36,670,568,178);c.clip();c.fillStyle='#97b8ce';c.font='30px sans-serif';wrap(c,state?.state==='Ready'||!state?l.ready:text,44,700,540,35);c.restore();
 const terminalSummary=sessionSummary(model);

 if(terminalSummary){
  c.clearRect(24,490,592,365);c.fillStyle='rgba(7,22,46,.8)';c.fillRect(24,490,592,365);
  c.fillStyle='#fff';c.font='72px sans-serif';c.fillText(String(terminalSummary.workSeconds),320,570);
  c.font='30px sans-serif';c.fillText(l.seconds+' / '+l.timer,320,610);
  c.font='64px sans-serif';c.fillText(String(terminalSummary.sets),320,638);
  c.font='30px sans-serif';c.fillText(l.outcomes.Completed,320,680);
  c.fillStyle='#79f4ff';c.font='27px sans-serif';c.fillText(terminalSummary.completed?l.completed:l.stopped,320,766);
 }
 // The silhouette is the same recorded video rendered as a diagram, never body tracking.
 if(showXRProgress&&session&&model?.personal?.progress){
  const progress=model.personal.progress;c.clearRect(24,490,592,365);c.fillStyle='rgba(7,22,46,.85)';c.fillRect(24,490,592,365);c.textAlign='left';c.fillStyle='#b3f3ff';c.font='34px sans-serif';
  c.fillText(l.lang==='tr'?'Kayıtlarından ilerlemen':'Progress from records',44,545);c.font='30px sans-serif';
  c.fillText((l.lang==='tr'?'Bugün: ':'Today: ')+progress.daily.completedSessions,44,610);c.fillText((l.lang==='tr'?'Bu hafta: ':'This week: ')+progress.weekly.completedSessions,44,665);c.fillText((l.lang==='tr'?'Bu ay: ':'This month: ')+progress.monthly.completedSessions,44,720);
  c.fillText(`${progress.weekly.completedDays} / ${progress.weeklyTargetDays} `+(l.lang==='tr'?'antrenman günü':'training days'),44,780);c.font='23px sans-serif';c.fillText(l.lang==='tr'?'UTC takvimi · Tamamlanan seanslar':'UTC calendar · Completed sessions',44,830);
 }
 c.textAlign='center';c.fillStyle='#c4f7ff';c.font='32px sans-serif';c.fillText(state?(model?.personal?.session?`${l.interval} ${model.personal.session.index+1} / ${model.personal.session.record.blocks.length}`:`${l.exercise} ${state.set||1} / ${state.totalSets||1}`):l.plan,320,900);c.fillStyle='#bfc2b8';c.font='23px sans-serif';c.fillText(l.recorded,320,940);panel.texture.needsUpdate=true;
 const q=cue.ctx;q.clearRect(0,0,1536,360);q.fillStyle='rgba(28,32,31,.85)';q.beginPath();q.roundRect(0,0,1536,360,26);q.fill();q.textAlign='center';q.fillStyle='#79f4ff';q.font='30px sans-serif';q.fillText(model?.personal?.enabled?(state?.exerciseName||l.plan)+` · ${Math.ceil(state?.remaining||0)} s`:state?.exerciseId==='march'?'Hafif yerinde yürüyüş':'Rahat omuz çevirme',768,54);q.fillStyle='#fff';q.font='35px sans-serif';wrap(q,text,768,112,1400,44);q.fillStyle='#8deeff';q.font='26px sans-serif';q.fillText(l.next+': '+(state?.nextExercise||'—'),768,330);cue.texture.needsUpdate=true;
 const t=timeline.ctx;t.clearRect(0,0,1536,200);t.fillStyle='rgba(6,20,43,.70)';t.beginPath();t.roundRect(0,0,1536,200,24);t.fill();t.textAlign='center';
 const timelinePlan=model?.personal?.enabled?(model.plan||[]).slice(model.personal.session?.index||0,(model.personal.session?.index||0)+2):[];
 const entries=[...timelinePlan.map(e=>[e.id,model?.personal?.enabled?e.name:e.id==='march'?(l.lang==='tr'?'Yerinde yürüyüş':'Marching in place'):e.id==='shoulders'?(l.lang==='tr'?'Omuz çevirme':'Shoulder circles'):e.name,e.sets?`${e.sets} × ${e.workSeconds} s`:l.plan]),['summary',l.summary,l.history]];const tileWidth=1536/entries.length;
 entries.forEach(([id,name,dose],i)=>{const active=model?.personal?.enabled?i===0:id===state?.exerciseId||(id==='summary'&&state?.state==='Completed');t.fillStyle=active?'#79f4ff':'#b8d4ed';t.font='34px sans-serif';t.fillText((model?.personal?.enabled&&i===1?l.next+': ':'')+name,tileWidth*(i+.5),91);t.fillStyle='#afb4aa';t.font='25px sans-serif';t.fillText(dose,tileWidth*(i+.5),139);if(active){t.fillStyle='#79f4ff';t.fillRect(i*tileWidth+30,176,tileWidth-60,5);}});timeline.texture.needsUpdate=true;
 const livingPose=livingCoach(model,{offline,localPaused,visible:session?session.visibilityState==='visible':!document.hidden});
 const instruction=livingInstruction(model,livingPose,{offline,localPaused,visible:session?session.visibilityState==='visible':!document.hidden});
 if(instruction)text=instruction.text;if(motionCaption&&state?.exerciseId==='knees'&&state.state==='Work'&&!offline&&!localPaused)text=motionCaption;
 el('coach-cue').textContent=!state||state.state==='Ready'?l.ready:text;

 el('workout-title').textContent=!state||state.state==='Ready'?l.headline:living&&state.state==='Introduction'?(l.lang==='tr'?'Karşılama ve güvenlik':'Welcome and safety'):state?.state==='Completed'?l.completed:state?.state==='Aborted'?l.stopped:model?.personal?.enabled?state?.exerciseName||'Bugünkü planın':state?.exerciseId==='march'?'Yerinde yürüyüş':'Omuz çevirme';
 el('space-state').textContent=model?.gate?.space?l.spaceReady:l.spaceNeed;
 localizeWorldButtons(l);updateSetup();updateFlow();personalUI.render(model,!!session);
}
async function readState(){try{const r=await fetch('/api/state');if(!r.ok)throw new Error();model=await r.json();modelAt=performance.now();offline=false;el('connection-status').textContent=studioCopy(model).connection;paintPanel();}catch{failure();}}
function failure(){voice.cancel();offline=true;localPaused=true;el('connection-status').textContent=studioCopy(model).offline;paintPanel();}
function send(action,extra={}){
 if(action==='coachStart'&&model?.personal?.recommendation?.blocks?.some(b=>b.exercise.id==='knees')&&!human?.animation.motionAvailable){el('personal-plan-error').textContent='Diz hareketi yüklenemedi. Seans başlatılmadı / Knee motion unavailable; session not started.';return Promise.resolve(false);}
 const result=queue.then(async()=>{try{const r=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify({action,...extra})});const data=await r.json();if(!r.ok)throw new Error();model=data.state;modelAt=performance.now();offline=false;if(model.workout?.state==='Paused')localPaused=true;if((action==='resume'||action==='start'||action==='coachStart'||action==='coachFeedback')&&data.accepted){localPaused=false;lastClock=performance.now();}el('connection-status').textContent=model.notice||studioCopy(model).connection;paintPanel();return data.accepted;}catch{failure();return false;}});
 queue=result.catch(()=>false);return result;
}
function pause(){voice.cancel();localPaused=true;previewPaused=true;return send('pause');}
async function start(){if(!human){el('asset-status').textContent=studioCopy(model).assetWait;return;}previewPaused=false;if(!model?.athlete?.profile){el('athlete-open').click();return;}if(model?.setup.step!=='Complete'){el('setup-panel').hidden=false;updateSetup();return;}if(model?.workout?.state==='Paused'){await send('resume');return;}if(model?.athlete?.profile){if(!model.personal?.recommendation)await send('coachLiving');if(session){await send('coachStart');return;}personalUI.openPlan();return;}if(!await send('dailyFive')){el('asset-status').textContent=model?.notice||'Bu program mevcut tercihlerine uygun değil.';return;}await send('start');}
function button(label,x,y,action,color='#313b3a'){
 const b=surface(.56,.115,768,160);b.mesh.position.set(x,y,.012);const c=b.ctx;c.fillStyle=color;c.fillRect(0,0,768,160);c.fillStyle='#fff';c.font='500 40px sans-serif';c.textAlign='center';c.fillText(label,384,96);b.texture.needsUpdate=true;b.mesh.userData.action=action;b.mesh.userData.label=label;b.mesh.userData.surface=b;b.mesh.userData.color=color;content.add(b.mesh);buttons.push(b.mesh);b.mesh.visible=false;return b;
}
button('Alanı onayla',-1.12,.64,confirmSpace);button('Seans öner',-1.12,.47,()=>send(model?.athlete?.profile?'coachLiving':'dailyQuick'));
button('Başlat / Devam',-1.12,.30,start,'#71652f');button('Ⅱ  DURAKLAT',1.02,1.94,pause,'#6c3a32');
button('Seansı bitir',1.02,.47,()=>send('stop'));button('XR çıkış',1.02,.30,async()=>{await pause();if(session)await session.end();});
button('Koçu yerleştir',-1.12,.13,()=>{place();pause();send('confirmSpace',{enabled:false});});
button('Sesi aç',1.02,.64,()=>voice.setEnabled(!voice.enabled));
button('Aralığı atla',-1.12,.81,()=>send('coachSkip'));
button('İlerlemem',1.02,.13,()=>{showXRProgress=!showXRProgress;lastPanel='';paintPanel();});
function xrFeedback(outcome,difficulty,discomfort=false){const s=model?.personal?.session;if(!s?.awaitingFeedback)return;return send('coachFeedback',{value:{blockIndex:s.index,exerciseId:s.record.blocks[s.index].exerciseId,outcome,difficulty,repetitions:null,discomfort}});}
for(let i=1;i<=5;i++){const b=button('Tamam · '+i,-1.45,1.8-i*.13,()=>xrFeedback(xrFeedbackOutcome,i));b.mesh.userData.feedback=true;}
for(const [label,y,action] of [['Kısmen · 3',.97,()=>{xrFeedbackOutcome=xrFeedbackOutcome==='Completed'?'Partial':'Completed';worldLabelLanguage='';localizeWorldButtons(studioCopy(model));}],['Atladım',.84,()=>xrFeedback('Skipped',null)],['Rahatsızlık',.71,()=>xrFeedback('Skipped',null,true)]]){const b=button(label,-1.45,y,action);b.mesh.userData.feedback=true;}
let worldLabelLanguage='',voiceButtonEnabled=false;
function localizeWorldButtons(l){
 if(worldLabelLanguage===l.lang)return;worldLabelLanguage=l.lang;
 const fixed={'Alanı onayla':l.confirmShort,'Seans öner':l.plan,'Başlat / Devam':l.start+' / '+l.resume,'Ⅱ  DURAKLAT':'Ⅱ '+l.pause,'Seansı bitir':l.stop,'XR çıkış':l.exitXR,'Koçu yerleştir':l.reposition,'Kısmen · 3':xrFeedbackOutcome==='Completed'?(l.lang==='tr'?'Kısmen seç':'Use partial'):(l.lang==='tr'?'Tamamlandı seç':'Use completed'),'Atladım':l.outcomes.Skipped,'Rahatsızlık':l.lang==='tr'?'Rahatsızlık · atla / dur':'Discomfort · skip / stop','Sesi aç':voice.enabled?(l.lang==='tr'?'Sesi kapat':'Mute voice'):(l.lang==='tr'?'Sesi aç':'Enable voice'),'Aralığı atla':l.skip,'İlerlemem':l.lang==='tr'?'İlerlemem':'Progress'};
 for(const mesh of buttons){const source=mesh.userData.label,b=mesh.userData.surface,text=source.startsWith('Tamam ·')?(xrFeedbackOutcome==='Partial'?(l.lang==='tr'?'Kısmen · ':'Partial · '):(l.lang==='tr'?'Tamam · ':'Completed · '))+source.split('·')[1].trim():fixed[source]||source;const c=b.ctx;c.clearRect(0,0,768,160);c.fillStyle=mesh.userData.color;c.fillRect(0,0,768,160);c.fillStyle='#fff';c.font='500 40px sans-serif';c.textAlign='center';c.fillText(text,384,96);b.texture.needsUpdate=true;}
}
function choose(origin,direction){raycaster.set(origin,direction);const hit=raycaster.intersectObjects(buttons.filter(b=>b.visible),false)[0];if(hit)hit.object.userData.action();}
for(let i=0;i<2;i++){
 const controller=renderer.xr.getController(i);scene.add(controller);controllers.push(controller);
 const geometry=new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(),new THREE.Vector3(0,0,-1)]);const line=new THREE.Line(geometry,new THREE.LineBasicMaterial({color:0x74c8c7}));line.scale.z=3;controller.add(line);rays.push(line);
 controller.addEventListener('select',()=>{rotation.identity().extractRotation(controller.matrixWorld);choose(new THREE.Vector3().setFromMatrixPosition(controller.matrixWorld),new THREE.Vector3(0,0,-1).applyMatrix4(rotation));});
}
function place(){const cam=renderer.xr.isPresenting?renderer.xr.getCamera():camera;const position=new THREE.Vector3();cam.getWorldPosition(position);const forward=new THREE.Vector3();cam.getWorldDirection(forward);forward.y=0;if(forward.lengthSq()<.01)forward.set(0,0,-1);forward.normalize();content.position.copy(position).addScaledVector(forward,1.8);content.position.y=0;content.rotation.y=Math.atan2(-forward.x,-forward.z);placed=true;}
function resize(){renderer.setSize(host.clientWidth,host.clientHeight);camera.aspect=host.clientWidth/host.clientHeight;camera.fov=camera.aspect<1?53:38;camera.position.set(camera.aspect<1?.9:.8,1.9,camera.aspect<1?3.4:2.4);camera.lookAt(0,1.1,-1.2);camera.updateProjectionMatrix();}window.addEventListener('resize',resize);resize();
let drag=null;
let orbitYaw=0;
renderer.domElement.addEventListener('pointerdown',e=>{if(session||diagnostics.enabled)return;drag={x:e.clientX,y:e.clientY,moved:false};renderer.domElement.setPointerCapture(e.pointerId);});
renderer.domElement.addEventListener('pointermove',e=>{
 if(!drag||session)return;const dx=e.clientX-drag.x;if(Math.abs(dx)>2)drag.moved=true;
 if(!drag.moved)return;orbitYaw=Math.max(-.55,Math.min(.55,orbitYaw+dx*.003));drag.x=e.clientX;
 const narrow=camera.aspect<1,base=new THREE.Vector3(narrow?.9:.8,1.9,(narrow?3.4:2.4)+1.2);base.applyAxisAngle(new THREE.Vector3(0,1,0),orbitYaw);camera.position.set(base.x,base.y,base.z-1.2);camera.lookAt(0,1.1,-1.2);
});
renderer.domElement.addEventListener('pointercancel',()=>{drag=null;});
renderer.domElement.addEventListener('pointerup',e=>{if(drag&&!drag.moved&&!session){const rect=renderer.domElement.getBoundingClientRect();raycaster.setFromCamera(new THREE.Vector2((e.clientX-rect.left)/rect.width*2-1,-(e.clientY-rect.top)/rect.height*2+1),camera);const hit=raycaster.intersectObjects(buttons.filter(b=>b.visible),false)[0];hit?.object.userData.action();}drag=null;});
el('living-open').onclick=async()=>{if(!model?.athlete?.profile){el('athlete-open').click();return;}if(await send('coachLiving'))personalUI.openPlan();};
el('living-qa-advance').onclick=()=>{lastClock=performance.now();return send('advance',{seconds:Number(el('living-qa-seconds').value)});};
el('living-skip').onclick=()=>send('coachSkip');el('living-stop').onclick=()=>send('stop');
el('voice-toggle').onclick=()=>voice.setEnabled(!voice.enabled);
el('environment-select').onchange=()=>send('coachEnvironment',{value:el('environment-select').value});
el('confirm-space').onclick=confirmSpace;el('prepare').onclick=()=>model?.athlete?.profile?el('personal-plan-open').click():send('dailyQuick');el('start').onclick=start;el('pause').onclick=()=>model?.workout?.state==='Paused'?(model?.gate?.space?send('resume'):confirmSpace()):pause();el('stop').onclick=()=>send('stop');el('reposition').onclick=()=>{place();pause();send('confirmSpace',{enabled:false});};
el('leave-xr').onclick=async()=>{await pause();await session?.end();};
el('space-confirm').onchange=()=>{el('enter-xr').disabled=!capability?.supported||!el('space-confirm').checked;el('enter-vr').disabled=!vrCapability?.supported||!el('space-confirm').checked;};
async function enterImmersive(mode){
 let requested;const l=studioCopy(model),virtual=mode==='vr';
 if(!model?.athlete?.profile){el('athlete-open').click();return;}
 if(model?.setup?.step!=='Complete'){el('setup-panel').hidden=false;updateSetup();return;}
 const active=model?.workout?.isRunning||model?.workout?.state==='Paused';
 if(virtual&&active){el('xr-status').textContent=l.lang==='tr'?'VR modundan önce açık seansı bitir.':'End the active session before entering VR.';return;}
 try{
  const options={secure:isSecureContext,confirmed:el('space-confirm').checked,noEquipment:el('space-confirm').checked,sessionActive:!!active};
  // Keep the request directly in the button gesture. AR never falls back to VR.
  requested=await (virtual?requestWorldVR(navigator.xr,options):requestMR(navigator.xr,options));
  if(!virtual&&!isTransparentSession(requested)){await requested.end();throw new Error('Transparent AR composition required.');}
  session=requested;sessionMode=mode;buttons.forEach(b=>b.visible=true);cue.mesh.visible=true;
  if(!await pause())throw new Error('Could not pause safely.');if(!await send('signal',{field:'tracking',enabled:false}))throw new Error('Tracking reset failed.');if(!await send('virtualMode',{enabled:virtual,value:virtual}))throw new Error('Rendering mode rejected.');placed=false;poseTracked=false;
  preview.visible=virtual;holographicStage.userData.setImmersive(!virtual);scene.background=virtual?new THREE.Color(environment.background):null;scene.fog=virtual?new THREE.FogExp2(environment.background,environment.fogDensity):null;renderer.setClearColor(0x000000,0);
  session.addEventListener('visibilitychange',()=>{const visible=session?.visibilityState==='visible';if(!visible)pause();send('signal',{field:'focus',enabled:visible});});
  session.addEventListener('end',()=>{
   session=null;lastTerminal=null;buttons.forEach(b=>b.visible=false);cue.mesh.visible=false;showXRProgress=false;preview.visible=true;holographicStage.userData.setImmersive(false);scene.background=new THREE.Color(environment.background);scene.fog=new THREE.FogExp2(environment.background,environment.fogDensity);
   el('leave-xr').hidden=true;el('enter-xr').hidden=false;el('enter-vr').hidden=false;el('space-confirm').checked=false;el('enter-xr').disabled=true;el('enter-vr').disabled=true;
   pause();send('virtualMode',{enabled:false});send('signal',{field:'passthrough',enabled:false});el('xr-status').textContent=studioCopy(model).xrEnded;paintPanel();
  });
  await renderer.xr.setSession(session);await send('signal',{field:'passthrough',enabled:!virtual});await send('signal',{field:'focus',enabled:session.visibilityState==='visible'});
  el('leave-xr').hidden=false;el('enter-xr').hidden=true;el('enter-vr').hidden=true;paintPanel();
 }catch(error){
  if(requested)await requested.end().catch(()=>{});session=null;buttons.forEach(b=>b.visible=false);cue.mesh.visible=false;showXRProgress=false;preview.visible=true;holographicStage.userData.setImmersive(false);scene.background=new THREE.Color(environment.background);scene.fog=new THREE.FogExp2(environment.background,environment.fogDensity);el('xr-status').textContent=l.xrError;console.warn('FORMA XR request failed',error.name);
 }
}
el('enter-xr').onclick=()=>enterImmersive('ar');el('enter-vr').onclick=()=>enterImmersive('vr');
async function confirmSpace(){
 if(!canConfirmSpace({immersive:!!session,tracked:poseTracked,visible:session?.visibilityState==='visible'})){el('xr-status').textContent=studioCopy(model).notTracked;return false;}
 // Desktop observations are explicitly synthetic. A headset's tracking signals come only from XRFrame.
 if(!session){if(!model?.workout?.isRunning&&model?.workout?.state!=='Paused')await send('virtualMode',{enabled:false});await send('signal',{field:'tracking',enabled:true});await send('signal',{field:'passthrough',enabled:true});await send('signal',{field:'focus',enabled:!document.hidden});}
 return await send('confirmSpace',{enabled:true});
}
async function newSession(){
 if(await send('dailyBack')){await send('confirmSpace',{enabled:false});el('summary-panel').hidden=true;previewPaused=false;}
}
function updateFlow(){
 const view=studioView(model,offline),l=studioCopy(model);el('primary-action').textContent=view.action==='setup'?l.setupFirst:view.action==='confirm'?l.confirm:view.action==='pause'?l.pause:view.action==='start'?(model?.workout?.state==='Paused'?l.resume:l.start):view.action==='new'?l.newSession:view.action==='connect'?l.connect:l.loading;
 if(model&&!offline&&!model.athlete?.profile&&!model.workout?.isRunning&&model.workout?.state!=='Paused')el('primary-action').textContent=l.create;el('primary-action').disabled=!view.action;
 if(model?.personal?.savePending)el('primary-action').textContent=personalUI.copy().retry;else if(model?.personal?.session?.awaitingFeedback)el('primary-action').textContent=personalUI.copy().giveFeedback;
 el('pause').disabled=offline||!model?.workout?.isRunning&&model?.workout?.state!=='Paused';el('pause-label').textContent=model?.workout?.state==='Paused'?l.resume:l.pause;el('pause').setAttribute('aria-label',model?.workout?.state==='Paused'?l.resume:l.pause);
 el('preview-play').disabled=!!model?.workout?.state&&model.workout.state!=='Ready';
 el('stop').disabled=!model?.workout?.isRunning&&model?.workout?.state!=='Paused';
 const summary=sessionSummary(model);
 if(summary){
  el('summary-title').textContent=summary.partial?(l.lang==='tr'?'Seans kısmen yapıldı':'Session partially completed'):summary.completed?l.completed:l.stopped;
  el('summary-copy').textContent=l.summaryCopy(summary.workSeconds,summary.sets,summary.completedSessions);
  el('personal-completion-advice').textContent=model?.personal?.nextRecommendation?.reasons?.join(' ')||'';
  el('summary-plan').textContent=l.summaryPlan+(model?.personal?.enabled?(model.plan||[]).map(x=>x.name).join(' / '):summary.plan.join(' / '));
  if(lastTerminal!==model.workout.state){el('summary-panel').hidden=!!session;lastTerminal=model.workout.state;}
 }else{lastTerminal=null;el('summary-panel').hidden=true;}
 const list=el('history-list');list.replaceChildren();
 const rows=[...(model?.personal?.history||[]).map(r=>({...r,outcome:r.outcome==='Cancelled'?'Aborted':r.outcome,activeSeconds:r.timedActiveSeconds,completedSets:r.feedback.filter(f=>f.outcome==='Completed').length})),...(model?.history||[]).filter(r=>model?.fixtureData||!r.isTestData)].sort((a,b)=>new Date(b.startedUtc)-new Date(a.startedUtc));
 if(!rows.length||model.historyAvailable===false){const p=document.createElement('p');p.textContent=model.historyAvailable===false?l.historyUnavailable:l.emptyHistory;list.append(p);}
 for(const row of rows.slice(0,8)){
  const item=document.createElement('article'),title=document.createElement('strong'),detail=document.createElement('p');
  title.textContent=l.outcomes[row.outcome]||l.outcomes.Interrupted;
  const date=new Date(row.startedUtc);detail.textContent=`${Number.isNaN(date.getTime())?'Tarih yok':date.toLocaleDateString(l.lang)} · ${Math.floor(row.activeSeconds||0)} s · ${row.completedSets||0}`;
  item.append(title,detail);list.append(item);
 }
}
el('primary-action').onclick=async()=>{
 if(model?.personal?.savePending){await send('coachRetrySave');return;}if(model?.personal?.session?.awaitingFeedback){personalUI.openFeedback();return;}
 if(model&&!offline&&!model.athlete?.profile&&!model.workout?.isRunning&&model.workout?.state!=='Paused'){el('athlete-open').click();return;}
 const action=studioView(model,offline).action;
 if(action==='connect'){await readState();if(!offline)await pause();}
 else if(action==='setup'){el('setup-panel').hidden=false;updateSetup();}
 else if(action==='confirm')await confirmSpace();
 else if(action==='pause')await pause();
 else if(action==='start')await start();
 else if(action==='new')await newSession();
};
el('summary-new').onclick=newSession;
el('restart').onclick=async()=>{await pause();if(model?.workout?.state==='Paused')await send('stop');await newSession();};
el('summary-close').onclick=()=>{el('summary-panel').hidden=true;};
el('history-open').onclick=()=>{updateFlow();el('history-panel').hidden=false;};
el('history-close').onclick=()=>{el('history-panel').hidden=true;};
function updateSetup(){
 if(!model)return;const setup=model.setup,l=studioCopy(model);el('setup-step').textContent=l.setupTitles[setup.step];
 el('setup-copy').textContent=setup.step==='Safety'?l.setupSafety:l.setupBodies[setup.step]||'';
 el('safety-checks').hidden=setup.step!=='Safety';el('setup-next').textContent=setup.step==='Review'?l.setupSave:setup.step==='Complete'?l.setupReturn:l.setupNext;
}
el('setup-profile-link').onclick=event=>{event.preventDefault();el('setup-panel').hidden=true;el('athlete-open').click();};
el('controls-open').onclick=()=>{el('control-drawer').hidden=!el('control-drawer').hidden;};
el('preview-play').hidden=true;
el('setup-open').onclick=()=>{el('setup-panel').hidden=false;updateSetup();};
el('setup-close').onclick=()=>{el('setup-panel').hidden=true;};
el('setup-next').onclick=async()=>{
 if(model?.setup?.step==='Complete'){el('setup-panel').hidden=true;return;}
 if(model?.setup?.step==='Welcome')await send('edit',{field:'language',value:studioCopy(model).lang});
 if(model?.setup?.step==='Safety'){
  if(!el('setup-ack').checked||!el('setup-space').checked){el('setup-copy').textContent=studioCopy(model).setupError;return;}
  await send('safety',{field:'ack',enabled:true});await send('safety',{field:'space',enabled:true});
 }
 await send(model?.setup?.step==='Review'?'finish':'next');if(model?.setup?.step==='Complete')el('setup-panel').hidden=true;
};
window.addEventListener('keydown',e=>{if(e.code==='Space'&&!document.activeElement?.closest('input,textarea,select,button,[contenteditable=true]')){e.preventDefault();pause();}});
document.addEventListener('visibilitychange',()=>{if(!session){if(document.hidden)pause();send('signal',{field:'focus',enabled:!document.hidden});}});
scene.background=new THREE.Color(environment.background);
renderer.setAnimationLoop((now,frame)=>{
 const rawDt=Math.max(0,(now-lastFrame)/1000),dt=Math.min(rawDt,.1);lastFrame=now;renderer.info.reset();
 if(session&&frame){const reference=renderer.xr.getReferenceSpace();const tracked=!!reference&&!!frame.getViewerPose(reference);if(tracked!==poseTracked){poseTracked=tracked;if(!tracked)pause();send('signal',{field:'tracking',enabled:tracked});}if(tracked&&!placed)place();for(const source of session.inputSources){const pressed=!!source.gamepad?.buttons[5]?.pressed;if(pressed&&!pauseButtons.get(source))pause();pauseButtons.set(source,pressed);}}
 const feedbackSession=model?.personal?.session;const feedbackIdentity=feedbackSession?.awaitingFeedback?feedbackSession.id+':'+feedbackSession.index:'';if(feedbackIdentity!==xrFeedbackKey){xrFeedbackKey=feedbackIdentity;xrFeedbackOutcome='Completed';worldLabelLanguage='';localizeWorldButtons(studioCopy(model));}
 const awaiting=!!feedbackSession?.awaitingFeedback;for(const b of buttons)b.visible=!!session && (b.userData.feedback?awaiting:!awaiting||b.position.x>=0);
 const motionOptions={offline,localPaused,visible:session?session.visibilityState==='visible':!document.hidden,reduced:!!model?.setup?.profile?.reducedMotion||matchMedia('(prefers-reduced-motion: reduce)').matches};
 const qaHold=!!model?.fixtureData&&el('living-qa-hold').checked;
 let livePose=livingCoach(model,motionOptions);if(livePose)host.dataset.intervalElapsed=String(livePose.seconds);if(livePose&&!qaHold&&model?.workout?.isRunning&&!motionOptions.localPaused&&!motionOptions.offline&&motionOptions.visible)livePose.seconds+=Math.min(.3,Math.max(0,(now-modelAt)/1000));
 const inspection=diagnostics.update(dt,{developer:!!model?.fixtureData,immersive:!!session});if(inspection)livePose=inspection;
 const pose=coachPresentation(model,{offline,localPaused,visible:session?session.visibilityState==='visible':!document.hidden,reduced:!!model?.setup?.profile?.reducedMotion||matchMedia('(prefers-reduced-motion: reduce)').matches});
 human?.update(pose.phase,pose.moving,pose.exercise,pose.reduced,dt,pose.state,livePose);host.dataset.coachState=livePose?.state||pose.state;host.dataset.animation=human?.animation.state||'loading';
 let instruction=inspection?{text:'',language:studioCopy(model).lang,key:'inspection',interrupted:false}:livingInstruction(model,livePose,motionOptions),motionView=null;
 const knee=livePose?.exercise==='knees'&&!livePose.reduced&&human?.animation.motion;
 if(knee){
  if(orchestratorDefinition!==knee.definition){orchestrator=new CoachingOrchestrator(knee.definition);orchestratorDefinition=knee.definition;}
  const state=livePose.inspection?(livePose.complete?'COMPLETE':livePose.playing?'ACTIVE':'PAUSED'):({idle:'IDLE',welcome:'INTRO',demonstrating:'DEMONSTRATE',exercising:'ACTIVE',resting:'REST',paused:'PAUSED',cooldown:'REST',completed:'COMPLETE'}[livePose.state]||'IDLE');
  motionView=orchestrator.update({scope:inspection?diagnostics.scope:model.personal.session.id+':'+model.personal.session.index,seconds:livePose.seconds,state,language:studioCopy(model).lang,seek:!!inspection?.seek});
  if(state==='ACTIVE'||state==='DEMONSTRATE'){
   instruction={...instruction,text:motionView.caption};if(motionView.cancelVoice)voice.cancel();if(motionView.cue)voice.update(motionView.cue);motionVoicePaused=false;if(!inspection&&motionCaption!==instruction.text){motionCaption=instruction.text;lastPanel='';paintPanel();}
  }else if(inspection){instruction.text=motionView.caption;if(!motionVoicePaused)voice.cancel();motionVoicePaused=true;}else if(instruction)voice.update(instruction);
  diagnostics.report({...motionView,eventHistory:orchestrator.events},human.animation);
 }else{motionCaption=null;if(orchestrator){orchestrator.reset();orchestrator=null;orchestratorDefinition=null;}if(instruction)voice.update(instruction);else if(wasLivingVoice)voice.cancel();}
 if(instruction)el('coach-cue').textContent=instruction.text;wasLivingVoice=!!instruction;
 capabilities=trackingCapabilities(frame,session,renderer.xr.getReferenceSpace());
 const tr=studioCopy(model).lang==='tr';const stateLabels=tr?{welcome:'Karşılama ve güvenlik',demonstrating:'Önce gösterimi izle',exercising:'Kendi hızında birlikte hareket et',resting:'Dinlen ve nasıl geçtiğini bildir',paused:'Duraklatıldı',cooldown:'Soğuma',completed:'Seans sona erdi'}:{welcome:'Welcome and safety',demonstrating:'Watch the demonstration first',exercising:'Move together at your own pace',resting:'Rest and report how it went',paused:'Paused',cooldown:'Cooldown',completed:'Session ended'};el('living-state').textContent=stateLabels[livePose?.state]||'';el('tracking-capabilities').textContent=tr?`Zamanlı + senin onayın · Baş: ${capabilities.headTracking?'izleniyor':'yok'} · Eller: ${capabilities.leftHandTracking?'sol ':''}${capabilities.rightHandTracking?'sağ':''}${!capabilities.leftHandTracking&&!capabilities.rightHandTracking?'yok':''} · Tam vücut / nabız: yok`:`Timed + user-confirmed · Head: ${capabilities.headTracking?'tracked':'unavailable'} · Hands: ${capabilities.leftHandTracking?'L ':''}${capabilities.rightHandTracking?'R':''}${!capabilities.leftHandTracking&&!capabilities.rightHandTracking?'unavailable':''} · Full body / heart rate: unavailable`;
 const voiceMessages=tr?{muted:'Ses kapalı · Altyazılar açık',unsupported:'Bu tarayıcıda konuşma yok · Altyazıları izle','no-local-voice':'Bu dilde yerel ses yok · Altyazıları izle',blocked:'Ses başlatılamadı · Sesi aç düğmesini yeniden dene',ready:'Yerel ses hazır',speaking:'Koç konuşuyor',requested:'Ses bekleniyor',interrupted:'Ses durduruldu'}:{muted:'Voice muted · Captions on',unsupported:'Speech unavailable · Follow captions','no-local-voice':'No local voice for this language · Follow captions',blocked:'Speech blocked · Try Enable voice again',ready:'Local voice ready',speaking:'Coach speaking',requested:'Speech requested',interrupted:'Speech stopped'};el('voice-status').textContent=voiceMessages[voiceStatus]||'';el('voice-toggle').textContent=voice.enabled?(tr?'Sesi kapat':'Mute voice'):(tr?'Sesi aç':'Enable voice');el('voice-toggle').setAttribute('aria-pressed',String(voice.enabled));el('living-open').disabled=!human?.animation.motionAvailable;el('living-open').textContent=tr?'FORMA START · 5 dk':'FORMA START · 5 min';
 holographicStage.userData.setLiving?.(!!livePose);timeline.mesh.visible=!livePose;panel.mesh.visible=!livePose;cue.mesh.visible=!!session;
 if(voiceButtonEnabled!==voice.enabled){voiceButtonEnabled=voice.enabled;worldLabelLanguage='';localizeWorldButtons(studioCopy(model));}

 if(now-lastClock>=250){const elapsed=(now-lastClock)/1000;lastClock=now;if(!diagnostics.enabled&&!qaHold&&model?.workout?.isRunning&&!localPaused&&!offline&&(session?session.visibilityState==='visible':!document.hidden)){if(elapsed>1.5)pause();else send('tick',{seconds:elapsed});}}
 if(session)renderer.render(scene,camera);else composer.render(scene,camera);diagnostics.sample(rawDt*1000,renderer.info.render.calls,renderer.info.render.triangles);
});
await readState();if(model?.workout?.isRunning)await pause();await send('signal',{field:'focus',enabled:!document.hidden});capability=await inspectAR(navigator.xr,isSecureContext);vrCapability=await inspectVR(navigator.xr,isSecureContext);el('xr-status').textContent=capability.supported?studioCopy(model).xrAvailable:studioCopy(model).xrUnavailable;el('enter-xr').disabled=!capability.supported||!el('space-confirm').checked;paintPanel();

window.addEventListener('pagehide',event=>{
 voice.cancel();if(event.persisted)return;
 diagnostics.dispose();human?.animation.dispose();composer.dispose();
 const geometries=new Set(),materials=new Set(),textures=new Set(),skeletons=new Set();
 scene.traverse(o=>{if(o.geometry)geometries.add(o.geometry);if(o.skeleton)skeletons.add(o.skeleton);for(const material of o.material?(Array.isArray(o.material)?o.material:[o.material]):[]){materials.add(material);for(const value of Object.values(material))if(value?.isTexture)textures.add(value);}});
 for(const item of geometries)item.dispose();for(const item of materials)item.dispose();for(const item of textures)item.dispose();for(const item of skeletons)item.dispose();renderer.dispose();
});
