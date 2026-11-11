import * as THREE from './vendor/three/three.module.js';

const cyan=0x69f4ff,ice=0xc1e6ff;
const basic=(color,opacity=1)=>new THREE.MeshBasicMaterial({color,transparent:opacity<1,opacity,depthWrite:opacity===1});
function beam(parent,points,color=cyan,radius=.008){
 const curve=new THREE.CatmullRomCurve3(points.map(p=>new THREE.Vector3(...p)));
 const geometry=new THREE.TubeGeometry(curve,Math.max(8,points.length*6),radius,6,false);
 const core=new THREE.Mesh(geometry,basic(color));parent.add(core);
 const halo=new THREE.Mesh(new THREE.TubeGeometry(curve,Math.max(8,points.length*6),radius*3.5,6,false),new THREE.MeshBasicMaterial({color,transparent:true,opacity:.10,blending:THREE.AdditiveBlending,depthWrite:false}));parent.add(halo);
 return core;
}
function ring(parent,r,y,color=cyan,opacity=1){const m=new THREE.Mesh(new THREE.RingGeometry(r-.006,r+.006,96),basic(color,opacity));m.rotation.x=-Math.PI/2;m.position.y=y;parent.add(m);return m;}
function box(parent,size,pos,material){const m=new THREE.Mesh(new THREE.BoxGeometry(...size),material);m.position.set(...pos);parent.add(m);return m;}
function glow(parent,x,y,z,size,color){
 const canvas=document.createElement('canvas');canvas.width=128;canvas.height=128;const ctx=canvas.getContext('2d'),g=ctx.createRadialGradient(64,64,0,64,64,64);g.addColorStop(0,'#ffffffa0');g.addColorStop(.2,'#ffffff40');g.addColorStop(1,'#ffffff00');ctx.fillStyle=g;ctx.fillRect(0,0,128,128);
 const m=new THREE.Mesh(new THREE.PlaneGeometry(size,size),new THREE.MeshBasicMaterial({map:new THREE.CanvasTexture(canvas),color,transparent:true,blending:THREE.AdditiveBlending,depthWrite:false,side:THREE.DoubleSide}));m.rotation.x=-Math.PI/2;m.position.set(x,y,z);parent.add(m);
}

// Original 3D architecture. No room photos, videos, fake athletes or telemetry.
export function createHolographicEnvironment(){
 const root=new THREE.Group();root.name='FORMA luminous studio — desktop 3D architecture';
 const floor=new THREE.MeshStandardMaterial({color:0x233751,metalness:.32,roughness:.38});
 box(root,[24,.12,24],[0,-.10,-2],floor);
 const wall=new THREE.MeshStandardMaterial({color:0x111b31,metalness:.35,roughness:.65});
 box(root,[20,5,.16],[0,2.4,-6],wall);box(root,[.15,5,18],[-7,2.4,-1],wall);box(root,[.15,5,18],[7,2.4,-1],wall);
 box(root,[20,.15,18],[0,4.5,-2],new THREE.MeshStandardMaterial({color:0x0b1427,roughness:.8}));
 // Floor tile joints and architectural wall reveals give depth when the camera moves.
 for(let x=-10;x<=10;x+=2)box(root,[.012,.003,22],[x,-.034,-2],basic(0x080f20));
 for(let z=-10;z<=8;z+=2)box(root,[22,.003,.012],[0,-.034,z],basic(0x080f20));
 for(let x=-6;x<=6;x+=2){
  box(root,[.06,4,.12],[x,2,-5.85],new THREE.MeshStandardMaterial({color:0x26354b,metalness:.8,roughness:.2}));
  beam(root,[[x,0,-5.75],[x,3.25,-5.75],[x+.12,3.5,-5.75]],ice,.009);
 }
 for(const z of [-5,-3.5,-2])beam(root,[[-6,3.15,z],[6,3.15,z]],ice,.024);
 for(const x of [-5,-2,1,4])beam(root,[[x,4.37,4],[x,4.37,-5.5]],ice,.025);
 for(const z of [-4,-1,2])beam(root,[[-6.9,3.1,z],[-6.9,3.6,z-.5],[-6.9,4.2,z-.5]],ice,.018);
 for(const x of [-4,4]){
  beam(root,[[x,.005,-5],[x,.005,4]],0x4679be,.007);
  glow(root,x,.003,-3,3,0x247fdd);
 }
 const fill=new THREE.PointLight(0x537eff,18,16,2);fill.position.set(-3,2.5,-3);root.add(fill);
 const key=new THREE.PointLight(0x70edff,13,12,2);key.position.set(3,3,1);root.add(key);
 return {id:'holographic-studio',root};
}

// Stage is attached to coach placement, including during a transparent AR session.
export function createHolographicStage(){
 const root=new THREE.Group();root.name='Holographic training bay';
 const platform=new THREE.Mesh(new THREE.CylinderGeometry(1.65,1.72,.065,96),new THREE.MeshStandardMaterial({color:0x0a2134,metalness:.8,roughness:.23}));platform.position.set(0,-.0325,0);root.add(platform);
 ring(root,1.62,.022,ice);ring(root,1.54,.024,cyan,.4);ring(root,.58,.026,cyan,.8);
 glow(root,-.43,.029,0,2.8,0x14d6ff);glow(root,.7,.03,-.2,2,0x5055ff);
 // Thin projected practice lane; no real-space boundary guarantee.
 const lane=[[-1.27,.028,.52],[-1.27,.028,1.85],[1.27,.028,1.85],[1.27,.028,.52]];
 beam(root,lane,cyan,.009);
 for(const x of [-.8,-.56,-.32])beam(root,[[x,.03,1.5],[x+.10,.03,1.6],[x,.03,1.7]],ice,.012);
 // Concave glass shell with a physically modelled curve and luminous frame.
 const shell=new THREE.Group();root.add(shell);
 const radius=2.05,angle=.89;
 function point(t,y){return [Math.sin(t)*radius,y,-.46-(Math.cos(t)-Math.cos(angle))*radius];}
 const vertices=[],indices=[];const steps=64;
 for(let i=0;i<=steps;i++){const t=-angle+2*angle*i/steps;vertices.push(...point(t,.11),...point(t,2.46));if(i<steps){const a=i*2;indices.push(a,a+1,a+2,a+1,a+3,a+2);}}
 const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(vertices,3));geo.setIndex(indices);geo.computeVertexNormals();
 shell.add(new THREE.Mesh(geo,new THREE.MeshPhysicalMaterial({color:0x426fba,metalness:.1,roughness:.12,transparent:true,opacity:.13,side:THREE.DoubleSide,depthWrite:false})));
 for(const y of [.11,2.46]){const pts=[];for(let i=0;i<=64;i++)pts.push(point(-angle+2*angle*i/64,y));beam(shell,pts,ice,.011);}
 for(const t of [-angle,angle])beam(shell,[point(t,.11),point(t,2.46)],ice,.011);
 for(const y of [.23,2.32]){const pts=[];for(let i=0;i<=48;i++)pts.push(point(-angle+.08+(2*angle-.16)*i/48,y));beam(shell,pts,cyan,.003);}
 // Quiet panel divisions, not invented health metrics.
 beam(shell,[point(0,.24),point(0,2.28)],0x72b8ed,.002);
 const particles=[];for(let i=0;i<90;i++)particles.push(Math.sin(i*13.17)*1.35,.22+((i*37)%100)/46,-.7-Math.abs(Math.cos(i*6.3))*.65);
 const dots=new THREE.BufferGeometry();dots.setAttribute('position',new THREE.Float32BufferAttribute(particles,3));shell.add(new THREE.Points(dots,new THREE.PointsMaterial({color:0x87ecff,size:.008,transparent:true,opacity:.5,depthWrite:false})));
 root.userData.setLiving=enabled=>{shell.visible=!enabled;};
 root.userData.setImmersive=enabled=>{platform.visible=!enabled;};
 return root;
}

// Skinned mesh materials retain the existing rig. Shader lines are visual only.
export function applyHologram(avatar){
 const meshes=[],obsoleteMaterials=new Set(),obsoleteGeometries=new Set();avatar.traverse(mesh=>{if(mesh.isMesh)meshes.push(mesh);});
 for(const mesh of meshes){
  // Smooth normals across duplicate positions while retaining skin weights and UVs.
  obsoleteGeometries.add(mesh.geometry);for(const material of Array.isArray(mesh.material)?mesh.material:[mesh.material])obsoleteMaterials.add(material);
  const geometry=mesh.geometry.clone();geometry.computeVertexNormals();
  const positions=geometry.attributes.position,normals=geometry.attributes.normal,sums=new Map();
  const key=i=>`${Math.round(positions.getX(i)*1e7)},${Math.round(positions.getY(i)*1e7)},${Math.round(positions.getZ(i)*1e7)}`;
  for(let i=0;i<positions.count;i++){const k=key(i),v=sums.get(k)||new THREE.Vector3();v.add(new THREE.Vector3(normals.getX(i),normals.getY(i),normals.getZ(i)));sums.set(k,v);}
  for(let i=0;i<positions.count;i++){const v=sums.get(key(i)).clone().normalize();normals.setXYZ(i,v.x,v.y,v.z);}mesh.geometry=geometry;
  const m=new THREE.MeshPhongMaterial({color:0x285b91,emissive:0x123862,emissiveIntensity:.35,specular:0x377da1,shininess:35,transparent:true,opacity:.85,side:THREE.DoubleSide});
  m.onBeforeCompile=shader=>{
   shader.vertexShader='varying vec3 vHoloPosition;\n'+shader.vertexShader;
   shader.vertexShader=shader.vertexShader.replace('#include <project_vertex>','#include <project_vertex>\n vHoloPosition=(modelMatrix*vec4(transformed,1.0)).xyz;');
   shader.fragmentShader='varying vec3 vHoloPosition;\n'+shader.fragmentShader;
   shader.fragmentShader=shader.fragmentShader.replace('#include <dithering_fragment>',`#include <dithering_fragment>
    float scan=pow(.5+.5*sin(vHoloPosition.y*160.0),24.0);
    float circuit=pow(.5+.5*sin(vHoloPosition.x*78.0+sin(vHoloPosition.y*28.0)*2.0),28.0);
    float rim=pow(1.0-abs(dot(normalize(normal),normalize(vViewPosition))),2.4);
    gl_FragColor.rgb += vec3(.12,.85,1.0)*(scan*.15+circuit*.04+rim*.45);
    gl_FragColor.a=.85+rim*.1+scan*.02;
   `);
  };
  m.customProgramCacheKey=()=> 'forma-hologram-v2';mesh.material=m;
  const wire=mesh.clone(false);wire.geometry=geometry;wire.material=new THREE.MeshBasicMaterial({color:0x57dfff,wireframe:true,transparent:true,opacity:.015,depthWrite:false,blending:THREE.AdditiveBlending});
  if(mesh.isSkinnedMesh)wire.bind(mesh.skeleton,mesh.bindMatrix);wire.name='Holographic rig surface';mesh.parent.add(wire);
 }
 for(const geometry of obsoleteGeometries)geometry.dispose();for(const material of obsoleteMaterials)material.dispose();
}
