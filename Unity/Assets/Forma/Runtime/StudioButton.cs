using System;
using UnityEngine;
namespace Forma.Runtime
{
    public sealed class StudioButton : MonoBehaviour
    {
        public Action Click;
        public TextMesh Label;
        public void Invoke() { Click?.Invoke(); }
    }
}
