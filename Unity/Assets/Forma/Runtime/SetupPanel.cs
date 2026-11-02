using System;
using System.Collections.Generic;
using UnityEngine;
namespace Forma.Runtime
{
    // A compact spatial panel. No HTML mockup or additional UI package is required.
    public sealed class SetupPanel : MonoBehaviour
    {
        public TextMesh title,body,progress,notice;
        public StudioButton[] choices;
        public StudioButton back,next,skip,place,coachPosition,exit;
        readonly List<Material> materials=new List<Material>();
        static readonly Color Ink=new Color(0.94f,0.96f,0.94f);
        static readonly Color Ocean=new Color(0.13f,0.49f,0.51f);
        public void Build()
        {
            var background=GameObject.CreatePrimitive(PrimitiveType.Cube);background.name="Setup surface";
            background.transform.SetParent(transform,false);background.transform.localScale=new Vector3(2.22f,2.48f,0.025f);
            background.transform.localPosition=new Vector3(0,-0.08f,0.04f);Destroy(background.GetComponent<Collider>());
            background.GetComponent<Renderer>().sharedMaterial=MakeMaterial(new Color(0.055f,0.10f,0.15f));
            progress=Text("Step",new Vector3(-0.96f,1.03f,-0.01f),0.021f);
            title=Text("Title",new Vector3(-0.96f,0.85f,-0.01f),0.043f);
            body=Text("Body",new Vector3(-0.96f,0.58f,-0.01f),0.025f);
            notice=Text("Status",new Vector3(-0.96f,-0.64f,-0.01f),0.021f);
            choices=new StudioButton[6];
            for(int i=0;i<6;i++)choices[i]=Button("Option "+i,new Vector3(0,0.18f-i*0.135f,0),1.96f);
            back=Button("Back",new Vector3(-0.74f,-0.84f,0),0.48f);
            skip=Button("Skip",new Vector3(-0.07f,-0.84f,0),0.8f);
            next=Button("Continue",new Vector3(0.72f,-0.84f,0),0.62f);
            place=Button("Place panel",new Vector3(-0.7f,-1.04f,0),0.62f);
            coachPosition=Button("Move coach",new Vector3(0,-1.04f,0),0.62f);
            exit=Button("Exit",new Vector3(0.7f,-1.04f,0),0.62f);
        }
        public void PlaceInFront(Camera camera)
        {
            Vector3 forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up);
            if(forward.sqrMagnitude<0.01f)forward=Vector3.forward;
            forward.Normalize();transform.SetPositionAndRotation(camera.transform.position+forward*2.4f+Vector3.up*0.12f,Quaternion.LookRotation(forward,Vector3.up));
        }
        TextMesh Text(string name,Vector3 position,float size)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=position;
            var text=go.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.UpperLeft;text.color=Ink;
            go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;return text;
        }
        StudioButton Button(string name,Vector3 position,float width)
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.transform.SetParent(transform,false);
            box.transform.localPosition=position;box.transform.localScale=new Vector3(width,0.112f,0.025f);
            box.GetComponent<Renderer>().sharedMaterial=MakeMaterial(Ocean);
            var button=box.AddComponent<StudioButton>();
            button.Label=Text(name+" label",position+new Vector3(-width/2+0.025f,0.027f,-0.02f),width<1?0.020f:0.024f);
            return button;
        }
        public void Set(StudioButton b,string label,Action action,bool visible=true)
        { b.gameObject.SetActive(visible);b.Label.gameObject.SetActive(visible);b.Label.text=label;b.Click=action; }
        Material MakeMaterial(Color color)
        {
            var shader=Shader.Find("Unlit/Color");if(shader==null)throw new InvalidOperationException("Built-in Unlit/Color is required.");
            var m=new Material(shader){color=color};materials.Add(m);return m;
        }
        void OnDestroy(){foreach(var m in materials)if(m!=null)Destroy(m);}
    }
}
