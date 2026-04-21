using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelRoad.Common
{
    [DisallowMultipleComponent]
    public sealed class BlobShadow : MonoBehaviour
    {
        [SerializeField] private float _radius = 0.45f;
        [SerializeField] private float _opacity = 0.35f;

        private static Texture2D s_tex;

        private void Awake()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = ShadowCastingMode.Off;
            AddShadowQuad();
        }

        private void AddShadowQuad()
        {
            EnsureTexture();

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "_BlobShadow";
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localPosition = new Vector3(0f, 0.02f - transform.position.y, 0f);
            float d = _radius * 2f;
            go.transform.localScale = new Vector3(d, d, 1f);

            Destroy(go.GetComponent<Collider>());

            // Sprites/Default: 별도 설정 없이 알파 투명도를 지원하는 셰이더
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = s_tex;
            mat.color = new Color(0f, 0f, 0f, _opacity);

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = -1;
        }

        private static void EnsureTexture()
        {
            if (s_tex != null) return;
            const int size = 64;
            s_tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            s_tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - dist);
                    pixels[y * size + x] = new Color(0f, 0f, 0f, a * a);
                }
            s_tex.SetPixels(pixels);
            s_tex.Apply();
        }
    }
}
