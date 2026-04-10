#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Mememelmelmel.ProfileApplier
{
    public class PresetEntry
    {
        // Modified properties: goPath → componentTypeName → propertyPath → stringValue
        [JsonProperty("overrides")]
        public Dictionary<
            string,
            Dictionary<string, Dictionary<string, string>>
        >? Overrides { get; set; }

        // Added components (exist in Variant but not in Base):
        // goPath → componentTypeName → propertyPath → stringValue (all serialized primitives)
        [JsonProperty("addedComponents")]
        public Dictionary<
            string,
            Dictionary<string, Dictionary<string, string>>
        >? AddedComponents { get; set; }

        // Removed components (exist in Base but removed in Variant):
        // goPath → list of componentTypeNames
        [JsonProperty("removedComponents")]
        public Dictionary<string, List<string>>? RemovedComponents { get; set; }
    }
}
