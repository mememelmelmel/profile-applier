#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Mememelmelmel.ProfileApplier
{
    [CustomEditor(typeof(ProfileApplier))]
    public class ProfileApplierEditor : Editor
    {
        private IReadOnlyList<string>? _keys;
        private TextAsset? _lastBundleJson;
        private string _search = "";
        private bool _dropdownOpen;
        private Vector2 _scroll;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var applier = (ProfileApplier)target;

            // ── Bundle JSON field ──────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            applier.BundleJson = (TextAsset?)EditorGUILayout.ObjectField(
                "Bundle JSON",
                applier.BundleJson,
                typeof(TextAsset),
                allowSceneObjects: false
            );
            if (EditorGUI.EndChangeCheck() || applier.BundleJson != _lastBundleJson)
            {
                _lastBundleJson = applier.BundleJson;
                _keys = applier.BundleJson != null
                    ? ProfileHelper.GetBundleKeys(applier.BundleJson.text)
                    : null;
                _dropdownOpen = false;
                EditorUtility.SetDirty(target);
            }

            // ── Avatar key searchable dropdown ─────────────────────────────────
            if (_keys == null || _keys.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField("Avatar", applier.AvatarKey);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Avatar");

                var displayLabel = string.IsNullOrEmpty(applier.AvatarKey)
                    ? "(select avatar)"
                    : applier.AvatarKey;

                if (GUILayout.Button(displayLabel, EditorStyles.popup))
                    _dropdownOpen = !_dropdownOpen;

                EditorGUILayout.EndHorizontal();

                if (_dropdownOpen)
                    DrawDropdown(applier);
            }

            EditorGUILayout.Space();

            // ── Apply button ───────────────────────────────────────────────────
            using (new EditorGUI.DisabledScope(
                applier.BundleJson == null || string.IsNullOrEmpty(applier.AvatarKey)
            ))
            {
                if (GUILayout.Button("Apply in Editor"))
                    ApplyProfile(applier);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDropdown(ProfileApplier applier)
        {
            if (_keys == null)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _search = EditorGUILayout.TextField("Search", _search);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(200));
            foreach (var key in _keys)
            {
                if (!string.IsNullOrEmpty(_search)
                    && key.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                bool selected = key == applier.AvatarKey;
                var style = selected ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(key, style))
                {
                    applier.AvatarKey = key;
                    _dropdownOpen = false;
                    _search = "";
                    EditorUtility.SetDirty(target);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private static void ApplyProfile(ProfileApplier applier)
        {
            if (applier.BundleJson == null || string.IsNullOrEmpty(applier.AvatarKey))
                return;

            if (!ProfileHelper.TryParseBundleEntry(
                applier.BundleJson.text,
                applier.AvatarKey,
                out var entry
            ) || entry == null)
            {
                EditorUtility.DisplayDialog(
                    "Profile Applier",
                    $"Avatar key \"{applier.AvatarKey}\" not found in bundle.",
                    "OK"
                );
                return;
            }

            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                applier.gameObject
            );
            var prefabPath = prefabAsset != null
                ? AssetDatabase.GetAssetPath(prefabAsset)
                : null;

            // Save component state so it survives RevertAllOverrides
            var bundleJson = applier.BundleJson;
            var avatarKey = applier.AvatarKey;

            if (!string.IsNullOrEmpty(prefabPath))
            {
                using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
                var root = scope.prefabContentsRoot;
                ProfileHelper.ApplyEntryToPrefab(root, entry);

                // Re-attach ProfileApplier if it was removed by RevertAllOverrides
                var comp = root.GetComponent<ProfileApplier>()
                    ?? root.AddComponent<ProfileApplier>();
                comp.BundleJson = bundleJson;
                comp.AvatarKey = avatarKey;
            }
            else
            {
                // Scene instance: apply directly
                ProfileHelper.ApplyEntryToPrefab(applier.gameObject, entry);

                var comp = applier.gameObject.GetComponent<ProfileApplier>()
                    ?? applier.gameObject.AddComponent<ProfileApplier>();
                comp.BundleJson = bundleJson;
                comp.AvatarKey = avatarKey;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Profile Applier", "Profile applied.", "OK");
        }
    }
}
