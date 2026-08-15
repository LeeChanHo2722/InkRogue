using UnityEngine;

public class RunManager : MonoBehaviour
{
    [Min(1)]
    public int maxFloor = 3;

    [SerializeField]
    private int currentFloor = 1;

    public int CurrentFloor => currentFloor;

    public bool IsLastFloor =>
        currentFloor >= maxFloor;

    public void AdvanceFloor()
    {
        currentFloor++;
    }
}
