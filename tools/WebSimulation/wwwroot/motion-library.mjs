import * as T from './vendor/three/three.module.js';
export const KNEE_EXERCISE='standing_alternating_knee_raise';
export function inspectCoachRig(avatar){
 const bones=new Map();let meshes=0,weightedVertices=0;
 avatar.traverse(o=>{if(o.isBone)bones.set(o.name,o);if(o.isSkinnedMesh){meshes++;if(!o.skeleton||!o.geometry.attributes.skinWeight||!o.geometry.attributes.skinIndex)throw new Error('Invalid skinned mesh');weightedVertices+=o.geometry.attributes.skinWeight.count;}});
 const required=['Body','Hips','Abdomen','Torso','Neck','Head','ShoulderL','ShoulderR','UpperArmL','UpperArmR','LowerArmL','LowerArmR','PalmL','PalmR','UpperLegL','UpperLegR','LowerLegL','LowerLegR','FootL','FootR'];
 if(!meshes||!weightedVertices)throw new Error('A weighted skinned coach is required');for(const n of required)if(!bones.has(n))throw new Error('Missing joint '+n);
 for(const side of ['L','R']){if(bones.get('LowerLeg'+side).parent!==bones.get('UpperLeg'+side)||bones.get('LowerArm'+side).parent!==bones.get('UpperArm'+side))throw new Error('Unsupported humanoid hierarchy');}
 return {bones,meshes,weightedVertices};
}
export function validateMotionAsset(asset){
 if(asset?.schemaVersion!==1||asset.id!==KNEE_EXERCISE||!(asset.duration>=6&&asset.duration<=20)||!asset.source||asset.license!=='LicenseRef-FORMA-Project-Owned'||!Array.isArray(asset.rigSignature))throw new Error('Unsupported motion asset');
 const d=asset.definition;if(!d||!Array.isArray(d.phases)||!Array.isArray(d.events)||!d.cues)throw new Error('Missing motion definition');let last=0;
 for(const p of d.phases){if(p.start!==last||!(p.end>p.start)||!['left','right'].includes(p.side)||!Array.isArray(p.contacts)||!p.contacts.length)throw new Error('Invalid phase sequence');last=p.end;}if(last!==asset.duration||d.duration!==asset.duration)throw new Error('Incomplete phase coverage');
 const order=['prepare','lift','peak','lower','recover'];if(d.phases.length!==10||d.phases.some((p,i)=>p.name!==order[i%5]||p.side!==(i<5?'right':'left')||p.contacts.some(s=>s!=='left'&&s!=='right')||!p.contacts.includes(p.side==='left'?'right':'left')))throw new Error('Invalid bilateral contact phases');
 last=-1;for(const e of d.events){if(!Number.isFinite(e.time)||e.time<last||e.time<0||e.time>asset.duration||!['left','right'].includes(e.side)||e.cue&&!d.cues[e.cue])throw new Error('Invalid motion event');last=e.time;}
 for(const type of ['rep_start','knee_peak','foot_contact','rep_complete'])if(d.events.filter(e=>e.type===type).map(e=>e.side).join(',')!=='right,left')throw new Error('Missing bilateral motion events');
 const c=asset.clip;if(c?.name!==KNEE_EXERCISE||c.duration!==asset.duration||!Array.isArray(c.tracks)||c.tracks.length<30)throw new Error('Missing skeletal clip');
 for(const t of c.tracks){if(!/^[A-Za-z0-9]+\.(quaternion|position)$/.test(t.name)||!Array.isArray(t.times)||!Array.isArray(t.values)||t.times.length<2||t.times[0]!==0||t.times.at(-1)!==asset.duration||t.times.some((x,i)=>!Number.isFinite(x)||i>0&&x<=t.times[i-1])||t.values.some(x=>!Number.isFinite(x)))throw new Error('Invalid animation track');const n=t.name.endsWith('quaternion')?4:3;if(t.values.length!==t.times.length*n)throw new Error('Invalid track size');if(n===4)for(let i=0;i<t.values.length;i+=4)if(Math.abs(Math.hypot(...t.values.slice(i,i+4))-1)>.001)throw new Error('Invalid rotation');}
 return asset;
}
export function bindMotionAsset(avatar,asset){
 validateMotionAsset(asset);const {bones}=inspectCoachRig(avatar);
 for(const s of asset.rigSignature){const b=bones.get(s.name);if(!b||b.parent.name!==s.parent||b.position.distanceTo(new T.Vector3(...s.position))>.00002||1-Math.abs(b.quaternion.dot(new T.Quaternion(...s.quaternion)))>.00002)throw new Error('Motion rig/rest-pose mismatch: '+s.name);}
 const clip=T.AnimationClip.parse(asset.clip);if(!clip.validate())throw new Error('Animation validation failed');for(const t of clip.tracks){const [name,property]=t.name.split('.');if(!bones.has(name))throw new Error('Unbound track '+name);t.name=bones.get(name).uuid+'.'+property;}
 return {clip,definition:asset.definition,source:asset.source,bones};
}
export async function loadMotionAsset(fetcher=globalThis.fetch){const response=await fetcher('/assets/motion/standing-alternating-knee-raise.clip.json');if(!response.ok)throw new Error('Motion asset unavailable');return validateMotionAsset(await response.json());}
