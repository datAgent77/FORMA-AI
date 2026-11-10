export const weightToDisplay=(kg,units)=>kg==null?'':kg*(units==='Imperial'?2.2046226218:1);
export const heightToDisplay=(cm,units)=>cm==null?'':cm/(units==='Imperial'?2.54:1);
export const weightFromDisplay=(value,units)=>String(value).trim()===''?null:Number(value)/(units==='Imperial'?2.2046226218:1);
export const heightFromDisplay=(value,units)=>String(value).trim()===''?null:Number(value)*(units==='Imperial'?2.54:1);

export function movementMask(value){if(typeof value==='number')return value;return (String(value).includes('AvoidLowerBody')?1:0)|(String(value).includes('AvoidShoulders')?2:0);}
