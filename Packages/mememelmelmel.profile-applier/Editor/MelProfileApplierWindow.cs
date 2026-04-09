#nullable enable
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Mememelmelmel.ProfileApplier
{
    public class MelProfileApplierWindow : EditorWindow
    {
        [MenuItem("Tools/Mel Profile Applier")]
        public static void ShowWindow()
        {
            var window = GetWindow<MelProfileApplierWindow>("Mel Profile Applier");
            window.minSize = new Vector2(300f, 100f);
            window.Show();
        }

        private GameObject? _prefab;
        private TextAsset? _profile;

        private void OnGUI()
        {
            _prefab = (GameObject)
                EditorGUILayout.ObjectField("Prefab", _prefab, typeof(GameObject), false);
            _profile = (TextAsset)
                EditorGUILayout.ObjectField("Profile", _profile, typeof(TextAsset), false);

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(_prefab == null || _profile == null);
            if (GUILayout.Button("Apply Profile"))
                ApplyProfile();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_prefab == null);
            if (GUILayout.Button("Export Profile"))
                ExportProfile();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void ApplyProfile()
        {
            if (_profile == null || _prefab == null) return;
            var profile = _profile;
            var prefab = _prefab;
            if (!ProfileHelper.TryParsePresetEntry(profile.text, out var entry) || entry == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to parse the profile.", "OK");
                return;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                    ProfileHelper.ApplyEntryToPrefab(scope.prefabContentsRoot, entry);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Undo.RecordObject(prefab, "Apply Profile");
                ProfileHelper.ApplyEntryToPrefab(prefab, entry);
            }
            EditorUtility.DisplayDialog("Done", "Profile applied successfully.", "OK");
        }

        private void ExportProfile()
        {
            if (_prefab == null) return;
            var prefab = _prefab;
            var initialPath =
                System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(prefab)) ?? "";
            var assetPath = EditorUtility.SaveFilePanelInProject(
                "Export Profile",
                prefab.name,
                "json",
                "Select a destination to save the profile",
                initialPath
            );
            if (string.IsNullOrEmpty(assetPath))
                return;

            var absPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "..", assetPath)
            );

            var entry = ProfileHelper.CollectPresetEntry(prefab);
            var json = JsonConvert.SerializeObject(
                entry,
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
            );
            System.IO.File.WriteAllText(absPath, json, Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done", "Profile exported successfully.", "OK");
        }
    }
}
