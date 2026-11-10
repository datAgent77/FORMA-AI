import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile,writeFile} from 'node:fs/promises';
import {join} from 'node:path';
const base=process.env.FORMA_SIM_URL||'http://127.0.0.1:5078';
async function athlete(name='Isolated Coach Fixture'){
 const response=await fetch(base+'/api/state'),cookie=response.headers.get('set-cookie').split(';')[0];
 const request=async(path,body)=>{const r=await fetch(base+path,{method:body?'POST':'GET',headers:{cookie,'Content-Type':'application/json','X-Forma-Simulation':'1'},...(body?{body:JSON.stringify(body)}:{})});return {status:r.status,...await r.json()};};
 const act=async(action,extra={})=>{const r=await request('/api/action',{action,...extra});assert.equal(r.status,200,JSON.stringify(r));return r;};
 await request('/api/athlete',{action:'save',expectedRevision:0,profile:{displayName:name,primaryGoal:'GeneralFitness',preferredMinutes:10,trainingDaysPerWeek:3,language:'tr'}});
 for(const action of ['next','next','next'])await act(action);await act('safety',{field:'ack',enabled:true});await act('safety',{field:'space',enabled:true});await act('next');await act('finish');
 return {act,request,cookie,id:cookie.split('=')[1]};
}
async function begin(c){await c.act('coachRecommend');await c.act('confirmSpace',{enabled:true});const r=await c.act('coachStart');assert.equal(r.accepted,true);return r.state;}
async function finish(c,rating=2){
 let state=await begin(c);
 for(let i=0;i<20;i++){
  let r=await c.act('advance',{seconds:600});state=r.state;const s=state.personal.session;
  assert.equal(s.awaitingFeedback,true);r=await c.act('coachFeedback',{value:{blockIndex:s.index,exerciseId:s.record.blocks[s.index].exerciseId,outcome:'Completed',difficulty:rating,repetitions:null,discomfort:false}});assert.equal(r.accepted,true);state=r.state;
  if(state.personal.session.state==='COMPLETED')return state;
 }
 throw new Error('Session failed to reach completion');
}
test('personal session has warm-up/core/cooldown, exact pause, mandatory feedback and durable completion without fabricated repetitions',async()=>{
 const c=await athlete();let state=await begin(c);const blocks=state.personal.recommendation.blocks;assert.equal(blocks[0].stage,'WarmUp');assert.equal(blocks.at(-1).stage,'Cooldown');assert.equal(state.personal.catalog.length,6);assert.equal(state.personal.recommendation.durationSeconds,300);
 state=(await c.act('advance',{seconds:12})).state;assert.equal(state.workout.remaining,33);await c.act('pause');await c.act('advance',{seconds:100});state=(await c.request('/api/state')).personal;assert.equal(state.session.state,'PAUSED');assert.equal((await c.act('resume')).accepted,false);await c.act('confirmSpace',{enabled:true});assert.equal((await c.act('resume')).accepted,true);
 state=(await c.act('advance',{seconds:600})).state;assert.equal(state.personal.session.state,'AWAITING_FEEDBACK');assert.equal(state.personal.session.record.elapsedSeconds,45);assert.equal((await c.act('advance',{seconds:600})).accepted,false);
 const s=state.personal.session;assert.equal((await c.request('/api/action',{action:'coachFeedback',value:{blockIndex:s.index,exerciseId:s.record.blocks[s.index].exerciseId,outcome:'Completed'}})).status,400);
 await c.act('stop');assert.equal((await c.request('/api/athlete')).completedSessionCount,0);
 state=await finish(c);assert.equal(state.personal.session.state,'COMPLETED');assert.equal(state.personal.session.record.elapsedSeconds,300);assert.equal(state.personal.history.filter(x=>x.outcome==='Completed').length,1);assert.ok(state.personal.session.record.feedback.every(x=>x.repetitions===null));
 const count=state.personal.history.length;assert.equal((await c.act('coachFeedback',{value:{blockIndex:7,exerciseId:'shoulders',outcome:'Completed',difficulty:2}})).accepted,false);await c.act('reload');state=await c.request('/api/state');assert.equal(state.personal.history.length,count);assert.equal(state.personal.history.filter(x=>x.outcome==='Completed').length,1);
 if(process.env.FORMA_SIM_DATA){const disk=JSON.parse(await readFile(join(process.env.FORMA_SIM_DATA,c.id,'personal-training.json'),'utf8'));assert.equal(disk.sessions.filter(x=>x.outcome==='Completed').length,1);assert.equal(disk.sessions.filter(x=>x.outcome==='Cancelled').length,1);}
});
test('manual skip does not add work time; discomfort cancels and blocks progression with persisted evidence',async()=>{
 const c=await athlete();let state=await begin(c);await c.act('advance',{seconds:10});state=(await c.act('coachSkip')).state;assert.equal(state.personal.session.state,'SKIPPED');assert.equal(state.personal.session.record.timedActiveSeconds,0);
 const s=state.personal.session;assert.equal((await c.act('coachFeedback',{value:{blockIndex:s.index,exerciseId:s.record.blocks[s.index].exerciseId,outcome:'Skipped',difficulty:null,repetitions:null,discomfort:true}})).accepted,true);
 await c.act('confirmSpace',{enabled:true});assert.equal((await c.act('coachStart')).accepted,false,'old recommendation cannot bypass a new discomfort record');assert.equal((await c.act('start')).accepted,false,'legacy start cannot bypass discomfort');
 state=(await c.act('coachRecommend')).state;assert.equal(state.personal.recommendation.rule,'Discomfort');assert.equal(state.personal.recommendation.status,'SafetyStop');assert.equal(state.personal.recommendation.blocks.length,0);assert.equal(state.personal.recommendation.evidenceSessionIds[0],s.id);assert.equal(state.personal.completedCount,0);
});
test('real persisted easy/high-difficulty/returning histories create different explained recommendations, not invented achievements',async()=>{
 const easy=await athlete('Easy fixture');let state;
 for(let i=0;i<3;i++)state=await finish(easy,2);
 await easy.act('reload');state=(await easy.act('coachRecommend')).state;const r=state.personal.recommendation;assert.equal(r.rule,'Consistent');assert.equal(r.evidenceSessionIds.length,3);assert.equal(r.blocks.find(x=>x.stage==='Exercise'&&x.exercise.id==='march').workSeconds,25);assert.match(r.reasons.join(' '),/üç/);
 const again=(await easy.act('coachRecommend')).state.personal.recommendation;assert.equal(again.blocks.find(x=>x.stage==='Exercise'&&x.exercise.id==='march').workSeconds,25,'requesting again cannot manufacture another progression');
 const hard=await athlete('Hard fixture');await finish(hard,4);await finish(hard,5);state=(await hard.act('coachRecommend')).state;assert.equal(state.personal.recommendation.rule,'HighDifficulty');assert.equal(state.personal.recommendation.evidenceSessionIds.length,2);assert.ok(state.personal.recommendation.blocks.filter(x=>x.stage==='Exercise').every(x=>x.workSeconds===15&&x.restSeconds===20));
 if(process.env.FORMA_SIM_DATA){
  const path=join(process.env.FORMA_SIM_DATA,easy.id,'personal-training.json'),d=JSON.parse(await readFile(path,'utf8'));const old=new Date(Date.now()-20*86400000).toISOString();for(const s of d.sessions){s.startedUtc=old;s.endedUtc=old;for(const f of s.feedback)f.recordedUtc=old;}await writeFile(path,JSON.stringify(d));await easy.act('reload');state=(await easy.act('coachRecommend')).state;assert.equal(state.personal.recommendation.rule,'Returning');assert.match(state.personal.recommendation.reasons.join(' '),/20 gün/);
 }
 const beginner=await athlete('Beginner fixture');state=(await beginner.act('coachRecommend')).state;assert.equal(state.personal.recommendation.rule,'Beginner');assert.equal(state.personal.completedCount,0);assert.equal(state.personal.recommendation.evidenceSessionIds.length,0);
});
test('failed feedback persistence stops the session and can be explicitly retried without duplicate feedback',async()=>{
 const c=await athlete();let state=await begin(c);state=(await c.act('advance',{seconds:600})).state;const s=state.personal.session;
 await c.act('writeFault',{enabled:true});assert.equal((await c.request('/api/action',{action:'coachFeedback',value:{blockIndex:s.index,exerciseId:s.record.blocks[s.index].exerciseId,outcome:'Completed',difficulty:2}})).status,400);
 state=await c.request('/api/state');assert.equal(state.personal.savePending,true);assert.equal(state.workout.isRunning,false);assert.equal(state.personal.history[0].feedback.length,0);
 await c.act('writeFault',{enabled:false});state=(await c.act('coachRetrySave')).state;assert.equal(state.personal.savePending,false);assert.equal(state.personal.history[0].feedback.length,1);assert.equal((await c.act('resume')).accepted,false);await c.act('confirmSpace',{enabled:true});assert.equal((await c.act('resume')).accepted,true);await c.act('stop');
});

test('a session whose intervals are all explicitly skipped is not counted completed',async()=>{
 const c=await athlete('Skipped fixture');let state=await begin(c);
 for(let i=0;i<8;i++){
  state=(await c.act('advance',{seconds:600})).state;const s=state.personal.session;
  state=(await c.act('coachFeedback',{value:{blockIndex:s.index,exerciseId:s.record.blocks[s.index].exerciseId,outcome:'Skipped',difficulty:null,repetitions:null,discomfort:false}})).state;
 }
 assert.equal(state.personal.session.state,'SKIPPED');assert.equal(state.personal.history[0].outcome,'Skipped');assert.equal(state.personal.completedCount,0);assert.equal(state.workout.state,'Aborted');
});
