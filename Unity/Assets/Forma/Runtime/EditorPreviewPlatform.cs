using UnityEngine;
namespace Forma.Runtime
{
    public sealed class EditorPreviewPlatform : StudioPlatform
    {
        // Preview can never authorize a headset workout in a player build.
        public override bool PassthroughReady => Application.isEditor;
        public override bool TrackingReady => Application.isEditor;
        public override bool HasFocus => Application.isFocused;
    }
}
