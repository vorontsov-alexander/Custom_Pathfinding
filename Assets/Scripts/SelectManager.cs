using UnityEngine;

public class SelectManager : MonoBehaviour
{
    public Camera cam;
    public LayerMask unitMask;
    public LayerMask groundMask;

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
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, unitMask))
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

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask))
            return;

        _selected.SetDirectTarget(hit.point);
    }
}
