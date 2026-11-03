using UnityEngine;
namespace Forma.Runtime
{
    public readonly struct SpatialPointerSample
    {
        public Ray Ray { get; }
        public bool Pressed { get; }
        public string SourceId { get; }
        public SpatialPointerSample(Ray ray,bool pressed,string sourceId){Ray=ray;Pressed=pressed;SourceId=sourceId;}
    }
    public abstract class SpatialInputSource:MonoBehaviour
    {
        public abstract bool TrySample(out SpatialPointerSample sample);
    }
}
