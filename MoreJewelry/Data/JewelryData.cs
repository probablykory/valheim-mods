using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreJewelry.Data
{
    public class JewelryDataUnparsed
    {
        public string? Name = null;
        public string? Description = null;
        public string? Prefab = null;
        public object? Gem = null;
        public object? Crafting = null;
        public object? Upgrade = null;
        public object? effect = null;
    }

    [Serializable, CanBeNull]
    public class JewelryData
    {
        public string Name = "";
        public string Description = "";
        public string Prefab = "";
        public VisualData Gem = null;
        public CraftingData Crafting = null;
        public UpgradeData Upgrade = null;
        public EffectData effect = null;
    }

    [Serializable, CanBeNull]
    public class CraftingData
    {
        public string CraftingStation = null;
        public int StationLevel = 0;
        public int MaxQuality = -1;
        public List<CostData> Costs = null;
    }

    [Serializable, CanBeNull]
    public class UpgradeData
    {
        public Dictionary<int, List<CostData>> Costs = null;
    }

    [Serializable, CanBeNull]
    public class CostData
    {
        public string Name = null;
        public int Amount = 0;
        public int AmountPerLevel = 0;
    }

    [Serializable, CanBeNull]
    public class EffectData
    {
        public string? NameToResolve = null;
        public StatusEffectData? SeData = null;
    }

    [Serializable, CanBeNull]
    public class VisualData
    {
        public bool Visible = false;
        public Color? Color = null;
    }
}

