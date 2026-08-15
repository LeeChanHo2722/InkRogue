using System;
using UnityEngine;

public abstract class FloorObjective : MonoBehaviour
{
    public event Action Completed;

    private bool completed;

    public void Begin()
    {
        completed = false;
    }

    public abstract void Evaluate();

    protected void Complete()
    {
        if (completed)
            return;

        completed = true;
        Completed?.Invoke();
    }
}
