const NO_EVENTS=Object.freeze([]);
export const MotionStates=Object.freeze(['IDLE','INTRO','DEMONSTRATE','ACTIVE','PAUSED','REST','COMPLETE']);
export function motionPhase(definition,seconds){const t=((Math.max(0,seconds)%definition.duration)+definition.duration)%definition.duration;for(const p of definition.phases)if(t>=p.start&&t<p.end)return p;return definition.phases[0];}
export class MotionEventCursor{
 constructor(definition){this.definition=definition;this.last=null;}
 reset(seconds=null){this.last=seconds;}
 advance(seconds,{paused=false,seek=false}={}){
  if(!Number.isFinite(seconds)||seconds<0)throw new Error('Invalid motion time');if(seek){this.last=seconds;return NO_EVENTS;}if(paused)return NO_EVENTS;
  const previous=this.last;if(previous!==null&&seconds<=previous)return NO_EVENTS;this.last=seconds;
  const start=previous??-.000001,d=this.definition.duration;let result=null;
  // A large jump is inspection/network discontinuity, not hundreds of instructions to replay.
  const low=Math.max(0,Math.floor(Math.max(start,seconds-d*2)/d));
  for(let cycle=low;cycle<=Math.floor(seconds/d);cycle++)for(let i=0;i<this.definition.events.length;i++){const e=this.definition.events[i],time=cycle*d+e.time;if(time>start&&time<=seconds){if(!result)result=[];result.push({...e,time,cycle,id:cycle+':'+i});}}
  if(!result)return NO_EVENTS;return result.sort((a,b)=>a.time-b.time||a.cycle-b.cycle);
 }
}
// A bounded current-phase queue: never play a stale 'lift' after lowering has started.
export class MotionCueQueue{
 constructor(definition){this.definition=definition;this.seen=new Set();this.pending=null;}
 reset(){this.seen.clear();this.pending=null;}
 accept(events,seconds,language,scope){for(const event of events){if(!event.cue||this.seen.has(event.id))continue;this.seen.add(event.id);if(this.seen.size>64)this.seen.delete(this.seen.values().next().value);const phase=motionPhase(this.definition,event.time+.00001);this.pending={key:scope+':'+event.id,text:this.definition.cues[event.cue][language],language,interrupted:false,expires:event.cycle*this.definition.duration+phase.end};}if(this.pending&&this.pending.expires<=seconds)this.pending=null;const next=this.pending;this.pending=null;return next;}
}
export class CoachingOrchestrator{
 constructor(definition){this.definition=definition;this.cursor=new MotionEventCursor(definition);this.cues=new MotionCueQueue(definition);this.scope=null;this.currentCue=null;this.events=[];this.state='IDLE';this.view={};}
 update({scope,seconds,state,language='tr',seek=false}){
  if(!MotionStates.includes(state))throw new Error('Unsupported motion state');
  if(scope!==this.scope){this.scope=scope;this.cursor.reset();this.cues.reset();this.currentCue=null;this.events=[];}
  if(seek){this.events=[];this.cues.reset();}
  this.state=state;const paused=state==='PAUSED',playing=state==='DEMONSTRATE'||state==='ACTIVE';
  const events=this.cursor.advance(seconds,{paused:paused||!playing,seek});if(!playing)this.cues.pending=null;
  if(events.length)this.events=[...this.events,...events].slice(-24);
  let cancelVoice=!!this.currentCue&&(!playing||seek||seconds>=this.currentCue.expires);if(cancelVoice)this.currentCue=null;
  const phase=motionPhase(this.definition,seconds),cue=playing&&!seek?this.cues.accept(events,seconds,language,scope):null;if(cue)this.currentCue=cue;
  const caption=this.definition.cues[phase.name==='prepare'&&phase.side==='right'?'stand':phase.name==='prepare'||phase.name==='lift'?phase.side:phase.name==='lower'?'lower':phase.name==='recover'?'recover':phase.name==='peak'?'hold':phase.side][language];
  Object.assign(this.view,{state,phase,events,cue,caption,cancelVoice});return this.view;
 }
 reset(){this.scope=null;this.currentCue=null;this.cursor.reset();this.cues.reset();this.events=[];this.state='IDLE';}
}
