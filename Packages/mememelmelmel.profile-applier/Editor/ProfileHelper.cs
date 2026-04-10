#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Mememelmelmel.ProfileApplier
{
    public static class ProfileHelper
    {
        // Collects all override information from the variant asset itself.
        public static PresetEntry CollectPresetEntry(GameObject variantAsset)
        {
            var modified = CollectModifiedProperties(variantAsset);
            var added = CollectAddedComponents(variantAsset);
            var removed = CollectRemovedComponents(variantAsset);
            return new PresetEntry
            {
                Overrides = modified.Count > 0 ? modified : null,
                AddedComponents = added.Count > 0 ? added : null,
                RemovedComponents = removed.Count > 0 ? removed : null,
            };
        }

        public static bool TryParsePresetEntry(string json, out PresetEntry? entry)
        {
            entry = null;
            try
            {
                entry = JsonConvert.DeserializeObject<PresetEntry>(json);
                return entry != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Parses a bundle JSON and returns all avatar keys, or null on error.
        /// </summary>
        public static System.Collections.Generic.IReadOnlyList<string>? GetBundleKeys(
            string bundleJson
        )
        {
            try
            {
                var dict =
                    JsonConvert.DeserializeObject<
                        System.Collections.Generic.Dictionary<string, object>
                    >(bundleJson);
                return dict != null
                    ? new System.Collections.Generic.List<string>(dict.Keys)
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Parses a bundle JSON and extracts the PresetEntry for the given avatar key.
        /// </summary>
        public static bool TryParseBundleEntry(
            string bundleJson,
            string avatarKey,
            out PresetEntry? entry
        )
        {
            entry = null;
            try
            {
                var dict =
                    JsonConvert.DeserializeObject<
                        System.Collections.Generic.Dictionary<string, PresetEntry>
                    >(bundleJson);
                if (dict == null || !dict.TryGetValue(avatarKey, out entry))
                    return false;
                return entry != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static void RevertAllOverrides(GameObject root)
        {
            PrefabUtility.SetPropertyModifications(root, new PropertyModification[0]);

            foreach (var added in PrefabUtility.GetAddedComponents(root))
                Object.DestroyImmediate(added.instanceComponent);

            foreach (var removed in PrefabUtility.GetRemovedComponents(root))
                PrefabUtility.RevertRemovedComponent(
                    removed.containingInstanceGameObject,
                    removed.assetComponent,
                    InteractionMode.AutomatedAction);
        }

        /// <summary>
        /// Applies a profile entry directly to a plain GameObject (no Prefab API calls).
        /// Use this in build callbacks where the target is not a Prefab instance.
        /// </summary>
        public static void ApplyEntryToInstance(GameObject root, PresetEntry entry)
        {
            if (entry.Overrides != null)
                ApplyPropertyDict(root, entry.Overrides, addIfMissing: false);

            if (entry.AddedComponents != null)
                ApplyPropertyDict(root, entry.AddedComponents, addIfMissing: true);

            if (entry.RemovedComponents != null)
            {
                foreach (var (goPath, typeNames) in entry.RemovedComponents)
                {
                    var go = FindGameObjectByPath(root, goPath);
                    if (go == null)
                        continue;

                    foreach (var typeName in typeNames)
                    {
                        var type = FindTypeByName(typeName);
                        if (type == null)
                            continue;
                        var comp = go.GetComponent(type);
                        if (comp != null)
                            Object.DestroyImmediate(comp);
                    }
                }
            }
        }

        public static void ApplyEntryToPrefab(GameObject root, PresetEntry entry)
        {
            RevertAllOverrides(root);

            // Modified properties
            if (entry.Overrides != null)
                ApplyPropertyDict(root, entry.Overrides, addIfMissing: false);

            // Added components
            if (entry.AddedComponents != null)
                ApplyPropertyDict(root, entry.AddedComponents, addIfMissing: true);

            // Removed components
            if (entry.RemovedComponents != null)
            {
                foreach (var (goPath, typeNames) in entry.RemovedComponents)
                {
                    var go = FindGameObjectByPath(root, goPath);
                    if (go == null)
                        continue;

                    foreach (var typeName in typeNames)
                    {
                        var type = FindTypeByName(typeName);
                        if (type == null)
                            continue;
                        var comp = go.GetComponent(type);
                        if (comp != null)
                            Object.DestroyImmediate(comp);
                    }
                }
            }
        }

        // ── Collect helpers ────────────────────────────────────────────────

        private static Dictionary<
            string,
            Dictionary<string, Dictionary<string, string>>
        > CollectModifiedProperties(GameObject variantAsset)
        {
            var result = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            var mods = PrefabUtility.GetPropertyModifications(variantAsset);
            if (mods == null)
                return result;

            var basePrefab =
                PrefabUtility.GetCorrespondingObjectFromSource(variantAsset) as GameObject;
            var baseName = basePrefab != null ? basePrefab.name : variantAsset.name;

            foreach (var mod in mods)
            {
                if (IsDefaultOverride(mod.propertyPath))
                    continue;
                var comp = mod.target as Component;
                if (comp == null)
                    continue;

                var goPath = NormalizeBasePath(
                    GetRelativePath(comp.transform, variantAsset.transform),
                    baseName
                );
                var compType = comp.GetType().Name;

                if (!result.ContainsKey(goPath))
                    result[goPath] = new Dictionary<string, Dictionary<string, string>>();
                if (!result[goPath].ContainsKey(compType))
                    result[goPath][compType] = new Dictionary<string, string>();

                result[goPath][compType][ResolvePropertyKey(mod.propertyPath, comp)] = mod.value;
            }
            return result;
        }

        private static Dictionary<
            string,
            Dictionary<string, Dictionary<string, string>>
        > CollectAddedComponents(GameObject variantAsset)
        {
            var result = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            var added = PrefabUtility.GetAddedComponents(variantAsset);
            if (added == null)
                return result;

            foreach (var ac in added)
            {
                var comp = ac.instanceComponent;
                if (comp == null)
                    continue;

                var goPath = NormalizeVariantPath(
                    GetRelativePath(comp.transform, variantAsset.transform)
                );
                string compType = comp.GetType().Name;

                if (!result.ContainsKey(goPath))
                    result[goPath] = new Dictionary<string, Dictionary<string, string>>();

                result[goPath][compType] = SerializeAllProperties(comp);
            }
            return result;
        }

        private static Dictionary<string, List<string>> CollectRemovedComponents(
            GameObject variantAsset
        )
        {
            var result = new Dictionary<string, List<string>>();
            var removed = PrefabUtility.GetRemovedComponents(variantAsset);
            if (removed == null)
                return result;

            foreach (var rc in removed)
            {
                var go = rc.containingInstanceGameObject;
                if (go == null || rc.assetComponent == null)
                    continue;

                var goPath = NormalizeVariantPath(
                    GetRelativePath(go.transform, variantAsset.transform)
                );
                string compType = rc.assetComponent.GetType().Name;

                if (!result.ContainsKey(goPath))
                    result[goPath] = new List<string>();
                result[goPath].Add(compType);
            }
            return result;
        }

        private static Dictionary<string, string> SerializeAllProperties(Component comp)
        {
            var props = new Dictionary<string, string>();
            var so = new SerializedObject(comp);
            var iter = so.GetIterator();
            while (iter.NextVisible(true))
            {
                if (iter.propertyType == SerializedPropertyType.Generic)
                    continue;
                if (iter.propertyPath == "m_ObjectHideFlags")
                    continue;
                var val = GetSerializedValueAsString(iter);
                if (val != null)
                    props[iter.propertyPath] = val;
            }
            return props;
        }

        private static string? GetSerializedValueAsString(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.Float => prop.floatValue.ToString(
                    CultureInfo.InvariantCulture
                ),
                SerializedPropertyType.Integer => prop.intValue.ToString(),
                SerializedPropertyType.Boolean => prop.boolValue ? "true" : "false",
                SerializedPropertyType.String => prop.stringValue,
                _ => null,
            };
        }

        private static readonly HashSet<string> _defaultOverridePaths = new HashSet<string>
        {
            "m_Name",
            "m_RootOrder",
            "m_LocalEulerAnglesHint.x",
            "m_LocalEulerAnglesHint.y",
            "m_LocalEulerAnglesHint.z",
        };

        private static bool IsDefaultOverride(string path) => _defaultOverridePaths.Contains(path);

        private static string ResolvePropertyKey(string propertyPath, Component comp)
        {
            const string Prefix = "m_BlendShapeWeights.Array.data[";
            if (!propertyPath.StartsWith(Prefix))
                return propertyPath;

            var lb = Prefix.Length;
            var rb = propertyPath.IndexOf(']', lb);
            if (rb <= lb)
                return propertyPath;
            if (!int.TryParse(propertyPath.Substring(lb, rb - lb), out var idx))
                return propertyPath;

            var smr = comp as SkinnedMeshRenderer;
            if (smr == null || smr.sharedMesh == null || idx >= smr.sharedMesh.blendShapeCount)
                return propertyPath;

            return "blendShape." + smr.sharedMesh.GetBlendShapeName(idx);
        }

        private static string GetRelativePath(Transform t, Transform root)
        {
            if (t == root)
                return "";
            var parts = new List<string>();
            var cur = t;
            while (cur != null && cur != root)
            {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string NormalizeBasePath(string path, string rootName)
        {
            if (path == rootName)
                return "";
            if (path.StartsWith(rootName + "/"))
                return "/" + path.Substring(rootName.Length + 1);
            return path;
        }

        private static string NormalizeVariantPath(string path) => path == "" ? "" : "/" + path;

        // ── Apply helpers ──────────────────────────────────────────────────

        private static void ApplyPropertyDict(
            GameObject root,
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> dict,
            bool addIfMissing
        )
        {
            foreach (var (goPath, compDict) in dict)
            {
                var go = FindGameObjectByPath(root, goPath);
                if (go == null)
                    continue;

                foreach (var (compType, propDict) in compDict)
                {
                    var comp = go.GetComponents<Component>()
                        .FirstOrDefault(c => c.GetType().Name == compType);

                    if (comp == null)
                    {
                        if (!addIfMissing)
                            continue;
                        var type = FindTypeByName(compType);
                        if (type == null)
                            continue;
                        comp = go.AddComponent(type);
                    }

                    var so = new SerializedObject(comp);
                    so.Update();
                    foreach (var (propPath, value) in propDict)
                    {
                        var prop = ResolveSerializedProperty(so, propPath, comp);
                        if (prop == null)
                            continue;
                        SetSerializedPropertyFromString(prop, value);
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static GameObject? FindGameObjectByPath(GameObject root, string path)
        {
            if (path == "")
                return root;
            var sub = path.StartsWith("/") ? path.Substring(1) : path;
            return root.transform.Find(sub)?.gameObject;
        }

        private static System.Type? FindTypeByName(string typeName) =>
            System
                .AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName))
                .FirstOrDefault(t => t != null && typeof(Component).IsAssignableFrom(t));

        private static SerializedProperty? ResolveSerializedProperty(
            SerializedObject so,
            string propPath,
            Component comp
        )
        {
            const string BlendPrefix = "blendShape.";
            if (propPath.StartsWith(BlendPrefix))
            {
                var smr = comp as SkinnedMeshRenderer;
                if (smr == null || smr.sharedMesh == null)
                    return null;

                var shapeName = propPath.Substring(BlendPrefix.Length);
                var idx = smr.sharedMesh.GetBlendShapeIndex(shapeName);
                if (idx < 0)
                    return null;

                return so.FindProperty($"m_BlendShapeWeights.Array.data[{idx}]");
            }
            return so.FindProperty(propPath);
        }

        private static void SetSerializedPropertyFromString(SerializedProperty prop, string value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    if (
                        float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var f
                        )
                    )
                        prop.floatValue = f;
                    break;
                case SerializedPropertyType.Integer:
                    if (int.TryParse(value, out var n))
                        prop.intValue = n;
                    break;
                case SerializedPropertyType.Boolean:
                    if (bool.TryParse(value, out var b))
                        prop.boolValue = b;
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = value;
                    break;
            }
        }
    }
}
