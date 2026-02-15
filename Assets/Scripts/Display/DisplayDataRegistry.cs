using System.Collections.Generic;
using Starquill.Core;
using UnityEngine;

namespace Starquill.Display
{
    public class SpeciesDisplayData
    {
        public string Name;
        public string BackArm, Body, Ears, Eyes, FacialDetail, FacialHair;
        public string FrontArm, Head, Legs, Mouth, Nose;
        public string[] Hair;
        public string[] OtherBodyParts;
        public string[] ItemRestrictions;
        public string[] SkinColor;
        public string[] HairColor;
        public string[] EyesColor;
        public string[] FacialDetailColor;
        public SkinVarianceSet[] SkinVarianceSets;
        public float XScale = 1f, YScale = 1f;
    }

    public class SkinVarianceSet
    {
        public string[] HexColors;
        public int[] Indices;
    }

    public class EquipmentDisplayData
    {
        public string ItemType;
        public string Description;
        public int Amount;
        public int[] LayerCodes;
        public int[] LayerColorVariance;
        public int[] HiddenLayers;
        public bool Modular;
    }

    public class DisplayDataRegistry
    {
        public Dictionary<string, SpeciesDisplayData> Species { get; private set; } = new();
        public Dictionary<string, EquipmentDisplayData> Equipment { get; private set; } = new();
        public Dictionary<string, int> ModularPartCounts { get; private set; } = new();
        public Dictionary<string, int[]> SpeciesLayerMappings { get; private set; } = new();
        public ColorManager Colors { get; private set; } = new();

        private static DisplayDataRegistry _instance;
        public static DisplayDataRegistry Instance => _instance ??= new DisplayDataRegistry();

        public void LoadAll()
        {
            LoadSpecies();
            LoadEquipment();
            LoadModularParts();
            LoadLayerMappings();
            Colors.LoadFromResources();
        }

        private void LoadSpecies()
        {
            var asset = Resources.Load<TextAsset>("Data/species");
            if (asset == null) { Debug.LogWarning("DisplayDataRegistry: species.json not found"); return; }
            ParseSpeciesJson(asset.text);
        }

        private void LoadEquipment()
        {
            var asset = Resources.Load<TextAsset>("Data/equipment");
            if (asset == null) { Debug.LogWarning("DisplayDataRegistry: equipment.json not found"); return; }
            ParseEquipmentJson(asset.text);
        }

        private void LoadModularParts()
        {
            var asset = Resources.Load<TextAsset>("Data/speciesModularParts");
            if (asset == null) { Debug.LogWarning("DisplayDataRegistry: speciesModularParts.json not found"); return; }
            ParseModularPartsJson(asset.text);
        }

        private void LoadLayerMappings()
        {
            SpeciesLayerMappings = new Dictionary<string, int[]>
            {
                { "f01", new[] { 84 } },
                { "f02", new[] { 86 } },
                { "f03", new[] { 85 } },
                { "f04", new[] { 100 } },
                { "f05", new[] { 122 } },
                { "f06", new[] { 87 } },
                { "h01", new[] { 92 } },
                { "h02", new[] { 92, 128 } },
                { "h03", new[] { 92, 154 } },
                { "h04", new[] { 92, 128, 154 } },
                { "d02", new[] { 86 } },
                { "d04", new[] { 100 } },
                { "d05", new[] { 122 } },
                { "d06", new[] { 87 } },
                { "g02", new[] { 86 } },
                { "s01", new[] { 82 } },
                { "s02", new[] { 84 } },
                { "s03", new[] { 86 } },
                { "s04", new[] { 87 } },
            };
        }

        private void ParseSpeciesJson(string json)
        {
            Species.Clear();
            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                var data = new SpeciesDisplayData();
                data.Name = obj.GetString("name", "");
                data.BackArm = obj.GetString("backArm", "");
                data.Body = obj.GetString("body", "");
                data.Ears = obj.GetString("ears", "");
                data.Eyes = obj.GetString("eyes", "");
                data.FacialDetail = obj.GetString("facialDetail", "");
                data.FacialHair = obj.GetString("facialHair", "");
                data.FrontArm = obj.GetString("frontArm", "");
                data.Head = obj.GetString("head", "");
                data.Legs = obj.GetString("legs", "");
                data.Mouth = obj.GetString("mouth", "");
                data.Nose = obj.GetString("nose", "");
                data.Hair = obj.GetStringArray("hair");
                data.OtherBodyParts = obj.GetStringArray("otherBodyParts");
                data.ItemRestrictions = obj.GetStringArray("itemRestrictions");
                data.SkinColor = obj.GetStringArray("skin_color");
                data.HairColor = obj.GetStringArray("hair_color");
                data.EyesColor = obj.GetStringArray("eyes_color");
                data.FacialDetailColor = obj.GetStringArray("facialDetail_color");
                data.XScale = obj.GetFloat("x_scale", 1f);
                data.YScale = obj.GetFloat("y_scale", 1f);

                var sets = obj.GetArray("skinVariance_sets");
                if (sets != null)
                {
                    data.SkinVarianceSets = new SkinVarianceSet[sets.Count];
                    for (int i = 0; i < sets.Count; i++)
                    {
                        var setObj = sets[i] as Dictionary<string, object>;
                        if (setObj == null) continue;
                        var vs = new SkinVarianceSet();
                        vs.HexColors = SimpleJson.ToStringArray(setObj, "hex_colors");
                        vs.Indices = SimpleJson.ToIntArray(setObj, "indices");
                        data.SkinVarianceSets[i] = vs;
                    }
                }
                else
                {
                    data.SkinVarianceSets = new SkinVarianceSet[0];
                }

                if (!string.IsNullOrEmpty(data.Name))
                    Species[data.Name] = data;
            }
        }

        private void ParseEquipmentJson(string json)
        {
            Equipment.Clear();
            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                var data = new EquipmentDisplayData();
                data.ItemType = obj.GetString("item_type", "");
                data.Description = obj.GetString("description", "");
                data.Amount = obj.GetInt("amount", 1);
                data.LayerCodes = obj.GetIntArray("layer_codes");
                data.LayerColorVariance = obj.GetIntArray("layer_color_variance");
                data.HiddenLayers = obj.GetIntArray("hidden_layers");
                data.Modular = obj.GetBool("modular", false);

                if (!string.IsNullOrEmpty(data.ItemType))
                    Equipment[data.ItemType] = data;
            }
        }

        private void ParseModularPartsJson(string json)
        {
            ModularPartCounts.Clear();
            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                string type = obj.GetString("type", "");
                int amount = obj.GetInt("amount", 1);
                if (!string.IsNullOrEmpty(type))
                    ModularPartCounts[type] = amount;
            }
        }

        public string PickModularImageNum(string groupType, System.Random rng = null)
        {
            if (!ModularPartCounts.TryGetValue(groupType, out int amount))
                amount = 1;
            int num = rng != null ? rng.Next(1, amount + 1) : Random.Range(1, amount + 1);
            return num.ToString("D4");
        }
    }
}
