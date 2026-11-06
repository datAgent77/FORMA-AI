import * as THREE from './vendor/three/three.module.js';
import {GLTFLoader} from './vendor/three/GLTFLoader.js';
// All room geometry is a desktop-only backdrop. Never drawn over real passthrough.
export function createPreviewRoom(){
 const room=new THREE.Group();room.name='Desktop example room — not passthrough';
 const mat=(color,roughness=.85)=>new THREE.MeshStandardMaterial({color,roughness});
 const wood=mat(0xb69b79),wall=mat(0xe0d9cb),dark=mat(0x343b3c),fabric=mat(0xa5a7a0),glass=mat(0xb1ccd2),green=mat(0x69836b);
 function box(w,h,d,x,y,z,m){const o=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),m);o.position.set(x,y,z);o.receiveShadow=true;room.add(o);return o;}
 box(12,.08,12,0,-.06,-1,wood);
 for(let x=-6;x<6;x+=.28){box(.012,.004,12,x,-.014,-1,mat(0xa68b6d));for(let z=-6;z<5;z+=2.1)box(.27,.004,.008,x+.135,-.012,z+(Math.round(x/.28)%2)*.9,mat(0xa68b6d));}
 box(12,.5,.12,0,3.15,-4.5,wall);box(3.8,2.9,.12,-4.1,1.45,-4.5,wall);box(3.8,2.9,.12,4.1,1.45,-4.5,wall);box(.12,3.4,12,4,1.7,-1,wall);box(12,.1,12,0,3.4,-1,mat(0xd2bca0));
 for(let x=-5.8;x<6;x+=.38)box(.012,.015,12,x,3.34,-1,mat(0xbda686));
 // Broad glazed wall and a sunlit garden beyond it, intentionally architectural rather than a grid.
 box(.12,3.3,10,-3.6,1.65,-1,glass);
 for(let z=-4.5;z<4;z+=1.65){box(.09,3.4,.07,-3.48,1.7,z,dark);box(.07,.07,1.65,-3.48,.22,z+.825,dark);}
 box(.08,.07,10,-3.48,2.95,-1,dark);
 for(let z=-4;z<4;z+=1.2){const crown=new THREE.Mesh(new THREE.SphereGeometry(.55,12,8),green);crown.position.set(-3.35,.75,z);room.add(crown);}
 // Garden-facing doors behind the coach.
 for(const x of [-1.7,0,1.7])box(.075,2.8,.14,x,1.4,-4.35,dark);
 box(3.5,.08,.14,0,2.78,-4.35,dark);box(3.5,.05,.14,0,.08,-4.35,dark);
 const windowMat=new THREE.MeshStandardMaterial({color:0xc6e2df,transparent:true,opacity:.12,roughness:.1,depthWrite:false});
 for(const x of [-.85,.85])box(1.6,2.65,.035,x,1.4,-4.4,windowMat);
 box(20,.1,12,0,-.15,-10,mat(0x738873));
 box(5,.05,2.5,0,-.05,-5.9,mat(0xd4c4a8));
 for(const [x,z] of [[-2.5,-6],[2.5,-7],[-4,-8],[4,-9]]){
  box(.18,2,.18,x,.85,z,mat(0x76654c));
  const tree=new THREE.Mesh(new THREE.IcosahedronGeometry(1.3,1),mat(0x65816b));tree.position.set(x,2.5,z);room.add(tree);
 }
 // Recessed ceiling lights and a warm wall panel are original geometry.
 for(const x of [-2,2])for(const z of [-2,1])box(.6,.025,.22,x,3.32,z,new THREE.MeshStandardMaterial({color:0xfff1cd,emissive:0xffe0a0,emissiveIntensity:2}));
 // Sofa remains well behind the virtual coach in this preview.
 box(1.8,.36,.85,2.7,.35,-2.8,fabric);box(1.8,.52,.18,2.7,.73,-3.18,fabric);
 for(const x of [1.87,3.53])box(.16,.44,.85,x,.57,-2.8,fabric);
 for(const x of [2.25,3.12])box(.8,.12,.7,x,.58,-2.74,mat(0xbcbdb4));
 box(1.1,.045,.65,2.5,.36,-1.45,mat(0x645c50));for(const x of [2.05,2.95])box(.045,.34,.5,x,.17,-1.45,dark);
 box(2.15,.014,1.5,0,-.008,-1.25,mat(0x838a82));
 return room;
}
export async function createHumanCoach(parent,onStatus){
 const gltf=await new GLTFLoader().loadAsync('/assets/coach/woman-casual.glb');
 const avatar=gltf.scene;avatar.name='Temporary rigged human coach — Quaternius';
 // Use a standing pose; no unrelated stock dance/fight animations are played.
 const clip=gltf.animations.find(a=>a.name.endsWith('Female_Idle'))||gltf.animations.find(a=>a.name.endsWith('Female_Standing'));
 const mixer=new THREE.AnimationMixer(avatar);if(clip){const action=mixer.clipAction(clip);action.play();mixer.update(.01);action.paused=true;}
 avatar.updateMatrixWorld(true);const bounds=new THREE.Box3().setFromObject(avatar),height=bounds.max.y-bounds.min.y;
 avatar.scale.multiplyScalar(1.75/height);avatar.updateMatrixWorld(true);const sized=new THREE.Box3().setFromObject(avatar);
 avatar.position.y-=sized.min.y;avatar.position.x-=(sized.max.x+sized.min.x)/2;
 const rest=new Map();avatar.traverse(o=>{if(o.isBone)rest.set(o.name,{bone:o,q:o.quaternion.clone()});if(o.isMesh){o.castShadow=true;o.frustumCulled=false;for(const m of (Array.isArray(o.material)?o.material:[o.material])){m.roughness=.85;if(m.name==='Shirt')m.color.set(0x26343a);if(m.name==='Pants')m.color.set(0x25292d);}}});
 parent.add(avatar);onStatus('3B koç hazır · Geçici stilize model');
 const offset=(name,axis,angle)=>{const entry=rest.get(name);if(entry)entry.bone.quaternion.copy(entry.q).multiply(new THREE.Quaternion().setFromAxisAngle(axis,angle));};
 const x=new THREE.Vector3(1,0,0),z=new THREE.Vector3(0,0,1);
 return {avatar,height:1.75,update(phase,moving,exercise,reduced,dt=.016){
  const previous=new Map([...rest].map(([name,{bone}])=>[name,bone.quaternion.clone()]));
  for(const {bone,q} of rest.values())bone.quaternion.copy(q);
  if(moving&&!reduced){
  const swing=Math.sin(phase);
  if(exercise==='march'){
   offset('UpperLeg.L',x,Math.max(0,swing)*.28);offset('UpperLeg.R',x,Math.max(0,-swing)*.28);
   offset('LowerLeg.L',x,-Math.max(0,swing)*.18);offset('LowerLeg.R',x,-Math.max(0,-swing)*.18);
   offset('UpperArm.L',x,-swing*.12);offset('UpperArm.R',x,swing*.12);
  }else{offset('Shoulder.L',z,swing*.09);offset('Shoulder.R',z,-swing*.09);offset('UpperArm.L',x,Math.cos(phase)*.07);offset('UpperArm.R',x,Math.cos(phase)*.07);}
 }
  const blend=1-Math.exp(-Math.max(0,dt)*10);
  for(const [name,{bone}] of rest)bone.quaternion.copy(previous.get(name).slerp(bone.quaternion.clone(),blend));
 }};
}

export function createEnvironment(id='studio'){
 if(id!=='studio')throw new Error('Environment asset not available: '+id);
 return {id,root:createPreviewRoom(),kind:'original-3d-geometry',provenance:'original project geometry; no third-party room assets'};
}
