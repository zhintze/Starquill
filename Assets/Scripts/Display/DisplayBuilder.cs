using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starquill.Display
{
    public class DisplayBuilder
    {
        /// CCW degrees applied to off-hand (non-shield) weapons. Tunable so
        /// the landing spot can be dialed against real art.
        public static float OffhandWeaponRotation = 60f;

        /// Canvas-pixel nudge applied after the rotation to seat the weapon
        /// in the hand.
        public static Vector2 OffhandWeaponOffset = new(10f, 4f);

        private static readonly HashSet<string> HairFields = new() { "hair", "facialHair" };
        private static readonly HashSet<string> EyesFields = new() { "eyes" };
        private static readonly HashSet<string> FacialDetailFields = new() { "facialDetail" };

        private readonly DisplayDataRegistry registry;

        public DisplayBuilder(DisplayDataRegistry registry)
        {
            this.registry = registry;
        }

        public List<DisplayPiece> Build(
            SpeciesInstanceData speciesInstance,
            SpeciesDisplayData speciesData,
            List<EquipmentDisplayInfo> equippedItems = null)
        {
            var speciesPieces = BuildSpeciesPieces(speciesInstance, speciesData);
            var equipmentPieces = new List<DisplayPiece>();
            var hiddenLayers = new HashSet<int>();

            if (equippedItems != null && equippedItems.Count > 0)
            {
                var deduped = DeduplicateEquipment(equippedItems, speciesData.ItemRestrictions);
                foreach (var item in deduped)
                {
                    if (!registry.Equipment.TryGetValue(item.ItemType, out var eqData))
                        continue;

                    foreach (int layer in eqData.HiddenLayers)
                        hiddenLayers.Add(layer);

                    BuildEquipmentPieces(item, eqData, equipmentPieces);
                }
            }

            speciesPieces = FilterHiddenLayers(speciesPieces, hiddenLayers);

            return MergeAndSort(speciesPieces, equipmentPieces);
        }

        public List<DisplayPiece> BuildSpeciesPieces(SpeciesInstanceData instance, SpeciesDisplayData species)
        {
            var pieces = new List<DisplayPiece>();

            AddFieldPieces(pieces, "backArm", species.BackArm, instance, instance.SkinColor);
            AddFieldPieces(pieces, "legs", species.Legs, instance, instance.SkinColor);
            AddFieldPieces(pieces, "body", species.Body, instance, instance.SkinColor);
            AddFieldPieces(pieces, "head", species.Head, instance, instance.SkinColor);
            AddFieldPieces(pieces, "ears", species.Ears, instance, instance.SkinColor);
            AddFieldPieces(pieces, "eyes", species.Eyes, instance, instance.EyesColor);
            AddFieldPieces(pieces, "nose", species.Nose, instance, instance.SkinColor);
            AddFieldPieces(pieces, "mouth", species.Mouth, instance, instance.SkinColor);
            AddFieldPieces(pieces, "facialHair", species.FacialHair, instance, instance.HairColor);
            AddFieldPieces(pieces, "facialDetail", species.FacialDetail, instance, instance.FacialDetailColor);
            AddFieldPieces(pieces, "frontArm", species.FrontArm, instance, instance.SkinColor);

            if (!string.IsNullOrEmpty(instance.ChosenHairGroup))
                AddFieldPieces(pieces, "hair", instance.ChosenHairGroup, instance, instance.HairColor);

            if (species.OtherBodyParts != null)
            {
                foreach (var part in species.OtherBodyParts)
                    AddFieldPieces(pieces, "otherBodyParts", part, instance, instance.SkinColor);
            }

            pieces.Sort((a, b) => a.Layer.CompareTo(b.Layer));
            return pieces;
        }

        private void AddFieldPieces(List<DisplayPiece> pieces, string fieldName, string token,
            SpeciesInstanceData instance, Color baseColor)
        {
            if (string.IsNullOrEmpty(token)) return;

            var parsed = ImageToken.Parse(token);
            switch (parsed.Kind)
            {
                case ImageTokenKind.Static:
                {
                    var tint = CalculateSpeciesTint(fieldName, parsed.Layer, baseColor, instance);
                    pieces.Add(new DisplayPiece(parsed.Layer, parsed.ToSpeciesSpritePath(), tint));
                    break;
                }
                case ImageTokenKind.ModularFull:
                {
                    var tint = CalculateSpeciesTint(fieldName, parsed.Layer, baseColor, instance);
                    pieces.Add(new DisplayPiece(parsed.Layer, parsed.ToSpeciesSpritePath(), tint));
                    break;
                }
                case ImageTokenKind.ModularGroup:
                {
                    if (!registry.SpeciesLayerMappings.TryGetValue(parsed.GroupType, out var layers))
                        break;
                    if (!instance.ModularImageNums.TryGetValue(parsed.GroupType, out var imageNum))
                        break;

                    foreach (int layer in layers)
                    {
                        var tint = CalculateSpeciesTint(fieldName, layer, baseColor, instance);
                        string path = ImageToken.BuildModularSpeciesPath(parsed.GroupType, imageNum, layer);
                        pieces.Add(new DisplayPiece(layer, path, tint));
                    }
                    break;
                }
            }
        }

        private Color CalculateSpeciesTint(string fieldName, int layer, Color baseColor, SpeciesInstanceData instance)
        {
            if (HairFields.Contains(fieldName) || EyesFields.Contains(fieldName) || FacialDetailFields.Contains(fieldName))
                return baseColor;

            var variance = instance.GetVarianceColor(layer);
            return variance != Color.white ? variance : baseColor;
        }

        private void BuildEquipmentPieces(EquipmentDisplayInfo item, EquipmentDisplayData eqData, List<DisplayPiece> pieces)
        {
            bool isWeapon = item.ItemType.StartsWith("w");

            for (int i = 0; i < eqData.LayerCodes.Length; i++)
            {
                int layer = eqData.LayerCodes[i];
                string path;

                if (isWeapon)
                {
                    int variant = item.LayerVariants != null && i < item.LayerVariants.Length
                        ? item.LayerVariants[i] : item.ItemNum;
                    path = ImageToken.BuildWeaponSpritePath(item.ItemType, layer, variant);
                }
                else
                {
                    path = ImageToken.BuildEquipmentSpritePath(item.ItemType, item.ItemNum, layer);
                }

                Color tint = item.BaseColor;
                if (eqData.LayerColorVariance != null && eqData.LayerColorVariance.Contains(layer))
                {
                    if (item.VarianceColors != null && item.VarianceColors.TryGetValue(layer, out var vc))
                        tint = vc;
                }

                bool offhandWeapon = item.IsOffhand && isWeapon;
                bool isShield = item.ItemType == "w08" || item.ItemType == "w09";

                // Off-hand weapons draw just under the main hand's layers so
                // the pair reads as two held weapons. Sorting them behind the
                // body was tried and rejected: torso pixels swallow the weapon.
                int sortLayer = offhandWeapon && !isShield ? layer - 1 : layer;

                var piece = new DisplayPiece(sortLayer, path, tint);

                if (offhandWeapon)
                {
                    piece.IsOffhandWeapon = true;
                    if (isShield)
                    {
                        piece.Offset = new Vector2(42, 0);
                    }
                    else
                    {
                        // Rotate about the canvas center: preserves the art's
                        // handedness (a flip mirrors it) and carries the
                        // weapon from the main hand across to the other hand.
                        piece.Rotation = OffhandWeaponRotation;
                        piece.Offset = OffhandWeaponOffset;
                    }
                }

                pieces.Add(piece);
            }
        }

        public static List<DisplayPiece> FilterHiddenLayers(List<DisplayPiece> pieces, HashSet<int> hiddenLayers)
        {
            if (hiddenLayers == null || hiddenLayers.Count == 0) return pieces;
            return pieces.Where(p => !hiddenLayers.Contains(p.Layer)).ToList();
        }

        public static List<(string itemType, int itemNum)> DeduplicateHats(List<(string itemType, int itemNum)> items)
        {
            int lastHatIdx = -1;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var type = items[i].itemType;
                if (type.Length == 4 && type.StartsWith("hd"))
                {
                    if (int.TryParse(type.Substring(2), out int num) && num >= 1 && num <= 8)
                    {
                        if (lastHatIdx < 0) lastHatIdx = i;
                    }
                }
            }

            if (lastHatIdx < 0) return items;

            return items.Where((item, idx) =>
            {
                var type = item.itemType;
                if (type.Length == 4 && type.StartsWith("hd") && int.TryParse(type.Substring(2), out int num) && num >= 1 && num <= 8)
                    return idx == lastHatIdx;
                return true;
            }).ToList();
        }

        private List<EquipmentDisplayInfo> DeduplicateEquipment(List<EquipmentDisplayInfo> items, string[] itemRestrictions)
        {
            var restrictions = new HashSet<string>(itemRestrictions ?? new string[0]);
            var filtered = items.Where(i => !restrictions.Contains(i.ItemType)).ToList();

            var seen = new Dictionary<string, int>();
            for (int i = 0; i < filtered.Count; i++)
                seen[filtered[i].ItemType] = i;
            filtered = filtered.Where((item, idx) => seen[item.ItemType] == idx).ToList();

            int lastHatIdx = -1;
            for (int i = filtered.Count - 1; i >= 0; i--)
            {
                var type = filtered[i].ItemType;
                if (type.Length == 4 && type.StartsWith("hd") && int.TryParse(type.Substring(2), out int num) && num >= 1 && num <= 8)
                {
                    if (lastHatIdx < 0) lastHatIdx = i;
                }
            }

            if (lastHatIdx >= 0)
            {
                filtered = filtered.Where((item, idx) =>
                {
                    var type = item.ItemType;
                    if (type.Length == 4 && type.StartsWith("hd") && int.TryParse(type.Substring(2), out int num) && num >= 1 && num <= 8)
                        return idx == lastHatIdx;
                    return true;
                }).ToList();
            }

            return filtered;
        }

        public static List<DisplayPiece> MergeAndSort(List<DisplayPiece> species, List<DisplayPiece> equipment)
        {
            var merged = new List<DisplayPiece>(species.Count + equipment.Count);
            merged.AddRange(species);
            merged.AddRange(equipment);
            merged.Sort((a, b) => a.Layer.CompareTo(b.Layer));
            return merged;
        }
    }

    public class EquipmentDisplayInfo
    {
        public string ItemType;
        public int ItemNum;
        public Color BaseColor = Color.white;
        public Dictionary<int, Color> VarianceColors;
        public int[] LayerVariants;
        public bool IsOffhand;
    }
}
