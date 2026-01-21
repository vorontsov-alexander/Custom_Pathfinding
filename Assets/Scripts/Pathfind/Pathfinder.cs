using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [SerializeField] private GridMap _grid;
    [SerializeField] private bool _allowDiagonals = true;

    [Header("Dynamic units on grid")]
    [SerializeField] private LayerMask _unitMask;
    [SerializeField] private float _unitBlockRadius = 0.35f;
    [SerializeField] private float _unitInfluenceRadius = 1.2f;
    [SerializeField] private int _unitPenalty = 60;

    private static readonly Collider[] _tmp = new Collider[32];

    private static readonly (int dx, int dy)[] Neigh4 =
    {
        (1,0), (-1,0), (0,1), (0,-1)
    };

    private static readonly (int dx, int dy)[] Neigh8 =
    {
        (1,0), (-1,0), (0,1), (0,-1),
        (1,1), (1,-1), (-1,1), (-1,-1)
    };

    public bool TryFindPath(Vector3 startWorld, Vector3 targetWorld, out List<Vector3> waypoints)
    {
        waypoints = null;
        if (!_grid) return false;

        if (!_grid.WorldToCell(startWorld, out int sx, out int sy)) return false;
        if (!_grid.WorldToCell(targetWorld, out int tx, out int ty)) return false;

        Node start = _grid.GetNode(sx, sy);
        Node target = _grid.GetNode(tx, ty);

        if (start == null || target == null) return false;
        if (!target.Walkable) return false;

        List<Node> open = new List<Node>();
        HashSet<Node> openSet = new HashSet<Node>();
        HashSet<Node> closed = new HashSet<Node>();

        start.ResetPath();
        target.ResetPath();

        open.Add(start);
        openSet.Add(start);

        while (open.Count > 0)
        {
            Node current = open[0];
            for (int i = 1; i < open.Count; i++)
            {
                Node n = open[i];
                if (n.F < current.F || (n.F == current.F && n.H < current.H))
                    current = n;
            }

            open.Remove(current);
            openSet.Remove(current);
            closed.Add(current);

            if (current == target)
            {
                waypoints = RetraceAndSimplify(start, target);
                return waypoints.Count > 0;
            }

            var neigh = _allowDiagonals ? Neigh8 : Neigh4;

            foreach (var (dx, dy) in neigh)
            {
                Node nb = _grid.GetNode(current.X + dx, current.Y + dy);
                if (nb == null || !nb.Walkable || closed.Contains(nb)) continue;

                if (_allowDiagonals && dx != 0 && dy != 0)
                {
                    Node a = _grid.GetNode(current.X + dx, current.Y);
                    Node b = _grid.GetNode(current.X, current.Y + dy);
                    if ((a != null && !a.Walkable) || (b != null && !b.Walkable))
                        continue;
                }

                if (IsCellOccupiedByUnit(nb.WorldPos, startWorld)) 
                    continue;

                int moveCost = current.G + StepCost(current, nb, startWorld);

                if (!openSet.Contains(nb) || moveCost < nb.G)
                {
                    nb.Parent = current;
                    nb.G = moveCost;
                    nb.H = Heuristic(nb, target);

                    if (!openSet.Contains(nb))
                    {
                        open.Add(nb);
                        openSet.Add(nb);
                    }
                }
            }
        }

        return false;
    }

    private bool IsCellOccupiedByUnit(Vector3 cellWorld, Vector3 selfWorld)
    {
        int count = Physics.OverlapSphereNonAlloc(cellWorld, _unitBlockRadius, _tmp, _unitMask);
        for (int i = 0; i < count; i++)
        {
            var c = _tmp[i];
            if (!c) continue;
            if ((c.transform.position - selfWorld).sqrMagnitude < 0.0001f) continue;
            return true;
        }
        return false;
    }

    private int UnitPenaltyAround(Vector3 cellWorld, Vector3 selfWorld)
    {
        int count = Physics.OverlapSphereNonAlloc(cellWorld, _unitInfluenceRadius, _tmp, _unitMask);
        if (count == 0) return 0;

        int penalty = 0;
        float r = _unitInfluenceRadius;

        for (int i = 0; i < count; i++)
        {
            var c = _tmp[i];
            if (!c) continue;
            if ((c.transform.position - selfWorld).sqrMagnitude < 0.0001f) continue;

            float d = Vector3.Distance(cellWorld, c.transform.position);
            float t = Mathf.Clamp01(1f - d / r);
            penalty += Mathf.RoundToInt(_unitPenalty * t * t);
        }

        return penalty;
    }

    private int Heuristic(Node a, Node b)
    {
        int dx = Mathf.Abs(a.X - b.X);
        int dy = Mathf.Abs(a.Y - b.Y);
        int diag = Mathf.Min(dx, dy);
        int straight = Mathf.Abs(dx - dy);
        return diag * 14 + straight * 10;
    }

    private int StepCost(Node from, Node to, Vector3 selfWorld)
    {
        bool diagonal = (from.X != to.X) && (from.Y != to.Y);
        int baseCost = diagonal ? 14 : 10;

        int unitPenalty = UnitPenaltyAround(to.WorldPos, selfWorld);
        return baseCost + unitPenalty;
    }

    private List<Vector3> RetraceAndSimplify(Node start, Node end)
    {
        var nodes = new List<Node>();
        Node cur = end;

        while (cur != null && cur != start)
        {
            nodes.Add(cur);
            cur = cur.Parent;
        }

        nodes.Reverse();

        var wp = new List<Vector3>();
        if (nodes.Count == 0) return wp;

        Vector2Int prevDir = Vector2Int.zero;

        for (int i = 0; i < nodes.Count; i++)
        {
            Vector2Int dir = Vector2Int.zero;
            if (i > 0)
                dir = new Vector2Int(nodes[i].X - nodes[i - 1].X, nodes[i].Y - nodes[i - 1].Y);

            if (i == 0 || dir != prevDir || i == nodes.Count - 1)
                wp.Add(nodes[i].WorldPos);

            prevDir = dir;
        }

        return wp;
    }
}
