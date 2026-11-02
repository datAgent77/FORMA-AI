using UnityEngine;
namespace Forma.Runtime
{
    public abstract class StudioPlatform : MonoBehaviour
    {
        public abstract bool PassthroughReady { get; }
        public abstract bool TrackingReady { get; }
        public abstract bool HasFocus { get; }
    }
}
