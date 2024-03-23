using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MockManager
{
    /// <summary>
    ///     Global cache of Unity Objects by asset name.<br />
    ///     Built on first access of every type and is cleared on scene change.
    /// </summary>
    public static class Cache
    {
        private static readonly Dictionary<Type, Dictionary<string, Object>> dictionaryCache =
            new Dictionary<Type, Dictionary<string, Object>>();

        /// <summary>
        ///     Get an instance of an Unity Object from the current scene with the given name.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to search for.</param>
        /// <param name="name">Name of the actual object to search for.</param>
        /// <returns></returns>
        public static Object GetPrefab(Type type, string name)
        {
            if (GetCachedMap(type).TryGetValue(name, out var unityObject))
            {
                return unityObject;
            }

            return null;
        }

        /// <summary>
        ///     Get an instance of an Unity Object from the current scene by name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetPrefab<T>(string name) where T : Object
        {
            return (T)GetPrefab(typeof(T), name);
        }

        /// <summary>
        ///     Get all instances of an Unity Object from the current scene by type.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to search for.</param>
        /// <returns></returns>
        public static Dictionary<string, Object> GetPrefabs(Type type)
        {
            return GetCachedMap(type);
        }

        private static Transform GetParent(Object obj)
        {
            return obj is GameObject gameObject ? gameObject.transform.parent : null;
        }

        /// <summary>
        ///     Determines the best matching asset for a given name.
        ///     Only one asset can be associated with a name, this ties to find the best match if there is already a cached one present.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="newObject"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Object FindBestAsset(IDictionary<string, Object> map, Object newObject, string name)
        {
            if (!map.TryGetValue(name, out Object cached))
            {
                return newObject;
            }

            // if a ObjectDB parent exists in the main scene, prefer it over the prefab
            if (name == "_NetScene" && cached is GameObject cachedGo && newObject is GameObject newGo)
            {
                if (!cachedGo.activeInHierarchy && newGo.activeInHierarchy)
                {
                    return newGo;
                }
            }

            if (cached is Material cachedMat && newObject is Material newMat && FindBestMaterial(cachedMat, newMat, out var material))
            {
                return material;
            }

            bool cachedHasParent = GetParent(cached);
            bool newHasParent = GetParent(newObject);

            if (!cachedHasParent && newHasParent)
            {
                // as the cached object has no parent, it is more likely a real prefab and not a child GameObject
                return cached;
            }

            if (cachedHasParent && !newHasParent)
            {
                // as the new object has no parent, it is more likely a real prefab and not a child GameObject
                return newObject;
            }

            return newObject;
        }

        private static bool FindBestMaterial(Material cachedMaterial, Material newMaterial, out Object material)
        {
            string cachedShaderName = cachedMaterial.shader.name;
            string newShaderName = newMaterial.shader.name;

            if (cachedShaderName == "Hidden/InternalErrorShader" && newShaderName != "Hidden/InternalErrorShader")
            {
                material = newMaterial;
                return true;
            }

            if (cachedShaderName != "Hidden/InternalErrorShader" && newShaderName == "Hidden/InternalErrorShader")
            {
                material = cachedMaterial;
                return true;
            }

            material = null;
            return false;
        }

        private static Dictionary<string, Object> GetCachedMap(Type type)
        {
            if (dictionaryCache.TryGetValue(type, out var map))
            {
                return map;
            }
            return InitCache(type);
        }

        private static Dictionary<string, Object> InitCache(Type type)
        {
            Dictionary<string, Object> map = new Dictionary<string, Object>();

            foreach (var unityObject in Resources.FindObjectsOfTypeAll(type))
            {
                string name = unityObject.name;
                map[name] = FindBestAsset(map, unityObject, name);
            }

            dictionaryCache[type] = map;
            return map;
        }

        /// <summary>
        ///     Clears the entire cache, resulting in a rebuilt on the next access.<br />
        ///     This can be useful if an asset is loaded late after a scene change and might be missing in the cache.
        ///     Rebuilding can be an expensive operation, so use with caution.
        /// </summary>
        public static void Clear()
        {
            dictionaryCache.Clear();
        }

        /// <summary>
        ///     Clears the cache for a specific type, resulting in a rebuilt on the next access.<br />
        ///     This can be useful if an asset is loaded late after a scene change and might be missing in the cache.
        ///     Rebuilding can be an expensive operation, so use with caution.
        /// </summary>
        /// <typeparam name="T">The type of object to clear the cache for</typeparam>
        public static void Clear<T>() where T : Object
        {
            dictionaryCache.Remove(typeof(T));
        }
    }
}
