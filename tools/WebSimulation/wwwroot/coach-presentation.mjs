import {coachPose} from './coach-motion.mjs';
export const CoachPresentationStates=Object.freeze(['IDLE','GREETING','DEMONSTRATING','EXERCISING','RESTING','CELEBRATING']);
export function coachPresentation(model,options){
 const w=model?.workout,pose=coachPose(w,options),supported=['march','shoulders','knees','mobility'].includes(w?.exerciseId);
 let state='IDLE';
 if(w?.state==='Completed')state='CELEBRATING';
 else if(w?.state==='Rest'||w?.state==='Paused')state='RESTING';
 else if(w?.state==='Work')state='EXERCISING';
 else if(w?.state==='Introduction')state=supported?'DEMONSTRATING':'GREETING';
 else if(model?.athlete?.profile)state='GREETING';
 if(state==='DEMONSTRATING'){
  pose.moving=supported&&!options.offline&&!options.localPaused&&options.visible&&!options.reduced;
  pose.phase=Math.max(0,(w.intervalSeconds||0)-(w.remaining||0))*Math.PI;
 }
 return {...pose,state,instructionalFallback:!!w&&(w.state==='Work'||w.state==='Introduction')&&!supported};
}
