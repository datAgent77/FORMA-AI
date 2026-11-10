// Isolated process-restart verification; never uses a browser's real data directory.
import {spawn} from 'node:child_process';
import {mkdtemp,rm,mkdir,copyFile,access} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {resolve,join} from 'node:path';
import {createServer} from 'node:net';
import assert from 'node:assert/strict';
const root=resolve(new URL('..',import.meta.url).pathname),data=await mkdtemp(join(tmpdir(),'forma-athlete-restart-'));
const socket=createServer();await new Promise((ok,bad)=>{socket.once('error',bad);socket.listen(0,'127.0.0.1',ok);});const port=socket.address().port;await new Promise(ok=>socket.close(ok));
const base=`http://127.0.0.1:${port}`;let host,cookie,contentRoot=join(root,'tools/WebSimulation'),dataOverride=data,fixtureMode='1',tempOverride=process.env.TMPDIR;
async function boot(){host=spawn(process.env.FORMA_DOTNET||'dotnet',[join(root,'tools/WebSimulation/bin/Release/net8.0/WebSimulation.dll'),'--contentRoot',contentRoot],{env:{...process.env,FORMA_SIM_FIXTURE_DATA:fixtureMode,DOTNET_USE_POLLING_FILE_WATCHER:'1',DOTNET_CLI_TELEMETRY_OPTOUT:'1',FORMA_SIM_PORT:String(port),FORMA_SIM_DATA:dataOverride,...(tempOverride?{TMPDIR:tempOverride}:{})},stdio:'pipe'});let logs='';host.stderr.on('data',x=>logs+=x);host.stdout.on('data',x=>logs+=x);for(let i=0;i<450;i++){if(host.exitCode!==null)throw new Error(logs);try{if((await fetch(base+'/health',{signal:AbortSignal.timeout(1000)})).ok)return;}catch{}await new Promise(ok=>setTimeout(ok,100));}throw new Error('Isolated host failed to start: '+logs);}
async function stop(){if(!host||host.exitCode!==null)return;const done=new Promise(ok=>host.once('exit',ok));host.kill('SIGTERM');await done;}
async function get(path){const r=await fetch(base+path,{headers:cookie?{cookie}:{}});assert.equal(r.status,200);if(!cookie)cookie=r.headers.get('set-cookie').split(';')[0];return r.json();}
async function post(path,body){const r=await fetch(base+path,{method:'POST',headers:{cookie,'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify(body)});assert.equal(r.status,200);return r.json();}
try{
 console.log('ISOLATED FIXTURE ONLY: synthetic clock advancement and explicit test feedback; not a human workout.');
 await boot();await get('/api/state');
 const profile={displayName:'Coach Restart Fixture',primaryGoal:'GeneralFitness',preferredMinutes:10,trainingDaysPerWeek:3,language:'tr'};
 const saved=await post('/api/athlete',{action:'save',profile,expectedRevision:0}),id=saved.state.profile.athleteId;
 const act=(action,extra={})=>post('/api/action',{action,...extra});
 for(const action of ['next','next','next'])await act(action);await act('safety',{field:'ack',enabled:true});await act('safety',{field:'space',enabled:true});await act('next');await act('finish');
 async function complete(reusePlan=false){
  if(!reusePlan)await act('coachRecommend');await act('confirmSpace',{enabled:true});assert.equal((await act('coachStart')).accepted,true);
  for(let i=0;i<20;i++){
   let r=await act('advance',{seconds:600});const s=r.state.personal.session;assert.equal(s.awaitingFeedback,true);
   r=await act('coachFeedback',{value:{blockIndex:s.index,exerciseId:s.record.blocks[s.index].exerciseId,outcome:'Completed',difficulty:2,repetitions:null,discomfort:false}});
   assert.equal(r.accepted,true);if(r.state.personal.session.state==='COMPLETED')return r.state;
  }throw new Error('Completion gate not reached');
 }
 await complete();await stop();await boot();let state=await get('/api/state');assert.equal(state.athlete.profile.athleteId,id);assert.equal(state.personal.completedCount,1);assert.match(state.personal.greeting,/1 tamamlanan/);
 console.log('PASS: profile + first feedback-complete session recognized after real host restart.');
 await complete();await complete();await stop();await boot();state=(await act('coachRecommend')).state;
 const recommendation=state.personal.recommendation;assert.equal(recommendation.rule,'Consistent');assert.equal(recommendation.evidenceSessionIds.length,3);assert.equal(recommendation.blocks.find(b=>b.stage==='Exercise'&&b.exercise.id==='march').workSeconds,25);
 const exported=await get('/api/export'),training=JSON.parse(exported.personalTraining);assert.equal(training.sessions.filter(x=>x.outcome==='Completed').length,3);assert.ok(training.sessions.every(x=>x.feedback.every(f=>f.repetitions===null)));assert.ok(training.recommendations.some(x=>x.recommendationId===recommendation.id));
 console.log('PASS: three persisted easy sessions change the next march from 20 to 25 seconds, with three exact source session IDs and a persisted explanation. Repetitions remain null.');
 const planId=recommendation.id;state=(await act('coachEnvironment',{value:'Forest'})).state;assert.equal(state.personal.recommendation.id,planId);assert.equal(state.athlete.profile.athleteId,id);assert.equal(state.personal.completedCount,3);
 state=await complete(true);assert.equal(state.personal.session.record.environment,'Forest');assert.equal(state.personal.completedCount,4);
 const today=new Date().toISOString().slice(0,10),earlier=new Date(Date.now()-7*86400000).toISOString().slice(0,10);
 for(const value of [{kind:'Weight',value:73,measuredOn:earlier,source:'Isolated test scale',provenance:'ManualEntry'},{kind:'Weight',value:72.5,measuredOn:today,source:'Isolated test scale',provenance:'ManualEntry'}])await act('coachMeasurement',{value});
 await stop();await boot();state=await get('/api/state');assert.equal(state.personal.progress.daily.completedSessions,4);assert.equal(state.personal.progress.weekly.completedSessions,4);assert.equal(state.personal.progress.monthly.completedSessions,4);assert.ok(state.personal.progress.monthly.sessions.some(s=>s.targetIntervals.some(b=>b.exerciseId==='march'&&b.workSeconds===25)));assert.equal(state.personal.progress.measurements.length,2);assert.equal(state.athlete.profile.preferences.preferredEnvironment,'Forest');
 console.log('PASS: same athlete/plan/history in Forest; fourth session and two dated measurements survive restart; daily/weekly/monthly dashboard reconstructs four completions and their feedback.');
 await stop();fixtureMode='0';await boot();state=await get('/api/state');assert.equal(state.personal.completedCount,0);assert.equal(state.personal.progress.measurements.length,0);assert.equal(state.personal.decisions.length,0);assert.equal(state.personal.nextRecommendation,null);assert.equal(JSON.parse((await get('/api/export')).personalTraining).sessions.length,4);
 console.log('PASS: normal mode excludes marked test sessions/measurements from coach memory and progress without deleting exported records.');
 const current=await get('/api/athlete');await post('/api/athlete',{action:'delete',confirmation:'DELETE',confirmAthleteId:id,expectedRevision:current.revision});await stop();await boot();state=await get('/api/state');assert.equal(state.athlete.profile,null);assert.equal(state.personal.history.length,0);assert.equal((await get('/api/export')).personalTraining,null);
 console.log('PASS: confirmed deletion removes athlete, session/feedback history, recommendations and body measurements across restart.');

}finally{await stop();await rm(data,{recursive:true,force:true});}
