using System.Collections.Generic;
using Forma.Product;
using UnityEngine;
namespace Forma.Runtime
{
    // Three original, explicitly development-stage geometric looks; no external mesh/voice assets.
    public sealed class CoachAppearance : MonoBehaviour
    {
        readonly List<Material> instances=new List<Material>();
        GameObject accessory;
        public void Apply(CoachDefinition definition)
        {
            ColorUtility.TryParseHtmlString(definition.primaryColor,out Color primary);
            ColorUtility.TryParseHtmlString(definition.accentColor,out Color accent);
            if(accessory!=null){accessory.SetActive(false);Destroy(accessory);accessory=null;}
            foreach(var m in instances)Destroy(m);instances.Clear();
            foreach(var r in GetComponentsInChildren<Renderer>())
            { var m=new Material(Shader.Find("Unlit/Color")){color=r.name=="Head"?accent:primary};r.sharedMaterial=m;instances.Add(m); }
            if(accessory!=null){Destroy(accessory);accessory=null;}
            accessory=GameObject.CreatePrimitive(definition.id=="atlas"?PrimitiveType.Cube:PrimitiveType.Sphere);
            accessory.name="Original preset accent";accessory.transform.SetParent(transform,false);
            accessory.transform.localPosition=definition.id=="mira"?new Vector3(0,1.90f,0):new Vector3(0,1.72f,-0.13f);
            accessory.transform.localScale=definition.id=="mira"?new Vector3(0.2f,0.04f,0.2f):new Vector3(0.14f,0.045f,0.04f);
            Destroy(accessory.GetComponent<Collider>());
            var material=new Material(Shader.Find("Unlit/Color")){color=accent};instances.Add(material);
            accessory.GetComponent<Renderer>().sharedMaterial=material;
        }
        public void Place(Camera camera,int slot)
        {
            Vector3 forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<0.1f)forward=Vector3.forward;
            forward=Quaternion.AngleAxis(slot==0?-35:slot==1?35:55,Vector3.up)*forward;
            Vector3 position=camera.transform.position+forward*2.6f;
            position.y=camera.transform.position.y-1.6f;
            transform.SetPositionAndRotation(position,Quaternion.LookRotation(-forward,Vector3.up));
            // Coach is placed manually by the user. No claim of physical obstacle detection.
        }
        void OnDestroy(){foreach(var m in instances)if(m!=null)Destroy(m);}
    }
}
