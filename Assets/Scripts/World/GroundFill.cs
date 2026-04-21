using UnityEngine;

namespace VoxelRoad.World
{
    /// <summary>레인 타일 틈새로 카메라 배경이 보이는 현상 방지.
    /// 플레이어 Z를 따라가는 대형 플레인을 y=-0.1에 유지해 배경을 가림.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GroundFill : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private Color _fillColor = new Color(0.25f, 0.45f, 0.18f); // 잔디와 유사한 녹색
        [SerializeField] private float _sizeX = 300f;
        [SerializeField] private float _sizeZ = 400f;

        private Transform _plane;

        private void Start()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "GroundFill";
            go.transform.SetParent(transform, false);
            // Plane 기본 크기 10x10 → sizeX/10 × sizeZ/10
            go.transform.localScale = new Vector3(_sizeX / 10f, 1f, _sizeZ / 10f);
            go.transform.localPosition = new Vector3(0f, -0.1f, 0f);

            Destroy(go.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", _fillColor);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_EnvironmentReflections", 0f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.GetComponent<MeshRenderer>().receiveShadows = false;

            _plane = go.transform;
        }

        private void LateUpdate()
        {
            if (_player == null || _plane == null) return;
            var p = _plane.position;
            _plane.position = new Vector3(p.x, p.y, _player.position.z);
        }
    }
}
