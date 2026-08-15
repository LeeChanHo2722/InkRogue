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

        if (!floorManager.IsLastWaveStarted)
            return;

        if (floorManager.RemainingEnemies > 0)
            return;

        Complete();
    }
}
