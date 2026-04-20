using UnityEngine;

/// <summary>입력 설정 ScriptableObject — 스와이프 임계값 조정</summary>
[CreateAssetMenu(fileName = "InputConfig", menuName = "VoxelRoad/InputConfig")]
public class InputConfigSO : ScriptableObject
{
    [SerializeField, Tooltip("스와이프 인식 최소 픽셀 거리")]
    private float _swipeMinDistancePx = 50f;

    public float SwipeMinDistancePx => _swipeMinDistancePx;
}
