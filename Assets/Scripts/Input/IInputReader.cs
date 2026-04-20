/// <summary>이동 입력 이벤트 소비자 인터페이스</summary>
public interface IInputReader
{
    event System.Action<MoveDirection> OnMoveInput;
}
