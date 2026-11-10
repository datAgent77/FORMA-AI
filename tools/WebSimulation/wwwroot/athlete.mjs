import {athleteCopy} from './athlete-copy.mjs';
import {weightToDisplay,heightToDisplay,weightFromDisplay,heightFromDisplay,movementMask} from './athlete-units.mjs';
const el=id=>document.getElementById(id),form=el('athlete-form'),panel=el('athlete-panel'),field=name=>form.elements.namedItem(name);
let state=null,language='tr',units='Metric',busy=false;
const copy=()=>athleteCopy[language]||athleteCopy.tr;
function notice(key,error=false){el('athlete-notice').textContent=copy()[key]||copy().network;el('athlete-notice').dataset.error=String(error);}
function labels(){
 const c=copy();panel.querySelectorAll('[data-ath-i18n]').forEach(node=>node.textContent=c[node.dataset.athI18n]||node.textContent);
 el('athlete-open').textContent=c.profile;el('athlete-close').setAttribute('aria-label',c.close);el('athlete-delete-confirm').setAttribute('aria-label',c.deleteTitle);
 el('athlete-height-label').textContent=`${c.height} (${units==='Imperial'?'in':'cm'}) · ${c.optional}`;
 el('athlete-weight-label').textContent=`${c.weight} (${units==='Imperial'?'lb':'kg'}) · ${c.optional}`;
 field('height').min=heightToDisplay(40,units);field('height').max=heightToDisplay(250,units);field('weight').min=weightToDisplay(20,units);field('weight').max=weightToDisplay(400,units);
 for(const option of field('preferredMinutes').options)option.textContent=`${option.value} ${language==='tr'?'dakika':'minutes'}`;
 welcome();
}
function welcome(){const p=state?.profile;el('athlete-welcome').textContent=p?(state.completedSessionCount==null?copy().welcomeUnknown(p.preferences.displayName):copy().welcome(p.preferences.displayName,state.completedSessionCount)):'';}
function lock(){el('athlete-save').disabled=busy||state?.status!=='Ready';el('athlete-delete').disabled=busy||(!state?.profile&&state?.status!=='Unavailable');}
function render(){
 const p=state?.profile,preferences=p?.preferences||{...state?.defaults,units:'Metric',ageRange:'Unspecified',displayName:'',preferredEnvironment:'HolographicStudio',trainingDaysPerWeek:3,equipment:[]};
 language=preferences.language==='en'?'en':'tr';units=preferences.units||'Metric';
 for(const name of ['displayName','language','ageRange','units','primaryGoal','experience','preferredMinutes','preferredEnvironment','trainingDaysPerWeek','voluntaryLimitations'])field(name).value=preferences[name]??(name==='voluntaryLimitations'?'':'');
 if(!field('primaryGoal').value)field('primaryGoal').value='GeneralFitness';
 if(!field('experience').value)field('experience').value='New';
 field('height').value=heightToDisplay(preferences.heightCm,units);field('weight').value=weightToDisplay(preferences.currentWeightKg,units);
 for(const checkbox of form.querySelectorAll('input[name=equipment]'))checkbox.checked=(preferences.equipment||[]).includes(checkbox.value);
 field('avoidLower').checked=!!(movementMask(preferences.movementLimits)&1);field('avoidShoulders').checked=!!(movementMask(preferences.movementLimits)&2);
 el('athlete-identity').textContent=p?`${copy().identity}: ${p.athleteId}\n${copy().created}: ${new Date(p.createdUtc).toLocaleString(language)}\n${copy().updated}: ${new Date(p.updatedUtc).toLocaleString(language)}`:copy().notSaved;
 el('athlete-delete-confirm').value='';labels();lock();if(state?.status!=='Ready')notice('storage',true);else el('athlete-notice').textContent='';
}
async function read(){
 try{const response=await fetch('/api/athlete');if(!response.ok)throw new Error();state=await response.json();render();return true;}
 catch{notice('network',true);el('athlete-save').disabled=true;return false;}
}
async function mutate(request){
 if(busy)return false;busy=true;lock();
 try{const response=await fetch('/api/athlete',{method:'POST',headers:{'Content-Type':'application/json','X-Forma-Simulation':'1'},body:JSON.stringify(request)});const result=await response.json();if(!response.ok){notice(result.code||'invalid',true);return false;}state=result.state;render();document.dispatchEvent(new CustomEvent('forma-athlete-updated'));return true;}
 catch{notice('network',true);return false;}finally{busy=false;lock();}
}
el('athlete-open').onclick=async()=>{panel.hidden=false;notice('loading');await read();};
el('athlete-close').onclick=()=>{panel.hidden=true;};
field('language').onchange=()=>{language=field('language').value;labels();};
field('units').onchange=()=>{const kg=weightFromDisplay(field('weight').value,units),cm=heightFromDisplay(field('height').value,units);units=field('units').value;field('weight').value=weightToDisplay(kg,units);field('height').value=heightToDisplay(cm,units);labels();};
form.onsubmit=async event=>{
 event.preventDefault();const preferences={};for(const name of ['displayName','language','ageRange','units','primaryGoal','experience','preferredEnvironment','voluntaryLimitations'])preferences[name]=field(name).value;
 preferences.preferredMinutes=Number(field('preferredMinutes').value);preferences.trainingDaysPerWeek=Number(field('trainingDaysPerWeek').value);
 preferences.heightCm=heightFromDisplay(field('height').value,units);preferences.currentWeightKg=weightFromDisplay(field('weight').value,units);
 preferences.movementLimits=(field('avoidLower').checked?1:0)|(field('avoidShoulders').checked?2:0);preferences.equipment=[...form.querySelectorAll('input[name=equipment]:checked')].map(x=>x.value);
 if(await mutate({action:'save',profile:preferences,expectedRevision:state?.revision||0}))notice('saved');
};
el('athlete-delete').onclick=async()=>{
 if(el('athlete-delete-confirm').value.trim()!==copy().deleteWord){notice('confirmation',true);return;}
 if(await mutate({action:'delete',confirmation:'DELETE',confirmAthleteId:state?.profile?.athleteId||'unreadable',expectedRevision:state?.revision||0})){notice('deleted');location.reload();}
};
await read();
