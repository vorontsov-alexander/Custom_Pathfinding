using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class UnitController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Pathfinder _pathfinder;
    
    [Header("Selection")]
    [SerializeField] private GameObject _selectRing;
    
    [Header("Move")]
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _turnSpeed = 12f;
    [SerializeField] private float _waypointReachRadius = 0.25f;

    private CharacterController _characterController;

    private readonly List<Vector3> _path = new List<Vector3>();
    private int _pathIndex;
    private bool _hasTarget;
    private Vector3 _target;
    
    public void SetSelected(bool selected) => _selectRing?.SetActive(selected);

    public void SetDirectTarget(Vector3 target)
    {
        _target = target;
        _hasTarget = true;
    }
    
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _selectRing?.SetActive(false);
    }
    
    private void Update()
    {
        if (_hasTarget)
            MoveAlongPath();
    }
    
    public void SetTarget(Vector3 target)
    {
        _target = target;
        _hasTarget = true;

        _path.Clear();
        _pathIndex = 0;

        if (_pathfinder != null && _pathfinder.TryFindPath(transform.position, _target, out var wp))
        {
            _path.AddRange(wp);
        }
        else
        {
            _hasTarget = false;
        }
    }

    private void MoveAlongPath()
    {
        if (_pathIndex >= _path.Count)
        {
            // дошли до конца
            if ((transform.position - _target).sqrMagnitude <= 0.3f * 0.3f)
                _hasTarget = false;
            return;
        }

        Vector3 next = _path[_pathIndex];
        Vector3 to = next - transform.position;
        to.y = 0f;

        if (to.magnitude <= _waypointReachRadius)
        {
            _pathIndex++;
            return;
        }

        Vector3 dir = to.normalized;
        Vector3 vel = dir * _moveSpeed;

        if (vel.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * _turnSpeed);
        }

        _characterController.SimpleMove(vel);
    }
}