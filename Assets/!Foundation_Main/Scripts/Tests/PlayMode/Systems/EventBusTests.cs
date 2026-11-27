using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Foundation.Systems.Game;
using UnityEngine;
using UnityEngine.TestTools;
using Foundation.Tests.PlayMode;

public struct Ping
{
    public int Value;
}

public sealed class EventBusProbe : ControlledBehaviour
{
    public bool ResolvedBus;
    public int ReceivedValue;

    private IEventBus eventBus;
    private System.Action<Ping> handler;

    protected override void OnInitialize()
    {
        ResolvedBus = GameEntryPoint.TryResolve(out eventBus);
        if (!ResolvedBus)
            return;

        handler = evt => ReceivedValue = evt.Value;
        eventBus.Subscribe(handler);
    }

    protected override void OnDeInitialize()
    {
        if (ResolvedBus && handler != null)
            eventBus.Unsubscribe(handler);
    }
}

public sealed class PingChannel : AssetEventChannel<Ping>
{
}

public sealed class AssetEventBusListenerProbe : ControlledBehaviour
{
    public bool ResolvedBus;
    public int ReceivedValue { get; private set; }

    private AssetEventChannel<Ping> channel;
    private IEventBus eventBus;
    public void Configure(IEventBus bus, AssetEventChannel<Ping> eventChannel)
    {
        eventBus = bus;
        channel = eventChannel;
    }

    protected override void OnEnable()
    {
        if (eventBus == null || channel == null) return;

        eventBus.Subscribe(channel, HandlePing);

    }
    protected override void OnDisable()
    {
        if (eventBus == null || channel == null) return;

        eventBus.Unsubscribe(channel, HandlePing);
    }

    private void HandlePing(Ping evt)
    {
        ReceivedValue = evt.Value;
    }
}

public sealed class EventBusRegistrationProbe : ControlledBehaviour
{
    public bool ResolvedBus;
    public bool ReceivedEvent { get; private set; }
    public bool WasRegisteredWhenEventReceived { get; private set; }
    public int ReceivedValue { get; private set; }

    private IEventBus eventBus;
    private System.Action<Ping> handler;

    protected override void OnInitialize()
    {
        ResolvedBus = GameEntryPoint.TryResolve(out eventBus);
        if (!ResolvedBus)
            return;

        handler = evt =>
        {
            ReceivedEvent = true;
            WasRegisteredWhenEventReceived = IsRegistered;
            ReceivedValue = evt.Value;
        };
        eventBus.Subscribe(handler);
    }

    protected override void OnDeInitialize()
    {
        if (ResolvedBus && handler != null)
            eventBus.Unsubscribe(handler);
    }
}

public class EventBusTests
{
    private GameController gameController;
    private PauseController pauseController;
    private const int MaxWaitFrames = 60;

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

    [UnityTest]
    public IEnumerator EventBusProbeReceivesPing()
    {
        // Ensure the event bus is resolved before proceeding with the test.
        Assert.That(GameEntryPoint.TryResolve<IEventBus>(out var eventBus), Is.True, "GameEntryPoint failed to resolve IEventBus");

        // Create a new GameObject with the EventBusProbe component to test event reception.
        var probe = new GameObject().AddComponent<EventBusProbe>();
        yield return null; // Wait for initialization
        Assert.That(probe.ResolvedBus, Is.True, "The EventBusProbe failed to resolve the event bus");

        // Publish a ping event to the event bus and verify that the probe receives it.
        var ping = new Ping { Value = 42 };
        eventBus.Publish(ping);

        yield return null;

        // Wait for the next frame to allow the event to be processed by the probe.
        Assert.That(probe.ReceivedValue, Is.EqualTo(42), "EventBusProbe did not receive the ping");

        GameObject.Destroy(probe.gameObject);
    }

    [UnityTest]
    public IEnumerator QueuedEvent_IsDispatchedAfterBehaviourRegistration()
    {
        // GameEntryPoint's BeforeSceneLoad initializer should have created the host.
        Assert.That(GameEntryPoint.TryResolve<IEventBus>(out var eventBus), Is.True, "GameEntryPoint failed to resolve IEventBus");

        var probe = new GameObject().AddComponent<EventBusRegistrationProbe>();

        // Before publishing the queued event, the probe should not be registered and should have resolved the bus.
        Assert.IsTrue(probe.ResolvedBus);
        Assert.IsFalse(probe.IsRegistered);

        // Publish a queued event before the probe has registered itself with the event bus.
        eventBus.PublishQueued(new Ping { Value = 42 });

        yield return null;

        // After yielding, the queued event should have been dispatched to the probe.
        Assert.IsTrue(probe.ReceivedEvent);
        Assert.IsTrue(probe.WasRegisteredWhenEventReceived);
        Assert.AreEqual(42, probe.ReceivedValue);

        GameObject.Destroy(probe.gameObject);
    }

    [UnityTest]
    public IEnumerator AssetChannelListener_ReceivesOnlyWhileEnabled()
    {
        // GameEntryPoint's BeforeSceneLoad initializer should have created the host.
        Assert.That(GameEntryPoint.TryResolve<IEventBus>(out var eventBus), Is.True, "GameEntryPoint failed to resolve IEventBus");

        // Create a new channel and listener for the test.
        var channel = ScriptableObject.CreateInstance<PingChannel>();
        var listenerObject = new GameObject("AssetEventBusListenerProbe");

        // Initially disable the listener object to test that it only receives events while active.
        listenerObject.SetActive(false);

        // Add the listener component to the listener object and configure it with the event bus and channel.
        var listener = listenerObject.AddComponent<AssetEventBusListenerProbe>();
        listener.Configure(eventBus, channel);

        try
        {
            // Enable the listener object and publish an event to verify it receives events while active.
            listenerObject.SetActive(true);
            eventBus.Publish(channel, new Ping { Value = 10 });
            Assert.AreEqual(10, listener.ReceivedValue);

            // Disable the listener component and publish an event to verify it does not receive events while disabled.
            listener.enabled = false;
            eventBus.Publish(channel, new Ping { Value = 20 });
            Assert.AreEqual(10, listener.ReceivedValue);

            // Re-enable the listener component and publish an event to verify it receives events again.
            listener.enabled = true;
            eventBus.Publish(channel, new Ping { Value = 30 });
            Assert.AreEqual(30, listener.ReceivedValue);

            // Disable the listener object and publish an event to verify it does not receive events while inactive.
            listenerObject.SetActive(false);
            eventBus.Publish(channel, new Ping { Value = 40 });
            Assert.AreEqual(30, listener.ReceivedValue);

            yield return null;
        }
        finally
        {
            GameObject.Destroy(listenerObject);
            ScriptableObject.Destroy(channel);
        }
    }
}
