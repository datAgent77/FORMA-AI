import * as THREE from './vendor/three/three.module.js';
// Original instanced geometry, no remote assets or background photographs.
export function createForestEnvironment(){
 const root=new THREE.Group();root.name='FORMA forest clearing — original 3D';
 const groundGeo=new THREE.PlaneGeometry(90,90,40,40);groundGeo.rotateX(-Math.PI/2);const p=groundGeo.attributes.position;
 for(let i=0;i<p.count;i++){const x=p.getX(i),z=p.getZ(i),r=Math.hypot(x,z+1.2);p.setY(i,r<4?-.08:Math.sin(x*.24)*Math.cos(z*.19)*.45-.09);}groundGeo.computeVertexNormals();
 const ground=new THREE.Mesh(groundGeo,new THREE.MeshStandardMaterial({color:0x26403a,roughness:.95}));root.add(ground);
 let seed=417;const random=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
 const trunks=new THREE.InstancedMesh(new THREE.CylinderGeometry(.08,.16,1,7),new THREE.MeshStandardMaterial({color:0x333d36,roughness:1}),100);
 const crowns=new THREE.InstancedMesh(new THREE.ConeGeometry(1,1,8),new THREE.MeshStandardMaterial({color:0x28534d,roughness:1}),300);
 const object=new THREE.Object3D();
 for(let i=0;i<100;i++){
  const angle=random()*Math.PI*2,radius=6+random()*32,x=Math.cos(angle)*radius,z=Math.sin(angle)*radius-1.2,height=3+random()*5;
  object.position.set(x,height*.5,z);object.scale.set(1,height,1);object.rotation.set(0,random()*Math.PI,0);object.updateMatrix();trunks.setMatrixAt(i,object.matrix);
  for(let tier=0;tier<3;tier++){const size=(1.8-tier*.35)*(height/5);object.position.set(x,height*.55+tier*height*.18,z);object.scale.set(size,height*.6,size);object.updateMatrix();crowns.setMatrixAt(i*3+tier,object.matrix);}
 }
 root.add(trunks,crowns);
 const rocks=new THREE.InstancedMesh(new THREE.IcosahedronGeometry(1,1),new THREE.MeshStandardMaterial({color:0x51666a,roughness:.85}),45);
 for(let i=0;i<45;i++){const a=random()*Math.PI*2,r=4.4+random()*25;object.position.set(Math.cos(a)*r,.12,Math.sin(a)*r-1.2);object.scale.set(.2+random()*.6,.15+random()*.4,.25+random()*.5);object.rotation.set(random(),random(),random());object.updateMatrix();rocks.setMatrixAt(i,object.matrix);}root.add(rocks);
 const light=new THREE.PointLight(0x9ed8d7,18,30,2);light.position.set(-4,5,1);root.add(light);
 return {id:'Forest',root,background:0x1a363e,fogDensity:.035,provenance:'Original project geometry; instanced trees and rocks'};
}
