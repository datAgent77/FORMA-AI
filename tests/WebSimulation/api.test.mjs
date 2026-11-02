import test from 'node:test';
import assert from 'node:assert/strict';
const base=process.env.FORMA_SIM_URL||'http://127.0.0.1:5078';
async function client(){
 const first=await fetch(base+'/api/state');assert.equal(first.status,200);
 const cookie=first.headers.get('set-cookie')?.split(';')[0];assert.ok(cookie);
 return {
  state:()=>fetch(base+'/api/state',{headers:{cookie}}).then(r=>r.json()),
  async act(action,extra={}){
   const response=await fetch(base+'/api/action',{method:'POST',headers:{cookie,'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify({action,...extra})});
   return {status:response.status,...await response.json()};
  },cookie
 };
}
async function setup(c,edits={}){
 for(const [field,value] of Object.entries(edits))assert.equal((await c.act('edit',{field,value})).accepted,true);
 assert.equal((await c.act('next')).state.setup.step,'Coach');
 assert.equal((await c.act('next')).state.setup.step,'Profile');
 assert.equal((await c.act('next')).state.setup.step,'Safety');
 await c.act('safety',{field:'ack',enabled:true});await c.act('safety',{field:'space',enabled:true});
 assert.equal((await c.act('next')).state.setup.step,'Review');
 return c.act('finish');
}
async function running(c){await setup(c);await c.act('confirmSpace',{enabled:true});assert.equal((await c.act('start')).accepted,true);}

test('browser assets serve locally with no remote dependencies',async()=>{
 for(const file of ['/','/app.js','/style.css','/mark.svg','/health'])assert.equal((await fetch(base+file)).status,200,file);
 const html=await (await fetch(base+'/')).text();assert.ok(html.includes('Web simulation'));assert.ok(!html.includes('https://'));
 const health=await (await fetch(base+'/health')).json();assert.equal(health.engine,'shared-csharp');assert.equal(health.hardwareValidated,false);
});
test('simulation begins with shared welcome and unconfirmed space',async()=>{
 const c=await client(),s=await c.state();assert.equal(s.setup.step,'Welcome');assert.equal(s.gate.space,false);assert.equal(s.coaches.length,3);assert.equal(s.simulationOnly,true);
 assert.equal((await c.act('start')).accepted,false);
});
test('setup rejects missing safety checks and preserves completion gate',async()=>{
 const c=await client();await c.act('next');await c.act('next');await c.act('skip');
 assert.equal((await c.act('next')).accepted,false);await c.act('safety',{field:'ack',enabled:true});assert.equal((await c.act('next')).accepted,false);
 await c.act('safety',{field:'space',enabled:true});await c.act('next');const result=await c.act('finish');assert.equal(result.accepted,true);assert.equal(result.state.gate.space,false);
});
test('optional profile skip clears volunteered fields without erasing posture',async()=>{
 const c=await client();await c.act('edit',{field:'posture',value:'Seated'});await c.act('next');await c.act('next');
 await c.act('edit',{field:'goal',value:'BuildRoutine'});await c.act('edit',{field:'movementLimits',value:1});
 const r=await c.act('skip');assert.equal(r.state.setup.profile.goal,'Skip');assert.equal(r.state.setup.profile.movementLimits,'Undisclosed');assert.equal(r.state.setup.profile.posture,'Seated');
});
test('write failure keeps review open; retry persists actual C# profile repository',async()=>{
 const c=await client();await c.act('writeFault',{enabled:true});const failed=await setup(c,{coachId:'mira',language:'tr'});
 assert.equal(failed.accepted,false);assert.equal(failed.state.setup.step,'Review');assert.equal(failed.state.setup.persistenceBlocked,true);
 await c.act('writeFault',{enabled:false});assert.equal((await c.act('finish')).accepted,true);
 const reload=await c.act('reload');assert.equal(reload.state.setup.step,'Complete');assert.equal(reload.state.setup.profile.coachId,'mira');assert.equal(reload.state.setup.profile.language,'tr');assert.equal(reload.state.gate.space,false);
});
test('onboarding focus interruption invalidates both physical confirmations',async()=>{
 const c=await client();await c.act('writeFault',{enabled:true});await setup(c);const loss=await c.act('signal',{field:'focus',enabled:false});
 assert.equal(loss.state.setup.step,'Safety');assert.equal(loss.state.setup.safetyAcknowledged,false);assert.equal(loss.state.setup.spaceConfirmed,false);
 await c.act('signal',{field:'focus',enabled:true});assert.equal((await c.act('finish')).accepted,false);
});
test('seated profile uses seated shoulder guidance and excludes standing march',async()=>{
 const c=await client();await setup(c,{posture:'Seated'});await c.act('confirmSpace',{enabled:true});const start=await c.act('start');
 assert.equal(start.state.workout.exerciseId,'shoulders');assert.match(start.state.workout.instructions,/Sit on a stable chair/);assert.equal(start.state.compatible.length,1);
});
test('no compatible starter movement fails closed',async()=>{
 const c=await client();await setup(c,{posture:'Seated',movementLimits:2});await c.act('confirmSpace',{enabled:true});
 const start=await c.act('start');assert.equal(start.accepted,false);assert.equal(start.state.workout,null);assert.equal(start.state.history.length,0);
});
test('pause freezes test clock; resume requires fresh explicit space confirmation',async()=>{
 const c=await client();await running(c);await c.act('advance',{seconds:15});const paused=await c.act('pause');
 assert.equal(paused.state.workout.state,'Paused');assert.equal(paused.state.workout.activeSeconds,5);assert.equal(paused.state.gate.space,false);
 const frozen=await c.act('advance',{seconds:100});assert.equal(frozen.accepted,false);assert.equal(frozen.state.workout.remaining,15);
 assert.equal((await c.act('resume')).accepted,false);await c.act('confirmSpace',{enabled:true});assert.equal((await c.act('resume')).accepted,true);
});
for(const field of ['focus','tracking','passthrough'])test(`${field} loss pauses and recovery never auto-resumes`,async()=>{
 const c=await client();await running(c);await c.act('advance',{seconds:12});const loss=await c.act('signal',{field,enabled:false});
 assert.equal(loss.state.workout.state,'Paused');assert.equal(loss.state.gate.ready,false);assert.equal(loss.state.gate.space,false);
 const restore=await c.act('signal',{field,enabled:true});assert.equal(restore.state.workout.state,'Paused');assert.equal((await c.act('resume')).accepted,false);
 await c.act('confirmSpace',{enabled:true});assert.equal((await c.act('resume')).accepted,true);
});
test('shared workout completes exactly and recorded totals survive setup editing',async()=>{
 const c=await client();await running(c);const done=await c.act('advance',{seconds:135});
 assert.equal(done.state.workout.state,'Completed');assert.equal(done.state.workout.activeSeconds,80);assert.equal(done.state.workout.completedSets,4);assert.equal(done.state.history[0].outcome,'Completed');
 const id=done.state.history[0].sessionId;await c.act('reopen');await setup(c,{coachId:'atlas'});const reloaded=await c.act('reload');
 assert.equal(reloaded.state.history[0].sessionId,id);assert.equal(reloaded.state.history[0].activeSeconds,80);assert.equal(reloaded.state.history[0].completedSets,4);
});
test('exit pauses first and confirms second; returning cannot start automatically',async()=>{
 const c=await client();await running(c);const first=await c.act('exit');assert.equal(first.state.workout.state,'Paused');assert.equal(first.state.closed,false);
 const second=await c.act('exit',{enabled:true});assert.equal(second.state.closed,true);assert.equal(second.state.workout.state,'Aborted');
 const back=await c.act('return');assert.equal(back.state.gate.space,false);assert.equal((await c.act('start')).accepted,false);
});
test('reset requires confirmation and cannot delete an active session',async()=>{
 const c=await client();assert.equal((await c.act('reset')).accepted,false);await running(c);assert.equal((await c.act('reset',{enabled:true})).accepted,false);
 await c.act('stop');const reset=await c.act('reset',{enabled:true});assert.equal(reset.state.setup.step,'Welcome');assert.equal(reset.state.history.length,0);
});
test('browser sessions remain isolated',async()=>{
 const a=await client(),b=await client();await a.act('edit',{field:'coachId',value:'atlas'});assert.equal((await a.state()).setup.profile.coachId,'atlas');assert.equal((await b.state()).setup.profile.coachId,'sol');
});
test('invalid field and invalid preference cannot mutate valid profile',async()=>{
 const c=await client();assert.equal((await c.act('edit',{field:'secret',value:'x'})).status,400);
 assert.equal((await c.act('edit',{field:'preferredMinutes',value:999})).accepted,false);assert.equal((await c.state()).setup.profile.preferredMinutes,0);
});
test('test clock rejects invalid or excessive elapsed time',async()=>{
 const c=await client();await running(c);assert.equal((await c.act('advance',{seconds:-1})).status,400);assert.equal((await c.act('advance',{seconds:601})).status,400);assert.equal((await c.state()).workout.remaining,10);
});
test('cross-origin and simple write requests are rejected',async()=>{
 for(const headers of [{'Content-Type':'application/json'},{'Content-Type':'application/json','X-Forma-Simulation':'1',Origin:'https://example.com'}])
  assert.equal((await fetch(base+'/api/action',{method:'POST',headers,body:JSON.stringify({action:'reset',enabled:true})})).status,403);
});
test('export is labeled synthetic and includes only the current browser session',async()=>{
 const c=await client();await running(c);await c.act('advance',{seconds:135});
 const response=await fetch(base+'/api/export',{headers:{cookie:c.cookie}}),data=await response.json();
 assert.equal(data.simulationOnly,true);assert.equal(data.sessions.length,1);assert.equal(data.sessions[0].activeSeconds,80);assert.match(response.headers.get('content-disposition'),/forma-simulation-export/);
});

test('exercise selection uses the shared plan and never allows an empty workout',async()=>{
 const c=await client();await setup(c);assert.equal((await c.act('selectExercise',{field:'march',enabled:false})).accepted,true);
 assert.equal((await c.act('selectExercise',{field:'shoulders',enabled:false})).accepted,false);
 await c.act('confirmSpace',{enabled:true});const start=await c.act('start');assert.equal(start.state.workout.exerciseId,'shoulders');
 const done=await c.act('advance',{seconds:65});assert.equal(done.state.workout.state,'Completed');assert.equal(done.state.workout.activeSeconds,40);
});
test('completion clears physical-space confirmation before the next session',async()=>{
 const c=await client();await running(c);const done=await c.act('advance',{seconds:135});assert.equal(done.state.gate.space,false);assert.equal((await c.act('start')).accepted,false);
 await c.act('confirmSpace',{enabled:true});assert.equal((await c.act('start')).accepted,true);
});
