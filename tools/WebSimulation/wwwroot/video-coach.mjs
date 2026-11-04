import * as THREE from './vendor/three/three.module.js';
// RGB and alpha share one H.264 frame, avoiding dual-video synchronization and alpha-codec differences.
export async function createVideoCoach(parent,onStatus){
 const metadata=await fetch('/assets/studio/coach-video.json').then(r=>{if(!r.ok)throw new Error('Coach metadata unavailable');return r.json();});
 const video=document.createElement('video');video.id='coach-video';video.muted=true;video.loop=true;video.playsInline=true;video.preload='auto';video.setAttribute('playsinline','');video.setAttribute('aria-label','Kayıtlı insan koç videosu');video.hidden=true;video.src='/assets/studio/coach-packed.mp4';document.body.append(video);
 const texture=new THREE.VideoTexture(video);texture.minFilter=THREE.LinearFilter;texture.magFilter=THREE.LinearFilter;texture.colorSpace=THREE.NoColorSpace;
 const material=new THREE.ShaderMaterial({uniforms:{map:{value:texture},diagram:{value:false}},transparent:true,depthWrite:false,side:THREE.DoubleSide,
 vertexShader:'varying vec2 vUv; void main(){vUv=uv;gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.0);}',
 fragmentShader:`uniform sampler2D map;uniform bool diagram;varying vec2 vUv;void main(){vec3 rgb=texture2D(map,vec2(vUv.x*.5,vUv.y)).rgb;float a=texture2D(map,vec2(.5+vUv.x*.5,vUv.y)).r;a=smoothstep(.04,.98,a);if(a<.025)discard;if(diagram){rgb=mix(vec3(.045),vec3(.95,.78,.22),step(.68,vUv.y)*step(vUv.y,.8));}gl_FragColor=vec4(rgb,a);}`});
 const height=1.75*metadata.height/(metadata.bodyPixelHeight||metadata.height),mesh=new THREE.Mesh(new THREE.PlaneGeometry(height*metadata.width/metadata.height,height),material);mesh.position.y=height/2-height*(metadata.footPaddingPixels||0)/metadata.height;mesh.renderOrder=2;parent.add(mesh);
 const diagram=new THREE.Mesh(new THREE.PlaneGeometry(.56*metadata.width/metadata.height,.56),material.clone());diagram.material.uniforms.diagram.value=true;diagram.renderOrder=3;
 let wanted=null;
 video.addEventListener('error',()=>onStatus('Koç videosu açılamadı. Yeniden yükle.'));
 await new Promise((resolve,reject)=>{video.addEventListener('loadeddata',resolve,{once:true});video.addEventListener('error',reject,{once:true});video.load();});
 onStatus('Gerçek insan videosu · Görsel önizleme');
 return {video,diagram,setPlaying(playing){if(wanted===playing)return;wanted=playing;if(playing)video.play().catch(()=>{wanted=null;onStatus('Videoyu başlatmak için Oynat’a dokun.');});else video.pause();},pause(){wanted=false;video.pause();}};
}
