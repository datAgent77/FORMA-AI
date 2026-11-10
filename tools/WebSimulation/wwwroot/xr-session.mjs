// These guards never manufacture an XR session and never fall back to opaque VR.
export async function inspectAR(xr,secure){
 if(!secure)return {supported:false,reason:'WebXR için güvenli bağlam gerekiyor. Quest üzerinde ADB reverse ile localhost kullan.'};
 if(!xr)return {supported:false,reason:'Bu tarayıcı WebXR sunmuyor. 3B masaüstü önizlemesi açık; headset oturumu yok.'};
 try{return await xr.isSessionSupported('immersive-ar')?{supported:true,reason:'WebXR AR destekleniyor. Açık alanı onaylayıp Quest’te başlatabilirsin.'}:{supported:false,reason:'Bu tarayıcıda immersive-ar kullanılamıyor. Quest Browser üzerinde aç.'};}
 catch{return {supported:false,reason:'WebXR AR desteği doğrulanamadı. Tarayıcı ve headset bağlantısını kontrol et.'};}
}
export function requestMR(xr,{secure,confirmed,noEquipment}){
 if(!secure || !xr || !confirmed || !noEquipment)return Promise.reject(new Error('Açık alan, ekipmansız kullanım ve güvenli WebXR bağlamı gerekli.'));
 // Must be invoked directly inside the user gesture, before unrelated asynchronous work.
 return xr.requestSession('immersive-ar',{requiredFeatures:['local-floor']});
}
export function isTransparentSession(session){return session?.environmentBlendMode==='alpha-blend'||session?.environmentBlendMode==='additive';}
// An explicit equipment-free Worlds action. Never called as an AR fallback.
export async function inspectVR(xr,secure){
 if(!secure||!xr)return {supported:false};
 try{return {supported:await xr.isSessionSupported('immersive-vr')};}catch{return {supported:false};}
}
export function requestWorldVR(xr,{secure,confirmed,noEquipment,sessionActive=false}){
 if(!secure||!xr||!confirmed||!noEquipment||sessionActive)return Promise.reject(new Error('Explicit equipment-free consent and no active workout are required.'));
 return xr.requestSession('immersive-vr',{requiredFeatures:['local-floor']});
}
