using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class UnitController : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private GameObject _selectRing;
    
    [Header("Move")]
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _turnSpeed = 12f;
    [SerializeField] private float _stopRadius = 0.2f;

    private CharacterController _cc;

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
        _cc = GetComponent<CharacterController>();
        _selectRing?.SetActive(false);
    }
    
    private void Update()
    {
        if (_hasTarget)
            MoveDirect();
    }
    
    private void MoveDirect()
    {
        Vector3 to = _target - transform.position;
        to.y = 0f;

        if (to.magnitude <= _stopRadius)
        {
            _hasTarget = false;
            return;
        }

        Vector3 dir = to.normalized;
        Vector3 vel = dir * _moveSpeed;

        if (vel.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * _turnSpeed);
        }

        _cc.SimpleMove(vel);
    }
}