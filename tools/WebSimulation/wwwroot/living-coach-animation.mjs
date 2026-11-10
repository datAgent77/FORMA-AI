import * as THREE from './vendor/three/three.module.js';
export const LIVING_ANIMATIONS=Object.freeze(['idle','welcome','demonstrate_march','march_loop','demonstrate_shoulder_circles','shoulder_circles_loop','demonstrate_knee_raise','knee_raise_loop','demonstrate_mobility','mobility_loop','rest','cooldown','celebrate']);
const jointNames=['Hips','Abdomen','Torso','Head','ShoulderL','ShoulderR','UpperArmL','UpperArmR','LowerArmL','LowerArmR','UpperLegL','UpperLegR','LowerLegL','LowerLegR','FootL','FootR'];
// Original authored skeletal curves, radians. Model-local axes; no root translation/spin.
export function exerciseAngles(name,t){
 const p=t*Math.PI*(/knee_raise/.test(name)?1:1.5),s=Math.sin(p),left=Math.max(0,s),right=Math.max(0,-s),a={};
 const set=(bone,x=0,y=0,z=0)=>a[bone]=[x,y,z];
 if(/march|knee_raise/.test(name)){
  const knee=/knee_raise/.test(name),hip=knee?.78:.40,bend=knee?1.05:.58;
  set('UpperLegL',-hip*left);set('UpperLegR',-hip*right);
  set('LowerLegL',bend*left);set('LowerLegR',bend*right);
  // Foot bones are exported IK controls, not children of the shin. Position tracks follow FK shin endpoints below.
  set('UpperArmL',.22*s,0,0);set('UpperArmR',-.22*s,0,0);
  set('LowerArmL',0,0,-.08);set('LowerArmR',0,0,.08);
 }else if(/shoulder_circles/.test(name)){
  const slow=t*Math.PI/2;
  set('ShoulderL',.10*Math.sin(slow),0,.10*Math.cos(slow));set('ShoulderR',.10*Math.sin(slow),0,-.10*Math.cos(slow));
  set('UpperArmL',.13*Math.sin(slow),.07*Math.cos(slow),-.12-.07*Math.cos(slow));
  set('UpperArmR',.13*Math.sin(slow),-.07*Math.cos(slow),.12+.07*Math.cos(slow));
 }else if(/mobility|cooldown/.test(name)){
  const slow=Math.sin(t*Math.PI/3),size=name==='cooldown'?.5:1;
  set('Abdomen',0,.10*slow*size,.045*slow*size);
  set('ShoulderL',.04*slow*size,0,-.06*size);set('ShoulderR',-.04*slow*size,0,.06*size);
  set('UpperArmL',.12*slow*size,0,-.18*size);set('UpperArmR',-.12*slow*size,0,.18*size);
 }else if(name==='welcome'){
  set('Head',.035*Math.sin(t*Math.PI/2));set('UpperArmR',-.12,0,.28);set('LowerArmR',0,0,.38);
 }else if(name==='celebrate'){
  set('UpperArmL',-.12,0,-.32);set('UpperArmR',-.12,0,.32);set('Head',.025*Math.sin(t*Math.PI));
 }else if(name==='rest'||name==='idle')set('Abdomen',.008*Math.sin(t*Math.PI/2));
 return a;
}
export function createLivingAnimation(avatar){
 const bones=new Map();avatar.traverse(o=>{if(o.isBone)bones.set(o.name.replaceAll('.',''),o);});
 for(const name of ['UpperLegL','UpperLegR','LowerLegL','LowerLegR','UpperArmL','UpperArmR','ShoulderL','ShoulderR','FootL','FootR'])if(!bones.has(name))throw new Error('Required coach joint missing: '+name);
 const rest=new Map([...bones].map(([n,b])=>[n,b.quaternion.clone()]));
 avatar.updateMatrixWorld(true);const footOrigins=new Map();for(const side of ['L','R']){const foot=bones.get('Foot'+side),tip=bones.get('LowerLeg'+side)?.children.find(b=>b.name.endsWith('_end'));if(!tip)throw new Error('Missing shin endpoint');footOrigins.set(side,{foot,tip,position:foot.position.clone(),tipPosition:foot.parent.worldToLocal(tip.getWorldPosition(new THREE.Vector3()))});}
 const mixer=new THREE.AnimationMixer(avatar),actions=new Map(),clips=[];
 const euler=new THREE.Euler(),delta=new THREE.Quaternion();
 for(const name of LIVING_ANIMATIONS){
  const duration=/shoulder/.test(name)?4:/mobility|cooldown/.test(name)?6:/knee/.test(name)?2:/march/.test(name)?4/3:4,tracks=[];
  for(const n of jointNames){const bone=bones.get(n);if(!bone)continue;const times=[],values=[];
   for(let i=0;i<=48;i++){const t=i*duration/48;times.push(t);const xyz=exerciseAngles(name,t)[n]||[0,0,0];delta.setFromEuler(euler.set(...xyz));rest.get(n).clone().multiply(delta).toArray(values,values.length);}
   // Bind by UUID. GLTFLoader sanitizes dots in names; old literal 'UpperLeg.L' offsets never found these joints.
   tracks.push(new THREE.QuaternionKeyframeTrack(bone.uuid+'.quaternion',times,values));
  }
  for(const side of ['L','R']){const {foot,tip,position,tipPosition}=footOrigins.get(side),times=[],values=[];
   for(let i=0;i<=48;i++){const t=i*duration/48,angles=exerciseAngles(name,t);for(const [n,bone] of bones){const xyz=angles[n]||[0,0,0];bone.quaternion.copy(rest.get(n)).multiply(delta.setFromEuler(euler.set(...xyz)));}avatar.updateMatrixWorld(true);const end=foot.parent.worldToLocal(tip.getWorldPosition(new THREE.Vector3()));position.clone().add(end.sub(tipPosition)).toArray(values,values.length);times.push(t);}
   tracks.push(new THREE.VectorKeyframeTrack(foot.uuid+'.position',times,values));
  }
  for(const [n,bone] of bones)bone.quaternion.copy(rest.get(n));for(const {foot,position} of footOrigins.values())foot.position.copy(position);
  const clip=new THREE.AnimationClip(name,duration,tracks);clips.push(clip);actions.set(name,mixer.clipAction(clip));
 }
 // Only foot-influenced low vertices need grounding; avoid a whole-avatar bounds scan each frame.
 avatar.updateMatrixWorld(true);const inverse=new THREE.Matrix4().copy(avatar.parent?.matrixWorld||new THREE.Matrix4()).invert(),v=new THREE.Vector3(),ground=[];
 avatar.traverse(mesh=>{if(!mesh.isSkinnedMesh)return;const indices=[];mesh.skeleton.update();for(let i=0;i<mesh.geometry.attributes.position.count;i++){mesh.getVertexPosition(i,v);v.applyMatrix4(mesh.matrixWorld).applyMatrix4(inverse);if(v.y<.38)indices.push(i);}if(indices.length)ground.push([mesh,indices]);});
 const baseY=avatar.position.y;let current=null,previous=null,transition=1,lastState=null;
 function groundFeet(){avatar.position.y=baseY;avatar.updateMatrixWorld(true);let min=Infinity;for(const [mesh,indices] of ground){mesh.skeleton.update();for(const i of indices){mesh.getVertexPosition(i,v);v.applyMatrix4(mesh.matrixWorld).applyMatrix4(inverse);min=Math.min(min,v.y);}}if(Number.isFinite(min)){avatar.position.y=baseY-min;avatar.updateMatrixWorld(true);}}
 return {mixer,clips,bones,actions,get state(){return lastState;},update(name,seconds,dt=0,freeze=false){
  if(!actions.has(name))name='idle';
  if(name!==lastState){previous=current;current=actions.get(name);current.reset().play();current.enabled=true;current.paused=true;transition=previous?0:1;lastState=name;}
  if(!freeze){transition=Math.min(1,transition+Math.max(0,dt)/.3);current.time=Math.max(0,seconds)%current.getClip().duration;}
  current.setEffectiveWeight(transition);if(previous&&previous!==current){previous.setEffectiveWeight(1-transition);if(transition===1){previous.stop();previous=null;}}
  // Explicit action times come from the authoritative C# clock. Mixer owns skeletal interpolation, not workout timing.
  mixer.update(0);groundFeet();
 },dispose(){mixer.stopAllAction();mixer.uncacheRoot(avatar);}};
}
