// Animation phase derives from authoritative C# interval time, never frame count.
export function coachPose(workout,{offline=false,localPaused=true,visible=true,reduced=false}={}){
 const moving=!!workout && workout.state==='Work' && !offline && !localPaused && visible && !reduced;
 const elapsed=Math.max(0,(workout?.intervalSeconds||0)-(workout?.remaining||0));
 return {moving,phase:elapsed*Math.PI,exercise:workout?.exerciseId,reduced};
}
