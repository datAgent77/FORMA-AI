import * as THREE from './vendor/three/three.module.js';
import {createHolographicEnvironment} from './holographic-studio.mjs';
import {createForestEnvironment} from './forest-environment.mjs';
export function createEnvironmentManager(){
 const root=new THREE.Group(),cache=new Map();let current=null;
 return {root,get background(){return current?.background??0x080f21;},get fogDensity(){return current?.fogDensity??.035;},select(id){
  if(id!=='HolographicStudio'&&id!=='Forest')throw new Error('Environment is not available.');
  if(current?.id===id)return false;
  if(!cache.has(id)){const e=id==='Forest'?createForestEnvironment():{...createHolographicEnvironment(),id:'HolographicStudio',background:0x080f21,fogDensity:.035};e.root.visible=false;cache.set(id,e);root.add(e.root);}
  if(current)current.root.visible=false;current=cache.get(id);current.root.visible=true;return true;
 }};
}
