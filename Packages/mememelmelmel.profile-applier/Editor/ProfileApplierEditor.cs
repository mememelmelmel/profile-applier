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
        private bool _focusSearch;
        private Vector2 _scroll;

        public override void OnInspectorGUI()
        {
            if (target == null) return;
            serializedObject.Update();

            var applier = (ProfileApplier)target;

            // ── Bundle JSON field ──────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            applier.BundleJson = (TextAsset?)EditorGUILayout.ObjectField(
                "Profile Bundle",
                applier.BundleJson,
                typeof(TextAsset),
                allowSceneObjects: false
            );
            bool bundleChanged = EditorGUI.EndChangeCheck();

            // Sync the key list whenever BundleJson differs from the cached value
            // (e.g. on first Inspector open). Only mark dirty when the user explicitly
            // changed the field — otherwise opening the Inspector would dirty the asset
            // and cause it to resurrect after deletion.
            if (bundleChanged || applier.BundleJson != _lastBundleJson)
            {
                _lastBundleJson = applier.BundleJson;
                _keys = applier.BundleJson != null
                    ? ProfileHelper.GetBundleKeys(applier.BundleJson.text)
                    : null;
                _dropdownOpen = false;
                if (bundleChanged)
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
                {
                    _dropdownOpen = !_dropdownOpen;
                    if (_dropdownOpen)
                        _focusSearch = true;
                }

                EditorGUILayout.EndHorizontal();

                if (_dropdownOpen)
                    DrawDropdown(applier);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDropdown(ProfileApplier applier)
        {
            if (_keys == null)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.SetNextControlName("ProfileApplierSearch");
            _search = EditorGUILayout.TextField("Search", _search);
            if (_focusSearch)
            {
                EditorGUI.FocusTextInControl("ProfileApplierSearch");
                if (Event.current.type == EventType.Repaint)
                    _focusSearch = false;
            }

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
                    ApplyProfile(applier);
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

            // Save component state so it survives RevertAllOverrides
            var bundleJson = applier.BundleJson;
            var avatarKey = applier.AvatarKey;

            // Always apply directly to applier.gameObject.
            //
            // - Prefab Edit Mode: the Prefab Stage owns saving; applying directly is correct.
            // - Scene instance: we apply as scene-level overrides only. Using
            //   EditPrefabContentsScope here would (a) write back to the prefab asset
            //   unexpectedly and (b) trigger a reimport that invalidates this Editor's
            //   target, causing SerializedObjectNotCreatableException.
            ProfileHelper.ApplyEntryToPrefab(applier.gameObject, entry);

            var comp = applier.gameObject.GetComponent<ProfileApplier>()
                ?? applier.gameObject.AddComponent<ProfileApplier>();
            comp.BundleJson = bundleJson;
            comp.AvatarKey = avatarKey;

            AssetDatabase.SaveAssets();
        }
    }
}
