using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class UnitController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Pathfinder _pathfinder;

    [Header("Selection")]
    [SerializeField] private GameObject _selectRing;
    [SerializeField] private LineRenderer _line;

    [Header("Move")]
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _turnSpeed = 12f;
    [SerializeField] private float _waypointReachRadius = 0.25f;

    [Header("Avoidance")]
    [SerializeField] private float _neighborRadius = 1.5f;
    [SerializeField] private float _avoidanceStrength = 10f;
    [SerializeField] private float _lookAhead = 1.1f;
    [SerializeField] private float _lateralStrength = 3.0f;
    [SerializeField] private float _avoidanceMaxSpeedFactor = 0.45f;
    [SerializeField] private LayerMask _unitMask;

    [Header("Replan")]
    [SerializeField] private float _stuckCheckInterval = 0.5f;
    [SerializeField] private float _stuckMinMove = 0.05f;
    [SerializeField] private float _replanCooldown = 0.6f;

    private float _stuckTimer;
    private Vector3 _lastPos;
    private float _replanTimer;

    private CharacterController _characterController;

    private readonly List<Vector3> _path = new List<Vector3>();
    private int _pathIndex;
    private bool _hasTarget;
    private Vector3 _target;

    private float _preferredSide;

    public void SetSelected(bool selected) => _selectRing?.SetActive(selected);

    private void Awake()
    {
        _preferredSide = (GetInstanceID() & 1) == 0 ? 1f : -1f;
        _lastPos = transform.position;
        _characterController = GetComponent<CharacterController>();
        _selectRing?.SetActive(false);
    }

    private void Update()
    {
        _replanTimer -= Time.deltaTime;
        if (!_hasTarget) return;
        MoveAlongPath();
        UpdateStuckAndReplan();
    }

    public void SetTarget(Vector3 target)
    {
        _target = target;
        _hasTarget = true;
        RebuildPath();
    }

    private void MoveAlongPath()
    {
        if (_pathIndex >= _path.Count)
        {
            if ((transform.position - _target).sqrMagnitude <= 0.3f * 0.3f)
                _hasTarget = false;
            DrawPath();
            return;
        }

        Vector3 next = _path[_pathIndex];
        Vector3 to = next - transform.position;
        to.y = 0f;

        if (to.magnitude <= _waypointReachRadius)
        {
            _pathIndex++;
            DrawPath();
            return;
        }

        Vector3 dir = to.normalized;

        Vector3 desired = dir * _moveSpeed;
        Vector3 separation = ComputeSeparation();
        Vector3 forwardAvoid = ComputeForwardAvoidance(dir);
        Vector3 lateral = Vector3.Cross(Vector3.up, dir) * _preferredSide;

        Vector3 velocity =
            desired +
            separation * _avoidanceStrength +
            forwardAvoid * _avoidanceStrength +
            lateral * (forwardAvoid.magnitude * _lateralStrength);

        float conflict = Mathf.Clamp01(separation.magnitude + forwardAvoid.magnitude);
        float maxSpeed = Mathf.Lerp(_moveSpeed, _moveSpeed * _avoidanceMaxSpeedFactor, conflict);

        Vector3 planarVel = new Vector3(velocity.x, 0f, velocity.z);
        if (planarVel.magnitude > maxSpeed)
            planarVel = planarVel.normalized * maxSpeed;

        if (planarVel.sqrMagnitude > 0.0001f)
        {
            Quaternion rot = Quaternion.LookRotation(planarVel.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * _turnSpeed);
        }

        _characterController.SimpleMove(planarVel);
    }

    private Vector3 ComputeSeparation()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _neighborRadius, _unitMask);
        if (hits == null || hits.Length == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;

        foreach (Collider h in hits)
        {
            if (h.transform == transform) continue;

            Vector3 diff = transform.position - h.transform.position;
            diff.y = 0f;

            float d = diff.magnitude;
            if (d < 0.0001f) continue;

            float w = 1f - Mathf.Clamp01(d / _neighborRadius);
            float inv = 1f / (d * d);
            sum += diff.normalized * inv * w;
        }

        return sum;
    }

    private Vector3 ComputeForwardAvoidance(Vector3 desiredDir)
    {
        float radius = _characterController.radius + 0.05f;

        float yBottom = transform.position.y + radius;
        float yTop = transform.position.y + Mathf.Max(radius, _characterController.height - radius);

        Vector3 p1 = new Vector3(transform.position.x, yBottom, transform.position.z);
        Vector3 p2 = new Vector3(transform.position.x, yTop, transform.position.z);

        if (Physics.CapsuleCast(p1, p2, radius, desiredDir, out RaycastHit hit, _lookAhead, _unitMask))
        {
            if (hit.transform != transform)
            {
                Vector3 away = transform.position - hit.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                    return away.normalized;
            }
        }

        return Vector3.zero;
    }

    private void RebuildPath()
    {
        _path.Clear();
        _pathIndex = 0;

        if (_pathfinder && _pathfinder.TryFindPath(transform.position, _target, out var wp))
        {
            _path.AddRange(wp);
        }
        else
        {
            _hasTarget = false;
        }

        DrawPath();
    }

    private void UpdateStuckAndReplan()
    {
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer < _stuckCheckInterval) return;
        _stuckTimer = 0f;

        float moved = (transform.position - _lastPos).magnitude;
        _lastPos = transform.position;

        if (moved < _stuckMinMove && _replanTimer <= 0f)
        {
            _replanTimer = _replanCooldown;
            RebuildPath();
        }
    }

    private void DrawPath()
    {
        if (!_line) return;

        if (!_hasTarget || _path.Count == 0 || _pathIndex >= _path.Count)
        {
            _line.positionCount = 0;
            return;
        }

        int count = (_path.Count - _pathIndex) + 1;
        _line.positionCount = count;

        _line.SetPosition(0, transform.position + Vector3.up * 0.05f);
        for (int i = 0; i < count - 1; i++)
            _line.SetPosition(i + 1, _path[_pathIndex + i] + Vector3.up * 0.05f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _neighborRadius);
    }
}
