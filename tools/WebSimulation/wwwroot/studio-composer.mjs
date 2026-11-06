import * as THREE from './vendor/three/three.module.js';
// Small desktop-only bloom pass. Immersive XR always uses the direct renderer.
export function createStudioComposer(renderer){
 const options={type:renderer.extensions.has('EXT_color_buffer_float')?THREE.HalfFloatType:THREE.UnsignedByteType,minFilter:THREE.LinearFilter,magFilter:THREE.LinearFilter,depthBuffer:false};
 const image=new THREE.WebGLRenderTarget(1,1,{...options,depthBuffer:true});
 const horizontal=new THREE.WebGLRenderTarget(1,1,options),vertical=new THREE.WebGLRenderTarget(1,1,options);
 const camera=new THREE.OrthographicCamera(-1,1,1,-1,0,1),scene=new THREE.Scene();
 const vertexShader='varying vec2 vUv;void main(){vUv=uv;gl_Position=vec4(position.xy,0.,1.);}';
 const blur=new THREE.ShaderMaterial({depthTest:false,depthWrite:false,toneMapped:false,uniforms:{source:{value:null},stepSize:{value:new THREE.Vector2()},extract:{value:true}},vertexShader,fragmentShader:`
 varying vec2 vUv;uniform sampler2D source;uniform vec2 stepSize;uniform bool extract;
 vec3 sampleLight(vec2 uv){vec3 c=texture2D(source,uv).rgb;if(extract){float l=max(c.r,max(c.g,c.b));c*=max(l-.7,0.)/max(l,.0001);}return c;}
 void main(){vec3 c=sampleLight(vUv)*.227027;c+=sampleLight(vUv+stepSize*1.384615)*.316216;c+=sampleLight(vUv-stepSize*1.384615)*.316216;c+=sampleLight(vUv+stepSize*3.230769)*.070270;c+=sampleLight(vUv-stepSize*3.230769)*.070270;gl_FragColor=vec4(c,1.);}`});
 const combine=new THREE.ShaderMaterial({depthTest:false,depthWrite:false,uniforms:{source:{value:image.texture},bloom:{value:vertical.texture}},vertexShader,fragmentShader:`
 varying vec2 vUv;uniform sampler2D source;uniform sampler2D bloom;
 void main(){vec3 c=texture2D(source,vUv).rgb+texture2D(bloom,vUv).rgb*.7;gl_FragColor=vec4(c,1.);
 #include <tonemapping_fragment>
 #include <colorspace_fragment>
 }`});
 const quad=new THREE.Mesh(new THREE.PlaneGeometry(2,2),blur);scene.add(quad);let width=0,height=0;
 return {render(world,view){
  const size=renderer.getDrawingBufferSize(new THREE.Vector2());
  if(size.x!==width||size.y!==height){width=size.x;height=size.y;image.setSize(width,height);horizontal.setSize(Math.max(1,width>>1),Math.max(1,height>>1));vertical.setSize(Math.max(1,width>>1),Math.max(1,height>>1));}
  renderer.setRenderTarget(image);renderer.render(world,view);
  quad.material=blur;blur.uniforms.source.value=image.texture;blur.uniforms.extract.value=true;blur.uniforms.stepSize.value.set(3/width,0);renderer.setRenderTarget(horizontal);renderer.render(scene,camera);
  blur.uniforms.source.value=horizontal.texture;blur.uniforms.extract.value=false;blur.uniforms.stepSize.value.set(0,3/height);renderer.setRenderTarget(vertical);renderer.render(scene,camera);
  quad.material=combine;renderer.setRenderTarget(null);renderer.render(scene,camera);
 },dispose(){image.dispose();horizontal.dispose();vertical.dispose();blur.dispose();combine.dispose();quad.geometry.dispose();}};
}
