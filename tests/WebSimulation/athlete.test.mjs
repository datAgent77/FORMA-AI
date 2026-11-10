import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile,writeFile} from 'node:fs/promises';
import {join} from 'node:path';
import {weightToDisplay,weightFromDisplay,heightToDisplay,heightFromDisplay,movementMask} from '../../tools/WebSimulation/wwwroot/athlete-units.mjs';
const base=process.env.FORMA_SIM_URL||'http://127.0.0.1:5078';
const profile={displayName:'Fixture Athlete',primaryGoal:'GeneralFitness',experience:'New',preferredMinutes:10,trainingDaysPerWeek:3,units:'Metric',language:'tr',equipment:[],ageRange:'Unspecified',preferredEnvironment:'HolographicStudio',movementLimits:0,voluntaryLimitations:'',heightCm:175,currentWeightKg:73};
async function client(){
 const response=await fetch(base+'/api/state');const cookie=response.headers.get('set-cookie').split(';')[0];
 const get=async path=>{const r=await fetch(base+path,{headers:{cookie}});assert.equal(r.status,200);return r.json();};
 const post=async(path,body)=>{const r=await fetch(base+path,{method:'POST',headers:{cookie,'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify(body)});return {status:r.status,...await r.json()};};
 const act=async(action,extra={})=>{const result=await post('/api/action',{action,...extra});assert.equal(result.status,200);return result;};
 return {get,post,act,cookie,id:cookie.split('=')[1]};
}
async function oldCompletion(c){
 for(const action of ['next','next','next'])await c.act(action);
 await c.act('safety',{field:'ack',enabled:true});await c.act('safety',{field:'space',enabled:true});await c.act('next');await c.act('finish');
 await c.act('confirmSpace',{enabled:true});await c.act('start');await c.act('advance',{seconds:600});
}
test('athlete create, reload, edit goal, export and confirmed delete preserve then remove associated history',async()=>{
 const c=await client();await oldCompletion(c);
 let r=await c.post('/api/athlete',{action:'save',profile,expectedRevision:0});assert.equal(r.status,200);const saved=r.state.profile;assert.match(saved.athleteId,/^[a-f0-9]{32}$/);assert.equal(r.state.completedSessionCount,1);
 await c.act('reload');assert.deepEqual((await c.get('/api/athlete')).profile,saved);
 r=await c.post('/api/athlete',{action:'save',profile:{...profile,primaryGoal:'Mobility'},expectedRevision:1});assert.equal(r.status,200);assert.equal(r.state.profile.athleteId,saved.athleteId);assert.equal(r.state.profile.createdUtc,saved.createdUtc);assert.equal(r.state.profile.preferences.primaryGoal,'Mobility');assert.equal(r.state.completedSessionCount,1);
 const exported=await c.get('/api/export');assert.equal(exported.sessions.filter(s=>s.outcome==='Completed').length,1);assert.equal(JSON.parse(exported.athlete.data).profile.athleteId,saved.athleteId);
 r=await c.post('/api/athlete',{action:'delete',confirmAthleteId:saved.athleteId,expectedRevision:2});assert.equal(r.status,400);assert.equal((await c.get('/api/athlete')).revision,2);
 r=await c.post('/api/athlete',{action:'delete',confirmation:'DELETE',confirmAthleteId:saved.athleteId,expectedRevision:2});assert.equal(r.status,200);assert.equal(r.state.profile,null);assert.equal((await c.get('/api/state')).history.length,0);
 if(process.env.FORMA_SIM_DATA){const {access}=await import('node:fs/promises');for(const name of ['athlete-profile.json','simulation-history.json','product-profile.json'])await assert.rejects(access(join(process.env.FORMA_SIM_DATA,c.id,name)));}
});
test('athlete validation, tab conflicts, and storage write failures cannot overwrite the saved profile',async()=>{
 const c=await client();assert.equal((await c.post('/api/athlete',{action:'save',profile,expectedRevision:0})).status,200);
 for(const patch of [{displayName:''},{currentWeightKg:-1},{heightCm:999},{primaryGoal:999},{trainingDaysPerWeek:8},{equipment:['Dumbbells','Dumbbells']}])assert.equal((await c.post('/api/athlete',{action:'save',profile:{...profile,...patch},expectedRevision:1})).status,400);
 assert.equal((await c.post('/api/athlete',{action:'save',profile:{...profile,displayName:'Other tab'},expectedRevision:0})).status,409);
 await c.act('writeFault',{enabled:true});assert.equal((await c.post('/api/athlete',{action:'save',profile:{...profile,displayName:'Not committed'},expectedRevision:1})).status,503);assert.equal((await c.get('/api/athlete')).profile.preferences.displayName,profile.displayName);await c.act('writeFault',{enabled:false});
 const other=await client();assert.equal((await other.get('/api/athlete')).profile,null);
});
test('schema-1 athlete migration preserves identity/history; future and invalid schemas are never overwritten',async()=>{
 if(!process.env.FORMA_SIM_DATA)return;
 const c=await client();await oldCompletion(c);let r=await c.post('/api/athlete',{action:'save',profile,expectedRevision:0});const identity=r.state.profile.athleteId;
 const path=join(process.env.FORMA_SIM_DATA,c.id,'athlete-profile.json'),legacy=JSON.parse(await readFile(path,'utf8'));legacy.schemaVersion=1;delete legacy.profile.preferences.units;delete legacy.profile.preferences.preferredEnvironment;await writeFile(path,JSON.stringify(legacy));
 const migrated=await c.get('/api/athlete');assert.equal(migrated.schemaVersion,2);assert.equal(migrated.profile.athleteId,identity);assert.equal(migrated.profile.preferences.units,'Metric');assert.equal(migrated.completedSessionCount,1);assert.equal(JSON.parse(await readFile(path,'utf8')).schemaVersion,2);
 for(const raw of [JSON.stringify({...legacy,schemaVersion:99}),'{broken-json']){
  await writeFile(path,raw);assert.equal((await c.get('/api/athlete')).status,'Unavailable');assert.equal((await c.post('/api/athlete',{action:'save',profile,expectedRevision:1})).status,503);assert.equal(await readFile(path,'utf8'),raw);assert.equal((await c.get('/api/state')).history.length,1);assert.equal((await c.get('/api/export')).athlete.data,raw);
 }
});
test('unit changes round-trip optional values and structured movement exclusions without inventing measurements',()=>{
 assert.equal(weightFromDisplay('', 'Metric'),null);assert.equal(heightFromDisplay('', 'Imperial'),null);
 assert.ok(Math.abs(weightFromDisplay(weightToDisplay(73,'Imperial'),'Imperial')-73)<1e-8);assert.ok(Math.abs(heightFromDisplay(heightToDisplay(175,'Imperial'),'Imperial')-175)<1e-8);
 assert.equal(movementMask('AvoidLowerBody, AvoidShoulders'),3);assert.equal(movementMask('Undisclosed'),0);assert.equal(movementMask(2),2);
});
test('stored athlete movement exclusions block a conflicting legacy workout instead of being silently ignored',async()=>{
 const c=await client();await oldCompletion(c);assert.equal((await c.post('/api/athlete',{action:'save',profile:{...profile,movementLimits:1},expectedRevision:0})).status,200);
 await c.act('dailyBack');await c.act('dailyFive');await c.act('confirmSpace',{enabled:true});const r=await c.act('start');assert.equal(r.accepted,false);assert.equal(r.state.workout.state,'Ready');assert.equal(r.state.history.filter(x=>x.outcome==='Completed').length,1);
});
