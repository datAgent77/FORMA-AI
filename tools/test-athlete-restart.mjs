// Isolated process-restart verification; never uses a browser's real data directory.
import {spawn} from 'node:child_process';
import {mkdtemp,rm,mkdir,copyFile,access} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {resolve,join} from 'node:path';
import {createServer} from 'node:net';
import assert from 'node:assert/strict';
const root=resolve(new URL('..',import.meta.url).pathname),data=await mkdtemp(join(tmpdir(),'forma-athlete-restart-'));
const socket=createServer();await new Promise((ok,bad)=>{socket.once('error',bad);socket.listen(0,'127.0.0.1',ok);});const port=socket.address().port;await new Promise(ok=>socket.close(ok));
const base=`http://127.0.0.1:${port}`;let host,cookie,contentRoot=join(root,'tools/WebSimulation'),dataOverride=data,tempOverride=process.env.TMPDIR;
async function boot(){host=spawn(process.env.FORMA_DOTNET||'dotnet',[join(root,'tools/WebSimulation/bin/Release/net8.0/WebSimulation.dll'),'--contentRoot',contentRoot],{env:{...process.env,DOTNET_USE_POLLING_FILE_WATCHER:'1',DOTNET_CLI_TELEMETRY_OPTOUT:'1',FORMA_SIM_PORT:String(port),FORMA_SIM_DATA:dataOverride,...(tempOverride?{TMPDIR:tempOverride}:{})},stdio:'pipe'});let logs='';host.stderr.on('data',x=>logs+=x);host.stdout.on('data',x=>logs+=x);for(let i=0;i<450;i++){if(host.exitCode!==null)throw new Error(logs);try{if((await fetch(base+'/health',{signal:AbortSignal.timeout(1000)})).ok)return;}catch{}await new Promise(ok=>setTimeout(ok,100));}throw new Error('Isolated host failed to start: '+logs);}
async function stop(){if(!host||host.exitCode!==null)return;const done=new Promise(ok=>host.once('exit',ok));host.kill('SIGTERM');await done;}
async function get(path){const r=await fetch(base+path,{headers:cookie?{cookie}:{}});assert.equal(r.status,200);if(!cookie)cookie=r.headers.get('set-cookie').split(';')[0];return r.json();}
async function post(path,body){const r=await fetch(base+path,{method:'POST',headers:{cookie,'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify(body)});assert.equal(r.status,200);return r.json();}
try{
 await boot();await get('/api/athlete');
 let r=await post('/api/athlete',{action:'save',profile:{displayName:'Restart Fixture',primaryGoal:'Consistency',preferredMinutes:10,trainingDaysPerWeek:2},expectedRevision:0});const id=r.state.profile.athleteId;
 const act=(action,extra={})=>post('/api/action',{action,...extra});
 for(const action of ['next','next','next'])await act(action);await act('safety',{field:'ack',enabled:true});await act('safety',{field:'space',enabled:true});await act('next');await act('finish');await act('confirmSpace',{enabled:true});await act('start');await act('advance',{seconds:600});
 await stop();await boot();let state=await get('/api/athlete');assert.equal(state.profile.athleteId,id);assert.equal(state.profile.preferences.primaryGoal,'Consistency');assert.equal(state.completedSessionCount,1);
 const input={...state.profile.preferences,primaryGoal:'Mobility'};await post('/api/athlete',{action:'save',profile:input,expectedRevision:1});await stop();await boot();state=await get('/api/athlete');assert.equal(state.profile.preferences.primaryGoal,'Mobility');assert.equal(state.completedSessionCount,1);
 const exported=await get('/api/export');assert.equal(exported.sessions.length,1);assert.equal(JSON.parse(exported.athlete.data).profile.athleteId,id);
 await stop();contentRoot=join(data,'isolated-content');tempOverride=join(data,'isolated-temp');await mkdir(contentRoot);await mkdir(tempOverride);
 const legacy=join(tempOverride,'forma-web-simulation',cookie.split('=')[1]);await mkdir(legacy,{recursive:true});
 for(const name of ['athlete-profile.json','product-profile.json','simulation-history.json'])await copyFile(join(data,cookie.split('=')[1],name),join(legacy,name));
 dataOverride='';await boot();state=await get('/api/athlete');assert.equal(state.profile.athleteId,id);assert.equal(state.completedSessionCount,1);
 await post('/api/athlete',{action:'delete',confirmation:'DELETE',confirmAthleteId:id,expectedRevision:2});for(const name of ['athlete-profile.json','product-profile.json','simulation-history.json'])await assert.rejects(access(join(legacy,name)));
 await stop();await boot();state=await get('/api/athlete');assert.equal(state.profile,null);assert.equal(state.completedSessionCount,0);
 console.log('PASS: profile + completed history survive two real host restarts; goal edit persists; default-store legacy migration preserves identity/history; deletion removes legacy copies and does not resurrect after restart. All fixture data isolated.');
}finally{await stop();await rm(data,{recursive:true,force:true});}
