#nullable enable
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Mememelmelmel.ProfileApplier
{
    /// <summary>
    /// Applies a hair profile from a bundle JSON at build time (VRC upload)
    /// or via the "Apply in Editor" button in the Inspector.
    /// </summary>
    [AddComponentMenu("Mel/Profile Applier")]
    public class ProfileApplier : MonoBehaviour
    {
        [Tooltip("The bundle JSON asset containing profiles for this hair asset.")]
        public TextAsset? BundleJson;

        [Tooltip("The avatar key to apply from the bundle.")]
        public string AvatarKey = "";
    }
}
