using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Managers;
using UnityEngine;
using Object = UnityEngine.Object;
using Logger = Managers.Logger;

namespace ItemManager;

public partial class Item
{
    public static List<Item> RegisteredItems
    {
        get
        {
            return registeredItems;
        }
    }

    public ItemDrop.ItemData ItemData
    {
        get
        {
            return this.Prefab.GetComponent<ItemDrop>().m_itemData;
        }
    }

    public ItemDrop.ItemData.SharedData ItemSharedData
    {
        get
        {
            return this.ItemData.m_shared;
        }
    }

    //public static GameObject CreateClonePrefab(GameObject prefab, string newName)
    //{
    //    var itemDrop = prefab.GetComponent<ItemDrop>();

    //    if (itemDrop == null) { return null; }

    //    string newKey = "$" + newName.ToLower();

    //    var clone = Object.Instantiate<GameObject>(prefab, Main.GetRootObject().transform);
    //    clone.SetActive(false);
    //    clone.name = newName;
    //    var oldItemDrop = clone.GetComponent<ItemDrop>();
    //    var newItemDrop = clone.AddComponent<ItemDrop>();
    //    Object.DestroyImmediate(oldItemDrop);
    //    newItemDrop.m_itemData = new ItemDrop.ItemData()
    //    {
    //        m_shared = new ItemDrop.ItemData.SharedData()
    //    };
    //    Utilities.Copy(itemDrop.m_itemData, ref newItemDrop.m_itemData);
    //    Utilities.Copy(itemDrop.m_itemData.m_shared, ref newItemDrop.m_itemData.m_shared);
    //    newItemDrop.m_itemData.m_shared.m_name = newKey;
    //    newItemDrop.m_itemData.m_shared.m_description = newKey + "_description";
    //    clone.SetActive(true);

    //    return clone;
    //}

    public Item(string prefabToClone, string prefabName)
    {
        if (string.IsNullOrEmpty(prefabToClone)) throw new ArgumentException("param cannot be null or empty", nameof(prefabToClone));
        if (string.IsNullOrEmpty(prefabName)) throw new ArgumentException("param cannot be null or empty", nameof(prefabName));

        var prefab = PrefabManager.GetPrefab(prefabToClone);
        if (prefab == null) throw new ArgumentException($"Unable to find prefab {prefabToClone}");
        var clone = Utilities.CreateClonePrefab(prefab, prefabName);


        PrefabManager.RegisterPrefab(clone, true);    
        Prefab = clone;
        registeredItems.Add(this);
        itemDropMap [Prefab.GetComponent<ItemDrop>()] = this;
        Prefab.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = Prefab;
    }


    public void ToggleAllActiveRecipes(bool isEnabled)
    {
        if (activeRecipes.ContainsKey(this)) {
            foreach(var kv in activeRecipes[this])
            {
                foreach(var item in kv.Value)
                {
                    item.m_enabled = isEnabled;
                }
            }
        }
    }

    public enum RecipesEnabled
    {
        False,
        True,
        Mixed
    }

    public RecipesEnabled GetActiveRecipesEnabled()
    {
        bool allTrue = true;
        bool allFalse = false;
        if (activeRecipes.ContainsKey(this))
        {
            foreach (var kv in activeRecipes[this])
            {
                foreach (var item in kv.Value)
                {
                    allTrue &= item.m_enabled;
                    allFalse |= item.m_enabled;
                }
            }
        }
        if (allTrue && allFalse)
            return RecipesEnabled.True;
        if (!allTrue && !allFalse)
            return RecipesEnabled.False;
        return RecipesEnabled.Mixed;
    }


    //private bool isObjectWithinCameraView(Camera camera, GameObject obj)
    //{
    //    if (camera == null || obj == null) return false;

    //    Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
    //    Bounds b = Utilities.GetBounds(obj);

    //    return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), b);
    //}

    //public static void FocusOn(Camera camera, GameObject focus, float marginPercentage)
    //{
    //    Bounds bounds = Utilities.GetBounds(focus);
    //    float maxExtent = bounds.extents.magnitude;
    //    float minDistance = (maxExtent * marginPercentage) / Mathf.Sin(Mathf.Deg2Rad * camera.fieldOfView / 2f);
    //    camera.transform.position = focus.transform.position - Vector3.forward * minDistance;
    //    camera.nearClipPlane = minDistance - maxExtent;
    //}


    public void Snapshot(float lightIntensity = 1.3f, Quaternion? cameraRotation = null) => SnapshotPiece(Prefab, lightIntensity, cameraRotation);

    internal void SnapshotPiece(GameObject prefab, float lightIntensity = 1.3f, Quaternion? cameraRotation = null)
    {
        void Do()
        {
            const int layer = 3;
            if (prefab == null) return;
            if (!prefab.GetComponentsInChildren<Renderer>().Any() && !prefab.GetComponentsInChildren<MeshFilter>().Any())
            {
                return;
            }

            Camera camera = new GameObject("CameraIcon", typeof(Camera)).GetComponent<Camera>();
            camera.backgroundColor = Color.clear;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(10000f, 10000f, 10000f);
            camera.transform.rotation = cameraRotation ?? Quaternion.Euler(0f, 180f, 0f);
            camera.fieldOfView = 0.5f;
            camera.farClipPlane = 100000;
            camera.cullingMask = 1 << layer;

            Light sideLight = new GameObject("LightIcon", typeof(Light)).GetComponent<Light>();
            sideLight.transform.position = new Vector3(10000f, 10000f, 10000f);
            sideLight.transform.rotation = Quaternion.Euler(5f, 180f, 5f);
            sideLight.type = LightType.Directional;
            sideLight.cullingMask = 1 << layer;
            sideLight.intensity = lightIntensity;

            GameObject visual = Object.Instantiate(prefab);
            foreach (Transform child in visual.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = layer;
            }

            visual.transform.position = Vector3.zero;
            visual.transform.rotation = Quaternion.Euler(23, 51, 25.8f);
            visual.name = prefab.name;

            MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>();
            Vector3 min = renderers.Aggregate(Vector3.positiveInfinity,
                (cur, renderer) => Vector3.Min(cur, renderer.bounds.min));
            Vector3 max = renderers.Aggregate(Vector3.negativeInfinity,
                (cur, renderer) => Vector3.Max(cur, renderer.bounds.max));
            // center the prefab
            visual.transform.position = (new Vector3(10000f, 10000f, 10000f)) - (min + max) / 2f;
            Vector3 size = max - min;

            // just in case it doesn't gets deleted properly later
            TimedDestruction timedDestruction = visual.AddComponent<TimedDestruction>();
            timedDestruction.Trigger(1f);
            Rect rect = new(0, 0, 128, 128);
            camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);

            camera.fieldOfView = 20f;
            // calculate the Z position of the prefab as it needs to be far away from the camera
            float maxMeshSize = Mathf.Max(size.x, size.y) + 0.1f;
            float distance = maxMeshSize / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad) * 1.1f;

            camera.transform.position = (new Vector3(10000f, 10000f, 10000f)) + new Vector3(0, 0, distance);

            camera.Render();

            RenderTexture currentRenderTexture = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;

            Texture2D previewImage = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            previewImage.ReadPixels(new Rect(0, 0, (int)rect.width, (int)rect.height), 0, 0);
            previewImage.Apply();

            RenderTexture.active = currentRenderTexture;

            if (prefab.TryGetComponent<Piece>(out Piece piece))
            {
                piece.m_icon = Sprite.Create(previewImage, new Rect(0, 0, (int)rect.width, (int)rect.height), Vector2.one / 2f);
            }
            else if (prefab.TryGetComponent<ItemDrop>(out ItemDrop itemDrop))
            {
                itemDrop.m_itemData.m_shared.m_icons = new[] { Sprite.Create(previewImage, new Rect(0, 0, (int)rect.width, (int)rect.height), Vector2.one / 2f) };
            }

            sideLight.gameObject.SetActive(false);
            camera.targetTexture.Release();
            camera.gameObject.SetActive(false);
            visual.SetActive(false);
            Object.DestroyImmediate(visual);

            Object.Destroy(camera);
            Object.Destroy(sideLight);
            Object.Destroy(camera.gameObject);
            Object.Destroy(sideLight.gameObject);
        }

        IEnumerator Delay()
        {
            yield return null;
            Do();
        }
        if (ObjectDB.instance)
        {
            Do();
        }
        else
        {
            Main.Mod.StartCoroutine(Delay());
        }
    }



    //public void Snapshot(Vector3? itemPosition = null, Quaternion? itemRotation = null, Vector3? itemScale = null)
    //{
    //    SnapshotItem(this.Prefab.GetComponent<ItemDrop>(), itemPosition, itemRotation, itemScale);
    //}

    //private static void SnapshotItem(ItemDrop item,Vector3? itemPosition = null, Quaternion? itemRotation = null, Vector3? itemScale = null)
    //{
    //    void Do()
    //    {
    //        const int layer = 30;

    //        Camera camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
    //        camera.backgroundColor = Color.clear;
    //        camera.clearFlags = CameraClearFlags.SolidColor;
    //        camera.fieldOfView = 0.5f;
    //        camera.farClipPlane = 10000000;
    //        camera.cullingMask = 1 << layer;

    //        Light topLight = new GameObject("Light", typeof(Light)).GetComponent<Light>();
    //        topLight.transform.position = new Vector3(0, 5f, 0);
    //        topLight.transform.rotation = Quaternion.Euler(0, -5f, 0);
    //        topLight.type = LightType.Directional;
    //        topLight.cullingMask = 1 << layer;
    //        topLight.intensity = 1.25f;

    //        Rect rect = new Rect(0, 0, 64, 64);

    //        GameObject visual = UnityEngine.Object.Instantiate(item.gameObject);
    //        visual.transform.rotation = itemRotation.HasValue ? itemRotation.Value : Quaternion.Euler(20, -150, 40);
    //        visual.transform.localPosition = itemPosition.HasValue ? itemPosition.Value : new Vector3(0, 0, 0);
    //        visual.transform.localScale = itemScale.HasValue ? itemScale.Value : new Vector3(1, 1, 1);
    //        foreach (Transform child in visual.GetComponentsInChildren<Transform>())
    //        {
    //            child.gameObject.layer = layer;
    //        }



    //        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
    //        Vector3 min = renderers.Aggregate(Vector3.positiveInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Min(cur, renderer.bounds.min));
    //        Vector3 max = renderers.Aggregate(Vector3.negativeInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Max(cur, renderer.bounds.max));
    //        Vector3 size = max - min;

    //        camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
    //        float zDist = Mathf.Max(size.x, size.y) * 1.05f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad);
    //        Transform transform = camera.transform;
    //        transform.position = (min + max) / 2 + new Vector3(transform.position.x, transform.position.y, -zDist);

    //        //FocusOn(camera, visual, 1f);

    //        camera.Render();

    //        RenderTexture currentRenderTexture = RenderTexture.active;
    //        RenderTexture.active = camera.targetTexture;

    //        Texture2D texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.ARGB32, false);
    //        texture.ReadPixels(rect, 0, 0);
    //        texture.Apply();

    //        RenderTexture.active = currentRenderTexture;

    //        item.m_itemData.m_shared.m_icons = new[] { Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f)) };

    //        if (item.m_itemData.m_shared.m_equipStatusEffect != null)
    //        {
    //            item.m_itemData.m_shared.m_equipStatusEffect.m_icon = item.m_itemData.m_shared.m_icons[0];
    //        }

    //        UnityEngine.Object.DestroyImmediate(visual);
    //        camera.targetTexture.Release();

    //        UnityEngine.Object.Destroy(camera);
    //        UnityEngine.Object.Destroy(topLight);
    //    }
    //    IEnumerator Delay()
    //    {
    //        yield return null;
    //        Do();
    //    }
    //    if (ObjectDB.instance)
    //    {
    //        Do();
    //    }
    //    else
    //    {
    //        Main.Mod.StartCoroutine(Delay());
    //    }

    //}


}
