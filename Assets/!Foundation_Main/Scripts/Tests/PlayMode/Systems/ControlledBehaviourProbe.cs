using UnityEngine;
using System.Collections;
using Foundation.Systems.Game;
using System;

public class ControlledBehaviourProbe : ControlledBehaviour
{
    private int executionOrder;

    public int UpdateCount { get; private set; }
    public int RegisteredCount { get; private set; }
    public int UnregisteredCount { get; private set; }
    public string Label { get; private set; }

    public Action<ControlledBehaviourProbe> updateAction { get; set; }

    public void Configure(
      string label,
      int order,
      ExecutionPhases phase,
      Action<ControlledBehaviourProbe> updateAction = null)
    {
        Label = label;
        executionOrder = order;
        this.updateAction = updateAction;
        SetExecutionPhase(phase);
    }

    protected override int GetExecutionOrder() => executionOrder;

    protected override void OnRegistered()
    {
        RegisteredCount++;
    }

    protected override void OnUnregistered()
    {
        UnregisteredCount++;
    }

    public override void ControlledUpdate()
    {
        UpdateCount++;
        updateAction?.Invoke(this);
    }
}
