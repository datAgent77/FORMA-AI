import test from 'node:test';
import assert from 'node:assert/strict';
import {shouldPlayCoach} from '../../tools/WebSimulation/wwwroot/coach-media-policy.mjs';
import {inspectAR,requestMR,isTransparentSession} from '../../tools/WebSimulation/wwwroot/xr-session.mjs';
const base=process.env.FORMA_SIM_URL||'http://127.0.0.1:5078';
test('WebXR assets and pinned local renderer are available',async()=>{for(const path of ['/xr.html','/xr.mjs','/xr.css','/xr-session.mjs','/vendor/three/three.module.js','/vendor/three/three.core.js','/vendor/three/LICENSE.txt'])assert.equal((await fetch(base+path)).status,200,path);});
test('unsupported browser is reported without fabricating a session',async()=>{assert.equal((await inspectAR(undefined,true)).supported,false);assert.equal((await inspectAR({isSessionSupported:async()=>false},true)).supported,false);});
test('insecure contexts are rejected before capability access',async()=>{let called=false;assert.equal((await inspectAR({isSessionSupported:async()=>{called=true;}},false)).supported,false);assert.equal(called,false);});
test('requestMR uses actual immersive-ar and local-floor with no VR fallback',async()=>{let args;const actual={environmentBlendMode:'alpha-blend'};const xr={requestSession:(...a)=>{args=a;return Promise.resolve(actual);}};assert.equal(await requestMR(xr,{secure:true,confirmed:true,noEquipment:true}),actual);assert.deepEqual(args,['immersive-ar',{requiredFeatures:['local-floor']}]);});
test('missing clear-space or equipment-free confirmation never requests XR',async()=>{let called=false;const xr={requestSession:()=>{called=true;}};await assert.rejects(requestMR(xr,{secure:true,confirmed:false,noEquipment:true}));await assert.rejects(requestMR(xr,{secure:true,confirmed:true,noEquipment:false}));assert.equal(called,false);});
test('opaque sessions cannot be mistaken for passthrough',()=>{assert.equal(isTransparentSession({environmentBlendMode:'opaque'}),false);assert.equal(isTransparentSession({environmentBlendMode:'alpha-blend'}),true);assert.equal(isTransparentSession({environmentBlendMode:'additive'}),true);assert.equal(isTransparentSession(undefined),false);});
test('reference studio serves a real locally hosted rigged GLB with license and model loaders',async()=>{
 const response=await fetch(base+'/assets/coach/woman-casual.glb');assert.equal(response.status,200);assert.match(response.headers.get('content-type'),/model\/gltf-binary/);
 const bytes=Buffer.from(await response.arrayBuffer());assert.equal(bytes.toString('ascii',0,4),'glTF');
 const gltf=JSON.parse(bytes.toString('utf8',20,20+bytes.readUInt32LE(12)));assert.ok(gltf.skins.length>0);assert.ok(gltf.nodes.length>20);assert.ok(!gltf.images?.some(i=>i.uri?.startsWith('http')));
 for(const file of ['/studio-scene.mjs','/vendor/three/GLTFLoader.js','/vendor/three/SkeletonUtils.js','/vendor/three/BufferGeometryUtils.js','/assets/coach/PROVENANCE.json'])assert.equal((await fetch(base+file)).status,200,file);
});
test('reference timer consumes shared interval duration, including pause and rest',async()=>{
 const initial=await fetch(base+'/api/state'),cookie=initial.headers.get('set-cookie').split(';')[0];
 const act=async(action,extra={})=>{const r=await fetch(base+'/api/action',{method:'POST',headers:{cookie,'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify({action,...extra})});return (await r.json()).state;};
 await act('next');await act('next');await act('next');await act('safety',{field:'ack',enabled:true});await act('safety',{field:'space',enabled:true});await act('next');await act('finish');await act('confirmSpace',{enabled:true});
 let state=await act('start');assert.equal(state.workout.intervalSeconds,10);assert.equal(state.plan[0].workSeconds,20);
 state=await act('advance',{seconds:12});assert.equal(state.workout.intervalSeconds,20);assert.equal(state.workout.remaining,18);
 state=await act('pause');assert.equal(state.workout.intervalSeconds,20);assert.equal(state.workout.remaining,18);
 await act('confirmSpace',{enabled:true});await act('resume');state=await act('advance',{seconds:18});assert.equal(state.workout.state,'Rest');assert.equal(state.workout.intervalSeconds,15);
});

test('recorded coach stops for pause, rest, offline, visibility loss and reduced motion',()=>{
 const active={state:'Work',exerciseId:'march',localPaused:false};
 assert.equal(shouldPlayCoach(active),true);
 for(const patch of [{state:'Paused'},{state:'Rest'},{state:'Completed'},{state:'Aborted'},{offline:true},{visible:false},{reducedMotion:true},{localPaused:true},{previewPaused:true},{exerciseId:'shoulders'}])assert.equal(shouldPlayCoach({...active,...patch}),false,JSON.stringify(patch));
 assert.equal(shouldPlayCoach({state:'Ready'}),true);
 assert.equal(shouldPlayCoach({state:'Ready',previewPaused:true}),false);
});
test('photographic demo serves local packed video, alpha metadata and generated assets',async()=>{
 for(const path of ['/video-coach.mjs','/coach-media-policy.mjs','/assets/studio/room-photo.png','/assets/studio/dumbbell-glove.png','/assets/studio/coach-video.json','/assets/studio/PROVENANCE.json'])assert.equal((await fetch(base+path)).status,200,path);
 const video=await fetch(base+'/assets/studio/coach-packed.mp4',{headers:{Range:'bytes=0-31'}});assert.equal(video.status,206);assert.match(video.headers.get('content-type'),/video\/mp4/);assert.equal((await video.arrayBuffer()).byteLength,32);
 const meta=await (await fetch(base+'/assets/studio/coach-video.json')).json();assert.ok(meta.width>0&&meta.height>0);assert.equal(meta.alphaLayout,'rgb-left-alpha-right');
});

test('VR Worlds entry is explicit, equipment-free and never an automatic MR fallback',async()=>{
 const {requestWorldVR,inspectVR}=await import('../../tools/WebSimulation/wwwroot/xr-session.mjs');let calls=[];
 const xr={isSessionSupported:async mode=>mode==='immersive-vr',requestSession:async(mode,options)=>{calls.push({mode,options});return {mode};}};
 assert.equal((await inspectVR(xr,true)).supported,true);
 for(const options of [{secure:false,confirmed:true,noEquipment:true},{secure:true,confirmed:false,noEquipment:true},{secure:true,confirmed:true,noEquipment:false},{secure:true,confirmed:true,noEquipment:true,sessionActive:true}])await assert.rejects(requestWorldVR(xr,options));
 assert.equal(calls.length,0);await requestWorldVR(xr,{secure:true,confirmed:true,noEquipment:true});assert.equal(calls[0].mode,'immersive-vr');assert.deepEqual(calls[0].options.requiredFeatures,['local-floor']);
});
