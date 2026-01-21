using System;
using UnityEngine;

public class GridMap : MonoBehaviour
{
    public Vector2 WorldSize = new Vector2(30, 30);
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private bool _drawGizmos = true;
    public Vector3 Origin { get; private set; }
    public int SizeX { get; private set; }
    public int SizeY { get; private set; }

    private Node[,] _nodes;

    public void Build()
    {
        SizeX = Mathf.RoundToInt(WorldSize.x / _cellSize);
        SizeY = Mathf.RoundToInt(WorldSize.y / _cellSize);

        Origin = transform.position - new Vector3(WorldSize.x * 0.5f, 0, WorldSize.y * 0.5f);

        _nodes = new Node[SizeX, SizeY];

        for (int x = 0; x < SizeX; x++)
        for (int y = 0; y < SizeY; y++)
        {
            Vector3 p = CellToWorldCenter(x, y);

            bool blocked = Physics.CheckBox(
                p,
                new Vector3(_cellSize * 0.45f, 0.5f, _cellSize * 0.45f),
                Quaternion.identity,
                _obstacleMask
            );

            _nodes[x, y] = new Node(x, y, p, !blocked);
        }
    }

    public Node GetNode(int x, int y)
    {
        if (x < 0 || y < 0 || x >= SizeX || y >= SizeY) return null;
        return _nodes[x, y];
    }

    public bool WorldToCell(Vector3 world, out int x, out int y)
    {
        Vector3 local = world - Origin;
        x = Mathf.FloorToInt(local.x / _cellSize);
        y = Mathf.FloorToInt(local.z / _cellSize);
        return x >= 0 && y >= 0 && x < SizeX && y < SizeY;
    }

    public Vector3 CellToWorldCenter(int x, int y)
    {
        return Origin + new Vector3((x + 0.5f) * _cellSize, 0f, (y + 0.5f) * _cellSize);
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos || _nodes == null) 
            return;

        for (int x = 0; x < SizeX; x++)
        for (int y = 0; y < SizeY; y++)
        {
            Gizmos.color = _nodes[x, y].Walkable
                ? new Color(0, 1, 0, 0.12f)
                : new Color(1, 0, 0, 0.20f);

            Gizmos.DrawCube(_nodes[x, y].WorldPos + Vector3.up * 0.05f, new Vector3(_cellSize, 0.1f, _cellSize));
        }
    }
}

[Serializable]
public class Node
{
    public int X;
    public int Y;
    public Vector3 WorldPos;
    public bool Walkable;

    public int G;
    public int H;
    public int F => G + H;
    public Node Parent;

    public Node(int x, int y, Vector3 pos, bool walkable)
    {
        X = x; Y = y; WorldPos = pos; Walkable = walkable;
    }

    public void ResetPath()
    {
        G = 0; H = 0; Parent = null;
    }
}
