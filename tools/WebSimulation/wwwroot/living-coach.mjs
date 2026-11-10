const animationNames={march:'march',shoulders:'shoulder_circles',knees:'knee_raise',mobility:'mobility'};
export function livingCoach(model,{offline=false,localPaused=false,visible=true,reduced=false}={}){
 const w=model?.workout,s=model?.personal?.session;
 if(w?.workoutId!=='forma-start-5-v1')return null;
 const elapsed=Math.max(0,(w.intervalSeconds||0)-(w.remaining||0)),freeze=offline||localPaused||!visible;
 let state='idle',animation='idle',seconds=elapsed;
 if(offline||!visible){state='paused';animation='rest';}
 else if(s?.awaitingFeedback){state='resting';animation='rest';}
 else if(w.state==='Paused'){state='paused';animation='rest';}
 else if(w.state==='Completed'){state='completed';animation='celebrate';seconds=0;}
 else if(w.state==='Aborted'){state='completed';animation='rest';}
 else if(w.state==='Introduction'){state='welcome';animation='welcome';}
 else if(w.state==='Rest'){state='resting';animation='rest';}
 else if(w.state==='Work'&&w.stage==='Cooldown'){state='cooldown';animation='cooldown';}
 else if(w.state==='Work'){
  state=elapsed<(w.demonstrationSeconds??8)?'demonstrating':'exercising';animation=(state==='demonstrating'?'demonstrate_':'')+animationNames[w.exerciseId]+(state==='exercising'?'_loop':'');
 }
 if(reduced)animation='rest';
 return {state,animation,seconds,freeze:freeze&&state!=='resting'&&state!=='completed',exercise:w.exerciseId,reduced};
}
export function trackingCapabilities(frame,session,referenceSpace){
 const result={headTracking:false,leftHandTracking:false,rightHandTracking:false,fullBodyTracking:false,heartRateAvailable:false};
 if(!frame||!session||!referenceSpace)return result;
 try{result.headTracking=!!frame.getViewerPose(referenceSpace);for(const source of session.inputSources||[]){if(!source.hand||!['left','right'].includes(source.handedness))continue;const wrist=source.hand.get('wrist');if(wrist&&frame.getJointPose?.(wrist,referenceSpace))result[source.handedness+'HandTracking']=true;}}catch{/* loss of tracking stays unavailable */}
 return result;
}
