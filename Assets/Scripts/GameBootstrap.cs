using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GridMap grid;

    private void Start()
    { 
        grid?.Build();
    }
}