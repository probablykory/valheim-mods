using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreJewelry
{
    public static class Icons
    {
        public static void SnapshotItem(ItemDrop item, Quaternion? quat = null)
        {
            IEnumerator Do()
            {
                yield return null;

                const int layer = 30;

                Quaternion itemRotation = quat.HasValue ? quat.Value : Quaternion.Euler(20, -150, 40);

                Camera camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
                camera.backgroundColor = Color.clear;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.fieldOfView = 0.5f;
                camera.farClipPlane = 10000000;
                camera.cullingMask = 1 << layer;

                Light topLight = new GameObject("Light", typeof(Light)).GetComponent<Light>();
                topLight.transform.rotation = Quaternion.Euler(5f, -5f, 5f);
                topLight.type = LightType.Directional;
                topLight.cullingMask = 1 << layer;
                topLight.intensity = 1.25f;

                Rect rect = new Rect(0, 0, 64, 64);

                GameObject visual = Object.Instantiate(item.transform.Find("attach").gameObject);
                visual.transform.rotation = itemRotation;
                foreach (Transform child in visual.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = layer;
                }

                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
                Vector3 min = renderers.Aggregate(Vector3.positiveInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Min(cur, renderer.bounds.min));
                Vector3 max = renderers.Aggregate(Vector3.negativeInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Max(cur, renderer.bounds.max));
                Vector3 size = max - min;

                camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                float zDist = Mathf.Max(size.x, size.y) * 1.05f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad);
                Transform transform = camera.transform;
                transform.position = (min + max) / 2 + new Vector3(0, 0, -zDist);

                camera.Render();

                RenderTexture currentRenderTexture = RenderTexture.active;
                RenderTexture.active = camera.targetTexture;

                Texture2D texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.ARGB32, false);
                texture.ReadPixels(rect, 0, 0);
                texture.Apply();

                RenderTexture.active = currentRenderTexture;

                item.m_itemData.m_shared.m_icons = new[] { Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f)) };

                if (item.m_itemData.m_shared.m_equipStatusEffect != null)
                {
                    item.m_itemData.m_shared.m_equipStatusEffect.m_icon = item.m_itemData.m_shared.m_icons[0];
                }

                Object.DestroyImmediate(visual);
                camera.targetTexture.Release();

                Object.Destroy(camera);
                Object.Destroy(topLight);
            }

            MoreJewelry.Instance?.StartCoroutine(Do());
        }
    }
}