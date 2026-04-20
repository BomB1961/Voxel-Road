using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

/// <summary>
/// 스와이프·탭·키보드 입력을 수신해 MoveDirection 이벤트로 변환.
/// EnhancedTouch API 사용 (New Input System 1.19).
/// </summary>
public class InputReader : MonoBehaviour, IInputReader
{
    [SerializeField] private InputConfigSO _config;

    public event System.Action<MoveDirection> OnMoveInput;

    private Vector2 _touchStart;
    private bool _touching;

    private float SwipeMin => _config != null ? _config.SwipeMinDistancePx : 50f;

    private void OnEnable() => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Update()
    {
        ReadTouchInput();
        ReadKeyInput();
    }

    private void ReadTouchInput()
    {
        var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        for (int i = 0; i < touches.Count; i++)
        {
            var t = touches[i];
            // UnityEngine.InputSystem.TouchPhase 로 명시
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _touchStart = t.screenPosition;
                _touching = true;
            }
            else if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended && _touching)
            {
                _touching = false;
                Vector2 delta = t.screenPosition - _touchStart;
                if (delta.magnitude >= SwipeMin)
                    EmitSwipe(delta);
                else
                    OnMoveInput?.Invoke(MoveDirection.Forward); // 탭 = 전진
            }
        }
    }

    private void ReadKeyInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            OnMoveInput?.Invoke(MoveDirection.Forward);
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
            OnMoveInput?.Invoke(MoveDirection.Backward);
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
            OnMoveInput?.Invoke(MoveDirection.Left);
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
            OnMoveInput?.Invoke(MoveDirection.Right);
    }

    // 스와이프 방향 결정: 절댓값 더 큰 축이 우선
    private void EmitSwipe(Vector2 delta)
    {
        MoveDirection dir;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            dir = delta.x > 0 ? MoveDirection.Right : MoveDirection.Left;
        else
            dir = delta.y > 0 ? MoveDirection.Forward : MoveDirection.Backward;
        OnMoveInput?.Invoke(dir);
    }
}
