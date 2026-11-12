using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Foundation.Systems.Game;
using UnityEngine;
using UnityEngine.TestTools;
using Foundation.Tests.PlayMode;

public class GameControllerTests
{
    public const int MaxWaitFrames = 60;

    private readonly List<GameObject> ownedGameObjects = new();
    private readonly List<PauseController.PauseToken> ownedPauseTokens = new();

    private GameController gameController;
    private PauseController pauseController;

    // Sets up the test environment by resolving the GameController and PauseController from the GameEntryPoint
    // This method will be called before each test to ensure the necessary controllers are available.
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        gameController = null;
        pauseController = null;

        for (int frame = 0; frame < MaxWaitFrames; frame++)
        {
            GameEntryPoint.TryResolve(out gameController);
            GameEntryPoint.TryResolve(out pauseController);

            if (gameController != null && pauseController != null) yield break;

            yield return null;
        }

        Assert.That(gameController, Is.Not.Null, "Runtime bootstrap failed to provide a GameController");
        Assert.That(pauseController, Is.Not.Null, "Runtime bootstrap failed to provide a PauseController");
    }

    // Tears down the test environment by releasing pause tokens and destroying owned game objects.
    // This method will be called after each test to ensure proper cleanup of resources.
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var token in ownedPauseTokens) token.Release();
        ownedPauseTokens.Clear();

        // Deactivate all owned game objects before clearing so pending unregistration or other operations can complete.
        foreach (var go in ownedGameObjects)
        {
            if (go != null) go.SetActive(false);
        }

        yield return null;

        foreach (GameObject owner in ownedGameObjects)
        {
            if (owner != null) UnityEngine.Object.Destroy(owner);
        }

        yield return null;

        ownedGameObjects.Clear();
    }

    // Tests that a behaviour which registers another behaviour during its update is deferred until the next synchronization.
    [UnityTest]
    public IEnumerator EnablingBehaviour_RegistersCorrectly()
    {
        ControlledBehaviourProbe probe = CreateProbe("register-on-enable", 0, ExecutionPhases.Update);

        Assert.That(probe, Is.Not.Null, "Probe should be created successfully");

        Assert.That(probe.IsRegistered, Is.False, "Probe should not be registered initially");

        yield return GenericHelperMethods.WaitFor(() => probe.IsRegistered, "Behaviour did not become registered", MaxWaitFrames);

        Assert.That(probe.RegisteredCount, Is.EqualTo(1), "Probe should have been registered");

        yield return null;

        Assert.That(probe.RegisteredCount, Is.EqualTo(1), "Probe should have been registered exactly once");
    }

    // Tests that a behaviour which unregisters itself during its update is properly unregistered and stops receiving updates.
    [UnityTest]
    public IEnumerator DisablingBehaviour_UnregistersCorrectly_AndStopsReceivingUpdates()
    {
        ControlledBehaviourProbe probe = CreateProbe("unregister-on-disable", 0, ExecutionPhases.Update);

        yield return GenericHelperMethods.WaitFor(() => probe.IsRegistered, "Behaviour did not become registered", MaxWaitFrames);

        probe.gameObject.SetActive(false);

        Assert.That(probe.IsRegistered, Is.True, "Probe should still be registered immediately after being disabled");

        yield return GenericHelperMethods.WaitFor(() => !probe.IsRegistered, "Behaviour did not become unregistered", MaxWaitFrames);

        Assert.That(probe.RegisteredCount, Is.EqualTo(1), "Probe should have been unregistered exactly once");

        int updatesAfterUnregister = probe.UpdateCount;
        yield return null;

        Assert.That(probe.UpdateCount, Is.EqualTo(updatesAfterUnregister), "Probe should not receive updates after being unregistered");
    }

    // Tests that a behaviour which registers another behaviour during its update is deferred until the next synchronization.
    [UnityTest]
    public IEnumerator RegisteredDuringUpdate_IsDeferredUntilNextSynchronization()
    {
        ControlledBehaviourProbe child = null;
        int childUpdatesAtCreation = -1;
        bool childCreated = false;

        CreateProbe("registration-requester", -10, ExecutionPhases.Update, _ =>
        {
            if (childCreated) return;

            child = CreateProbe("register-during-update", 0, ExecutionPhases.Update);
            childUpdatesAtCreation = child.UpdateCount;
            childCreated = true;
        });

        yield return GenericHelperMethods.WaitFor(() => child != null, "Parent behaviour did not create child during update", MaxWaitFrames);

        Assert.That(childUpdatesAtCreation, Is.Zero, "Child should not have received any updates at the moment of creation");

        yield return GenericHelperMethods.WaitFor(() => child.IsRegistered, "Child did not become registered", MaxWaitFrames);

        Assert.That(child.IsRegistered, Is.True, "Child should be registered after the next synchronization");

        yield return GenericHelperMethods.WaitFor(() => child.UpdateCount > childUpdatesAtCreation, "Child did not receive updates after registration", MaxWaitFrames);
    }

    // Tests that a behaviour which unregisters another behaviour during its update is properly unregistered and stops receiving updates.
    [UnityTest]
    public IEnumerator UnregistersAnotherDuringUpdate_StopsReceivingUpdates()
    {
        ControlledBehaviourProbe target = null;
        int targetUpdatesWhenDisabled = -1;

        CreateProbe("disable-requester", -10, ExecutionPhases.Update, _ =>
            {
                if (target == null || !target.gameObject.activeSelf) return;
                targetUpdatesWhenDisabled = target.UpdateCount;
                target.gameObject.SetActive(false);
            });

        target = CreateProbe("disabled-during-update", 10, ExecutionPhases.Update);

        yield return GenericHelperMethods.WaitFor(() => targetUpdatesWhenDisabled >= 0, "Requester did not disable the target during Update", MaxWaitFrames);

        Assert.That(target.UpdateCount, Is.EqualTo(targetUpdatesWhenDisabled), "The inactive target should be skipped later in the same phase.");

        yield return GenericHelperMethods.WaitFor(() => !target.IsRegistered, "Target did not become unregistered", MaxWaitFrames);

        Assert.That(target.UnregisteredCount, Is.EqualTo(1));
    }

    // Tests that destroying a registered behaviour removes it from the scheduler.
    [UnityTest]
    public IEnumerator DestroyingRegisteredBehaviour_RemovesItFromScheduler()
    {
        ControlledBehaviourProbe probe = CreateProbe("to-be-destroyed", 0, ExecutionPhases.Update);

        yield return GenericHelperMethods.WaitFor(() => probe.IsRegistered, "Probe did not become registered", MaxWaitFrames);

        GameObject owner = probe.gameObject;
        UnityEngine.Object.Destroy(owner);

        yield return GenericHelperMethods.WaitFor(() => !probe.IsRegistered, "Probe was not removed from scheduler after destruction", MaxWaitFrames);

        Assert.That(probe.UnregisteredCount, Is.EqualTo(1));
    }

    // Tests that registering and unregistering null behaviours does not throw exceptions.
    [UnityTest]
    public IEnumerator NullRegisterAndUnregister_AreHarmless()
    {
        Assert.DoesNotThrow(() => gameController.Register(null));
        Assert.DoesNotThrow(() => gameController.Unregister(null));

        ControlledBehaviourProbe probe = CreateProbe("register-after-null", 0, ExecutionPhases.Update);

        yield return GenericHelperMethods.WaitFor(() => probe.IsRegistered, "Null request disabled the scheduler and prevented probe registration", MaxWaitFrames);
    }

    // Tests execution by order and preserve registration order for tie-breaking scenarios.
    [UnityTest]
    public IEnumerator Behaviour_ExecutionOrder_Enforcement_And_RegistrationOrder_TieBreaking()
    {
        var executionTrace = new List<string>();

        ControlledBehaviourProbe tie_1 = CreateProbe("tie-1", 5, ExecutionPhases.Update, probe => executionTrace.Add(probe.Label));
        ControlledBehaviourProbe highPriority = CreateProbe("high-priority", 10, ExecutionPhases.Update, probe => executionTrace.Add(probe.Label));
        ControlledBehaviourProbe lowPriority = CreateProbe("low-priority", -1, ExecutionPhases.Update, probe => executionTrace.Add(probe.Label));
        ControlledBehaviourProbe tie_2 = CreateProbe("tie-2", 5, ExecutionPhases.Update, probe => executionTrace.Add(probe.Label));

        yield return GenericHelperMethods.WaitFor(() => tie_1.IsRegistered && highPriority.IsRegistered && lowPriority.IsRegistered && tie_2.IsRegistered, "Probes did not become registered", MaxWaitFrames);

        executionTrace.Clear();

        yield return null; // Wait for one frame to allow execution of the probes

        // Assert the execution order
        Assert.That(executionTrace, Is.EqualTo(new List<string> { "high-priority", "tie-1", "tie-2", "low-priority" }));
    }

    // Test PauseTokens stopping execution of behaviours.
    [UnityTest]
    public IEnumerator PauseToken_Stops_Behaviour_Execution()
    {
        ControlledBehaviourProbe probe = CreateProbe("pause-integration", 0, ExecutionPhases.Update);

        yield return GenericHelperMethods.WaitFor(() => probe.IsRegistered && probe.UpdateCount > 0, "Probe did not become registered or did not update", MaxWaitFrames);

        PauseController.PauseToken pauseToken = pauseController.RequestPause("GameControllerTests");
        ownedPauseTokens.Add(pauseToken);

        yield return null;
        int updatesWhilePaused = probe.UpdateCount;

        Assert.That(updatesWhilePaused, Is.EqualTo(probe.UpdateCount), "Controlled behaviour executed while paused");

        pauseToken.Release();

        yield return GenericHelperMethods.WaitFor(() => probe.UpdateCount > updatesWhilePaused, "Controlled behaviour did not resume after pause was released", MaxWaitFrames);
    }


    #region Helper Methods
    private ControlledBehaviourProbe CreateProbe(
       string label,
       int order,
       ExecutionPhases phase,
       Action<ControlledBehaviourProbe> updateAction = null)
    {
        var owner = new GameObject(label);
        owner.SetActive(false);

        ControlledBehaviourProbe probe =
            owner.AddComponent<ControlledBehaviourProbe>();

        probe.Configure(label, order, phase, updateAction);
        ownedGameObjects.Add(owner);

        owner.SetActive(true);
        return probe;
    }
    #endregion
}
