// Offline authoring tool. Whole-body key poses + analytical limb constraints -> immutable skeletal tracks.
// Runtime playback does not solve IK or synthesize per-joint sine waves for this exercise.
import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import * as T from '../WebSimulation/wwwroot/vendor/three/three.module.js';
import {GLTFLoader} from '../WebSimulation/wwwroot/vendor/three/GLTFLoader.js';
const base=new URL('../WebSimulation/wwwroot/',import.meta.url),source=JSON.parse(await readFile(new URL('assets/motion/standing-alternating-knee-raise.source.json',base),'utf8'));
const bytes=await readFile(new URL('assets/coach/woman-casual.glb',base));if(createHash('sha256').update(bytes).digest('hex')!==source.rigSha256)throw new Error('Rig source changed; re-review retargeting.');
const gltf=await new GLTFLoader().parseAsync(bytes.buffer.slice(bytes.byteOffset,bytes.byteOffset+bytes.byteLength),'');
const avatar=gltf.scene,parent=new T.Group();parent.add(avatar);const idle=new T.AnimationMixer(avatar);idle.clipAction(gltf.animations.find(c=>c.name.endsWith('Female_Idle'))).play();idle.update(.01);idle.stopAllAction(); // stop restores bind pose: apply frozen neutral again
const neutral=new T.AnimationMixer(avatar),action=neutral.clipAction(gltf.animations.find(c=>c.name.endsWith('Female_Idle'))).play();neutral.update(.01);action.paused=true;
avatar.updateMatrixWorld(true);let box=new T.Box3().setFromObject(avatar);avatar.scale.multiplyScalar(1.75/(box.max.y-box.min.y));avatar.updateMatrixWorld(true);box=new T.Box3().setFromObject(avatar);avatar.position.y-=box.min.y;avatar.updateMatrixWorld(true);
const bones=new Map();avatar.traverse(b=>{if(b.isBone)bones.set(b.name,b);});
const rest=new Map([...bones].map(([name,b])=>[name,{p:b.position.clone(),q:b.quaternion.clone(),world:b.getWorldPosition(new T.Vector3()),wq:b.getWorldQuaternion(new T.Quaternion())}]));
const pos=b=>b.getWorldPosition(new T.Vector3());
const chains={};for(const side of ['L','R']){const up=bones.get('UpperLeg'+side),low=bones.get('LowerLeg'+side),end=low.children.find(c=>c.name.endsWith('_end'));chains[side]={up,low,end,start:pos(up),joint:pos(low),tip:pos(end)};}
function aim(bone,restDirection,direction){const world=new T.Quaternion().setFromUnitVectors(restDirection.clone().normalize(),direction.clone().normalize()).multiply(rest.get(bone.name).wq);bone.quaternion.copy(bone.parent.getWorldQuaternion(new T.Quaternion()).invert().multiply(world));avatar.updateMatrixWorld(true);}
function solve(chain,target,pole=new T.Vector3(0,0,1)){
 const {up,low,end,start,joint,tip}=chain,hip=pos(up),l1=start.distanceTo(joint),l2=joint.distanceTo(tip),axis=target.clone().sub(hip),d=axis.length();if(d>l1+l2-.0001||d<Math.abs(l1-l2)+.0001)throw new Error('Unreachable authored limb target '+up.name+' '+d);axis.normalize();
 const projection=pole.clone().addScaledVector(axis,-pole.dot(axis)).normalize(),along=(l1*l1-l2*l2+d*d)/(2*d),height=Math.sqrt(Math.max(0,l1*l1-along*along));const knee=hip.clone().addScaledVector(axis,along).addScaledVector(projection,height);
 aim(up,joint.clone().sub(start),knee.clone().sub(hip));aim(low,tip.clone().sub(joint),target.clone().sub(pos(low)));
 if(pos(end).distanceTo(target)>.0001)throw new Error('IK residual '+up.name);
}
function poseAt(time){const keys=source.keyPoses;let i=0;while(i<keys.length-2&&time>keys[i+1].time)i++;const a=keys[i],b=keys[i+1],u=T.MathUtils.clamp((time-a.time)/(b.time-a.time),0,1),h=u*u*u*(u*(u*6-15)+10); // quintic zero velocity/acceleration at authored landmarks
 const blend=(x,y)=>Array.isArray(x)?x.map((v,j)=>v+(y[j]-v)*h):x+(y-x)*h;
 return {pelvis:blend(a.pelvis,b.pelvis),leftFoot:blend(a.leftFoot,b.leftFoot),rightFoot:blend(a.rightFoot,b.rightFoot),arms:blend(a.arms,b.arms),torso:blend(a.torso,b.torso)};
}
const animated=['Body','Hips','Abdomen','Torso','Neck','Head','ShoulderL','ShoulderR','UpperArmL','UpperArmR','LowerArmL','LowerArmR','PalmL','PalmR','UpperLegL','UpperLegR','LowerLegL','LowerLegR','FootL','FootR'];
const channels=new Map(animated.map(n=>[n,{q:[],p:[]}])) ,times=[];
for(let frame=0;frame<=source.duration*60;frame++){
 const time=frame/60,pose=poseAt(time);times.push(time);
 for(const [n,b] of bones){b.position.copy(rest.get(n).p);b.quaternion.copy(rest.get(n).q);}avatar.updateMatrixWorld(true);
 const body=bones.get('Body');body.position.copy(body.parent.worldToLocal(rest.get('Body').world.clone().add(new T.Vector3(...pose.pelvis))));
 bones.get('Abdomen').quaternion.multiply(new T.Quaternion().setFromAxisAngle(new T.Vector3(0,0,1),-pose.torso));avatar.updateMatrixWorld(true);
 for(const side of ['L','R']){
  const lift=new T.Vector3(...(side==='L'?pose.leftFoot:pose.rightFoot)),foot=bones.get('Foot'+side);foot.position.copy(foot.parent.worldToLocal(rest.get(foot.name).world.clone().add(lift)));solve(chains[side],chains[side].tip.clone().add(lift));
  // Contralateral hand path, authored with the same full-body key poses. Modest forward lift + soft elbows.
  const up=bones.get('UpperArm'+side),low=bones.get('LowerArm'+side),end=bones.get('Palm'+side),s=side==='L'?1:-1,forward=s*pose.arms;
  const arm={up,low,end,start:rest.get(up.name).world,joint:rest.get(low.name).world,tip:rest.get(end.name).world};
  const target=arm.tip.clone().add(new T.Vector3(s*.018, .028+Math.max(0,forward)*.045,forward*.10));target.x+=pose.pelvis[0];target.y+=pose.pelvis[1];
  solve(arm,target,new T.Vector3(s*.3,0,forward));
  // Keep relaxed palms close to their neutral world orientation instead of rolling wrists with the elbow solution.
  end.quaternion.copy(end.parent.getWorldQuaternion(new T.Quaternion()).invert().multiply(rest.get(end.name).wq));
 }
 avatar.updateMatrixWorld(true);
 for(const n of animated){const bone=bones.get(n),c=channels.get(n);bone.quaternion.toArray(c.q,c.q.length);bone.position.toArray(c.p,c.p.length);}
}
const tracks=[];for(const [n,c] of channels){tracks.push(new T.QuaternionKeyframeTrack(n+'.quaternion',times,c.q),new T.VectorKeyframeTrack(n+'.position',times,c.p));}
const clip=new T.AnimationClip(source.id,source.duration,tracks);if(!clip.validate())throw new Error('Invalid clip');
const asset={schemaVersion:1,id:source.id,source:source.source,license:source.license,rigSha256:source.rigSha256,sampleRate:60,duration:source.duration,definition:source,rigSignature:animated.map(n=>({name:n,parent:bones.get(n).parent.name,position:rest.get(n).p.toArray(),quaternion:rest.get(n).q.toArray()})),clip:T.AnimationClip.toJSON(clip)};
// Three.js UUIDs are runtime identifiers; exclude them for byte-for-byte reproducible authored assets.
delete asset.clip.uuid;
const output=new URL('assets/motion/standing-alternating-knee-raise.clip.json',base),serialized=JSON.stringify(asset);
if(process.argv.includes('--check')){if(await readFile(output,'utf8')!==serialized)throw new Error('Baked clip is stale; run node tools/motion/bake-knee-raise.mjs');}else await writeFile(output,serialized);
console.log('Baked '+source.id+': '+times.length+' frames, '+tracks.length+' skeletal tracks, '+source.duration+' seconds, original key-pose source.');
