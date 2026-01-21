using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class UnitController : MonoBehaviour
{
    [SerializeField] private GameObject _selectRing;

    public void SetSelected(bool selected) => _selectRing?.SetActive(selected);
}