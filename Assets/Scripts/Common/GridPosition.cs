using UnityEngine;

/// <summary>
/// 정수 그리드 좌표 (X=좌우, Z=앞뒤). GC-free readonly struct.
/// </summary>
public readonly struct GridPosition : System.IEquatable<GridPosition>
{
    public int X { get; }
    public int Z { get; }

    public GridPosition(int x, int z) { X = x; Z = z; }

    /// <summary>delta 만큼 이동한 새 좌표 반환</summary>
    public GridPosition Move(int dx, int dz) => new GridPosition(X + dx, Z + dz);

    /// <summary>타일 크기 기준 월드 좌표 반환 (Y=0)</summary>
    public Vector3 ToWorldPosition(float tileSize = 1f) =>
        new Vector3(X * tileSize, 0f, Z * tileSize);

    public bool Equals(GridPosition other) => X == other.X && Z == other.Z;
    public override bool Equals(object obj) => obj is GridPosition g && Equals(g);
    public override int GetHashCode() => X * 1000003 ^ Z;
    public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);
    public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);
    public override string ToString() => $"({X}, {Z})";
}
