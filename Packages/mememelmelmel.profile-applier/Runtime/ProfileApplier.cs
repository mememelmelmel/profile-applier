#nullable enable
using UnityEngine;

// When VRC SDK is not present, declare a local stub so the code compiles.
// When VRC SDK is present, the real VRC.SDKBase.IEditorOnly is used instead,
// which tells the VRC upload pipeline this component is editor-only (no warning).
#if !VRC_SDK_VRCSDK3
namespace VRC.SDKBase { public interface IEditorOnly { } }
#endif

// ReSharper disable once CheckNamespace
namespace Mememelmelmel.ProfileApplier
{
    /// <summary>
    /// Applies a hair profile from a bundle JSON at build time (VRC upload).
    /// Implements IEditorOnly so the VRC SDK upload tab does not warn about it.
    /// </summary>
    [AddComponentMenu("Mel/Profile Applier")]
    public class ProfileApplier : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        [Tooltip("The bundle JSON asset containing profiles for this hair asset.")]
        public TextAsset? BundleJson;

        [Tooltip("The avatar key to apply from the bundle.")]
        public string AvatarKey = "";
    }
}
