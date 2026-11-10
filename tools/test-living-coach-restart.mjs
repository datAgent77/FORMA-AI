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

 await act('coachLiving');await act('confirmSpace',{enabled:true});assert.equal((await act('coachStart')).accepted,true);
 let state;
 for(let i=0;i<5;i++){
  state=(await act('advance',{seconds:600})).state;const s=state.personal.session;assert.equal(s.index,i);assert.ok(s.awaitingFeedback);
  state=(await act('coachFeedback',{value:{blockIndex:i,exerciseId:s.record.blocks[i].exerciseId,outcome:'Completed',difficulty:2,repetitions:null,discomfort:false}})).state;
 }
 const sessionId=state.personal.session.id;assert.equal(state.personal.session.record.elapsedSeconds,300);assert.equal(state.personal.session.record.workoutId,'forma-start-5-v1');assert.equal(state.personal.completedCount,1);
 await act('coachRetrySave');await stop();await boot();state=await get('/api/state');assert.equal(state.athlete.profile.athleteId,id);assert.equal(state.personal.completedCount,1);assert.equal(state.personal.history.filter(s=>s.sessionId===sessionId).length,1);assert.equal(state.personal.progress.daily.completedSessions,1);assert.equal(state.personal.history[0].trackingMode,'user-confirmed');assert.equal(state.personal.history[0].feedback.length,5);
 console.log('PASS: Living Coach full 300-second fixture, feedback, stable athlete/workout identity, single completion and actual progress survive a real host process restart. No physical movement or Quest tracking was tested.');
}finally{await stop();await rm(data,{recursive:true,force:true});}
