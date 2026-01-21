using UnityEngine;

public class SelectManager : MonoBehaviour
{
    public Camera _Camera;
    public LayerMask UnitMask;
    public LayerMask GroundMask;

    private UnitController _selected;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (TrySelectUnit())
                return;

            TryMoveToGround();
        }
    }

    private bool TrySelectUnit()
    {
        Ray ray = _Camera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, UnitMask))
            return false;

        UnitController unit = hit.collider.GetComponentInParent<UnitController>();
        if (!unit) 
            return false;

        if (_selected) 
            _selected.SetSelected(false);
        _selected = unit;
        _selected.SetSelected(true);
        return true;
    }

    private void TryMoveToGround()
    {
        if (!_selected) 
            return;

        Ray ray = _Camera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, GroundMask))
            return;

        _selected.SetDirectTarget(hit.point);
    }
}
