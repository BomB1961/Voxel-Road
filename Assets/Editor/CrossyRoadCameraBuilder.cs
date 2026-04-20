// Assets/Editor/CrossyRoadCameraBuilder.cs
// Tools/Crossy Road/Setup Camera System 메뉴: 씬의 Cinemachine 카메라 시스템을 원클릭 구성.
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using VoxelRoad.CameraSystem;

public static class CrossyRoadCameraBuilder
{
    [MenuItem("Tools/Crossy Road/Setup Camera System")]
    public static void Setup()
    {
        var vcamGo = GameObject.Find("CM vcam1");
        if (vcamGo == null)
        {
            vcamGo = new GameObject("CM vcam1");
            Undo.RegisterCreatedObjectUndo(vcamGo, "Create CM vcam1");
        }
        if (vcamGo.GetComponent<CinemachineCamera>() == null)
            Undo.AddComponent<CinemachineCamera>(vcamGo);

        var mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
            Undo.AddComponent<CinemachineBrain>(mainCam.gameObject);

        var setup = vcamGo.GetComponent<CrossyRoadCameraSetup>();
        if (setup == null) setup = Undo.AddComponent<CrossyRoadCameraSetup>(vcamGo);
        setup.Apply();

        var perlin = vcamGo.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (perlin == null) perlin = Undo.AddComponent<CinemachineBasicMultiChannelPerlin>(vcamGo);
        if (perlin.NoiseProfile == null)
        {
            var guids = AssetDatabase.FindAssets("Handheld_mild t:NoiseSettings");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("Handheld t:NoiseSettings");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:NoiseSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                perlin.NoiseProfile = AssetDatabase.LoadAssetAtPath<NoiseSettings>(path);
            }
        }
        // 상시 흔들림 방지: 기본 진폭 0. 착지 이벤트가 일시적으로만 진폭을 올린다.
        perlin.AmplitudeGain = 0f;
        perlin.FrequencyGain = 1f;

        EditorUtility.SetDirty(vcamGo);
        Debug.Log("✅ Crossy Road Camera System 구성 완료");
    }
}
