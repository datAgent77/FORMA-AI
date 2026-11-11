import * as T from './vendor/three/three.module.js';
// Rendering adapter only; explicit times supplied by the workout/diagnostic clock.
export function createMotionController(avatar,clips,bones,{afterUpdate=()=>{}}={}){
 const mixer=new T.AnimationMixer(avatar),actions=new Map(clips.map(c=>[c.name,mixer.clipAction(c)]));let current=null,outgoing=null,outgoingClip=null,name=null,blend=1,disposed=false,wasFrozen=false;
 const release=()=>{if(outgoing){outgoing.stop();mixer.uncacheAction(outgoingClip);mixer.uncacheClip(outgoingClip);outgoing=null;outgoingClip=null;}};
 function snapshot(){const tracks=[];for(const b of bones.values()){tracks.push(new T.QuaternionKeyframeTrack(b.uuid+'.quaternion',[0,1],[...b.quaternion.toArray(),...b.quaternion.toArray()]),new T.VectorKeyframeTrack(b.uuid+'.position',[0,1],[...b.position.toArray(),...b.position.toArray()]));}return new T.AnimationClip('transition_pose',1,tracks);}
 return {mixer,clips,bones,actions,get state(){return name;},get blending(){return blend<1;},get disposed(){return disposed;},update(next,seconds,dt=0,freeze=false){
  if(disposed)throw new Error('Motion controller disposed');if(!actions.has(next))throw new Error('Missing animation: '+next);
  if(freeze&&current){wasFrozen=true;return;} // preserve both the pose and transition progress until explicit resume
  if(next!==name||wasFrozen){wasFrozen=false;const pose=current?snapshot():null;release();mixer.stopAllAction();current=actions.get(next);current.reset().play();current.paused=true;name=next;blend=pose?0:1;if(pose){outgoingClip=pose;outgoing=mixer.clipAction(pose).play();outgoing.paused=true;}}
  blend=Math.min(1,blend+Math.max(0,dt)/.4);const weight=blend*blend*(3-2*blend);current.time=Math.max(0,seconds)%current.getClip().duration;current.setEffectiveWeight(weight);if(outgoing)outgoing.setEffectiveWeight(1-weight);mixer.update(0);afterUpdate(name,!!outgoing);if(blend===1)release();
 },dispose(){if(disposed)return;release();mixer.stopAllAction();mixer.uncacheRoot(avatar);actions.clear();disposed=true;}};
}
