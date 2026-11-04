using System;
using UnityEngine;
namespace Forma.Runtime
{
    public sealed class StudioButton : MonoBehaviour
    {
        public Action Click;
        public TextMesh Label;
        Renderer surface;
        MaterialPropertyBlock highlight;
        Color labelColor;
        bool captured;
        public void SetHighlighted(bool value)
        {
            if(surface==null)surface=GetComponent<Renderer>();
            if(surface==null)return;
            if(highlight==null)highlight=new MaterialPropertyBlock();
            if(!captured && Label!=null){labelColor=Label.color;captured=true;}
            highlight.Clear();if(value)highlight.SetColor("_Color",new Color(.93f,.77f,.35f));surface.SetPropertyBlock(highlight);
            if(Label!=null)Label.color=value?new Color(.08f,.1f,.09f):labelColor;
        }
        public void Invoke() { Click?.Invoke(); }
    }
}
