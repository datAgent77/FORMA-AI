// Playback is presentation only. Workout state/time always comes from C#.
export function shouldPlayCoach({offline=false,visible=true,reducedMotion=false,previewPaused=false,localPaused=true,state,exerciseId}){
 if(offline||!visible||reducedMotion||previewPaused)return false;
 if(!state||state==='Ready')return true;
 return !localPaused && (state==='Work'||state==='Introduction') && exerciseId==='march';
}
