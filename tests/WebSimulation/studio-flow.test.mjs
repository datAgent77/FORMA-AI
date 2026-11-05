import test from 'node:test';
import assert from 'node:assert/strict';
import {studioView,sessionSummary,canConfirmSpace} from '../../tools/WebSimulation/wwwroot/studio-flow.mjs';
const ready={setup:{step:'Complete'},gate:{space:false},workout:{state:'Ready',isRunning:false}};
test('primary action requires setup and separate space consent before start',()=>{
 assert.equal(studioView({setup:{step:'Safety'}}).action,'setup');
 assert.equal(studioView(ready).action,'confirm');
 assert.equal(studioView({...ready,gate:{space:true}}).action,'start');
});
test('paused workout never resumes from restored signals or a single space click',()=>{
 const paused={...ready,workout:{state:'Paused',isRunning:false}};
 assert.equal(studioView(paused).action,'confirm');
 assert.equal(studioView({...paused,gate:{space:true}}).label,'Devam et');
 assert.equal(studioView({...ready,workout:{state:'Rest',isRunning:true}}).action,'pause');
 assert.equal(studioView(ready,true).action,'connect');
 assert.notEqual(studioView(ready,true).action,'start');
});
test('cancelled session summary cannot masquerade as completion or a streak',()=>{
 const model={...ready,workout:{state:'Aborted'},plan:[{id:'march'}],daily:{summary:{outcome:'Aborted',activeSeconds:4.8,sets:0,completedSessions:2}}};
 assert.equal(studioView(model).action,'new');const s=sessionSummary(model);
 assert.equal(s.completed,false);assert.equal(s.workSeconds,4);assert.equal(s.completedSessions,2);assert.equal(s.sets,0);
 assert.equal(sessionSummary(ready),null);
});
test('headset confirmation requires current tracking and visible session',()=>{
 assert.equal(canConfirmSpace({immersive:true,tracked:false,visible:true}),false);
 assert.equal(canConfirmSpace({immersive:true,tracked:true,visible:false}),false);
 assert.equal(canConfirmSpace({immersive:true,tracked:true,visible:true}),true);
 assert.equal(canConfirmSpace({immersive:false}),true);
});
test('workout completion is recorded once, restart preserves history, abort does not add completion',async()=>{
 const base=process.env.FORMA_SIM_URL||'http://127.0.0.1:5078';
 const first=await fetch(base+'/api/state'),cookie=first.headers.get('set-cookie').split(';')[0];
 const act=async(action,extra={})=>{const r=await fetch(base+'/api/action',{method:'POST',headers:{cookie,'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify({action,...extra})});assert.equal(r.status,200);return r.json();};
 await act('next');await act('next');await act('next');await act('safety',{field:'ack',enabled:true});await act('safety',{field:'space',enabled:true});await act('next');await act('finish');
 await act('dailyQuick');await act('confirmSpace',{enabled:true});assert.equal((await act('start')).accepted,true);
 let result=await act('advance',{seconds:600});assert.equal(result.state.workout.state,'Completed');assert.equal(result.state.history.filter(r=>r.outcome==='Completed').length,1);
 result=await act('advance',{seconds:600});assert.equal(result.accepted,false);assert.equal(result.state.history.filter(r=>r.outcome==='Completed').length,1);
 await act('dailyBack');await act('dailyQuick');await act('confirmSpace',{enabled:true});assert.equal((await act('start')).accepted,true);
 await act('advance',{seconds:11});result=await act('stop');assert.equal(result.state.workout.state,'Aborted');assert.equal(result.state.history.filter(r=>r.outcome==='Completed').length,1);assert.equal(result.state.history.filter(r=>r.outcome==='Aborted').length,1);
 if(process.env.FORMA_SIM_DATA){const {readFile}=await import('node:fs/promises');const {join}=await import('node:path');const id=cookie.split('=')[1];assert.match(id,/^[a-f0-9]{32}$/);const saved=JSON.parse(await readFile(join(process.env.FORMA_SIM_DATA,id,'simulation-history.json'),'utf8'));assert.equal(saved.length,2);assert.equal(saved.filter(r=>r.outcome==='Completed').length,1);}
});
