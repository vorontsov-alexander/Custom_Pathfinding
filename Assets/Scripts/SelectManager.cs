using UnityEngine;

public class SelectManager : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;

    [Header("Masks")]
    public LayerMask unitMask;

    private UnitController _selected;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectUnit();
        }
    }

    private void TrySelectUnit()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, unitMask))
            return;

        UnitController unit = hit.collider.GetComponentInParent<UnitController>();
        if (!unit) 
            return;

        if (_selected)
        {
            _selected.SetSelected(false);
        }
        _selected = unit;
        _selected.SetSelected(true);
    }
}
