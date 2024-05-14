using BepInEx.Configuration;
using HarmonyLib;
using MockManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ItemManager;

// customizations to prefabmanager
public static partial class PrefabManager
{
    public static GameObject GetPrefab(string name)
    {
        int hash = name.GetStableHashCode();

        if (ZNetScene.instance && ZNetScene.instance.m_namedPrefabs.TryGetValue(hash, out var prefab))
        {
            return prefab;
        }

        if (ObjectDB.instance && ObjectDB.instance.m_itemByHash.TryGetValue(hash, out var item))
        {
            return item;
        }

        if (Item.RegisteredItems.Count > 0)
        {
            var result = Item.RegisteredItems.FirstOrDefault((i) => name.Equals(i.Prefab.name));
            if (result != null)
                return result.Prefab;
        }

        return MockManager.Cache.GetPrefab<GameObject>(name);

        //return null;
    }

    public static GameObject RegisterPrefabWithMocks(AssetBundle assets, string prefabName, bool addToObjectDb = false, bool recursive = true) {
        if (assets == null) throw new System.ArgumentNullException(nameof(assets));

        GameObject prefab = RegisterPrefab(assets.LoadAsset<GameObject>(prefabName), addToObjectDb);
        prefab.FixReferences(recursive);
        return prefab;
    }
}
