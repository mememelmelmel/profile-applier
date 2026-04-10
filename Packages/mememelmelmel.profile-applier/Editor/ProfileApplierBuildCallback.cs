#nullable enable
#if VRC_SDK_VRCSDK3
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

// ReSharper disable once CheckNamespace
namespace Mememelmelmel.ProfileApplier
{
    /// <summary>
    /// Applies hair profiles and removes ProfileApplier components before VRC upload.
    /// Runs before VRC SDK validation, so the component is never present during upload checks.
    /// </summary>
    public class ProfileApplierBuildCallback : IVRCSDKPreprocessAvatarCallback
    {
        // Run early so other preprocessors see the applied values
        public int callbackOrder => -1000;

        public bool OnPreprocessAvatar(GameObject avatarRoot)
        {
            foreach (var applier in avatarRoot.GetComponentsInChildren<ProfileApplier>(true))
            {
                if (applier.BundleJson == null || string.IsNullOrEmpty(applier.AvatarKey))
                {
                    Object.DestroyImmediate(applier);
                    continue;
                }

                if (!ProfileHelper.TryParseBundleEntry(
                    applier.BundleJson.text,
                    applier.AvatarKey,
                    out var entry
                ) || entry == null)
                {
                    Debug.LogWarning(
                        $"[ProfileApplier] Avatar key \"{applier.AvatarKey}\" not found in bundle "
                            + $"on \"{applier.gameObject.name}\". Skipping.",
                        applier.gameObject
                    );
                    Object.DestroyImmediate(applier);
                    continue;
                }

                ProfileHelper.ApplyEntryToPrefab(applier.gameObject, entry);
                Object.DestroyImmediate(applier);
            }

            return true;
        }
    }
}
#endif
