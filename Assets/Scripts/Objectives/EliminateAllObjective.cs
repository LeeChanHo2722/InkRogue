public sealed class EliminateAllObjective : FloorObjective
{
    private FloorManager floorManager;

    private void Awake()
    {
        floorManager = GetComponent<FloorManager>();
    }

    public override void Evaluate()
    {
        if (floorManager == null)
            return;

        if (!floorManager.IsFloorCombatComplete)
            return;

        Complete();
    }
}
