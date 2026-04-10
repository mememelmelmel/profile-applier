#nullable enable
#if VRC_SDK_VRCSDK3
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

// ReSharper disable once CheckNamespace
namespace Mememelmelmel.ProfileApplier
{
    /// <summary>
    /// Removes ProfileApplier components before VRC upload.
    /// Profile values are already baked into the Prefab via Apply in Editor,
    /// so no re-application is needed at build time.
    /// </summary>
    public class ProfileApplierBuildCallback : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => 0;

        public bool OnPreprocessAvatar(GameObject avatarRoot)
        {
            // Profile values are already applied to the Prefab via Apply in Editor.
            // The only job here is to remove the component from the build copy so it
            // doesn't appear in the uploaded avatar.
            foreach (var applier in avatarRoot.GetComponentsInChildren<ProfileApplier>(true))
                Object.DestroyImmediate(applier);

            return true;
        }
    }
}
#endif
