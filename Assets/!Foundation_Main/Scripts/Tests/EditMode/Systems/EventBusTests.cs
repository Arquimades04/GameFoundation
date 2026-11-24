using NUnit.Framework;
using UnityEngine;
using Foundation.Systems.Game;


public class EventBusTests
{
    private struct Ping
    {
        public int Value;
    }

    private struct Pong
    {
        public int Value;
    }

    private GameObject host;
    private EventBus eventBus;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject();
        var debugController = host.AddComponent<DebugController>();
        eventBus = new EventBus();
        eventBus.Initialize(host.GetComponent<DebugController>());
    }
    
    [TearDown]
    public void TearDown()
    {
        eventBus.Shutdown();
        Object.DestroyImmediate(host);
    }

    [Test]
    public void Publish_Single_Subscriber()
    {
        int receivedValue = 0;

        // Arrange handlers for the Ping event
        System.Action<Ping> handler = null;
        handler = ping => receivedValue = ping.Value;

        // Subscribe the first handler and publish an event to verify it works
        eventBus.Subscribe(handler);
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(1, receivedValue);
    }

    [Test]
    public void Unsubscribe_And_Publish_Without_Subscribers()
    {
        int receivedValue = 0;

        // Arrange handlers for the Ping event
        System.Action<Ping> handler = null;
        handler = ping => receivedValue = ping.Value;

        // Subscribe the first handler and publish an event to verify it works
        eventBus.Subscribe(handler);
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(1, receivedValue);

        // Unsubscribe the first handler and verify only the second handler is invoked
        eventBus.Unsubscribe(handler);
        receivedValue = 0;
        eventBus.Publish(new Ping { Value = 3 });
        Assert.AreEqual(0, receivedValue);
    }

    [Test]
    public void Publish_To_Subscribers_And_Unsubscribe()
    {
        int calls = 0;

        // Arrange handlers for the Ping event
        System.Action<Ping> handler1 = null;
        System.Action<Ping> handler2 = null;
        handler1 = ping => calls++;
        handler2 = ping => calls++;

        // Subscribe the first handler and publish an event to verify it works
        eventBus.Subscribe(handler1);
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(1, calls);

        eventBus.Subscribe(handler2);
        // Publish another event and verify both handlers are invoked
        calls = 0;
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(2, calls);

        // Unsubscribe the first handler and verify only the second handler is invoked
        eventBus.Unsubscribe(handler1);
        calls = 0;
        eventBus.Publish(new Ping { Value = 3 });
        Assert.AreEqual(1, calls);

        // Unsubscribe the second handler and verify no handlers are invoked
        eventBus.Unsubscribe(handler2);
        calls = 0;
        eventBus.Publish(new Ping { Value = 4 });
        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Subscribe_During_Dispatch()
    {
        int calls = 0;

        System.Action<Ping> handler1 = null;
        System.Action<Ping> handler2 = null;
        handler1 = ping =>
        {
            calls++;
            eventBus.Subscribe(handler2);
        };
        handler2 = ping =>
        {
            calls++;
        };

        // Subscribe the first handler and publish an event to trigger it, should also subscribe the second handler after current publish
        eventBus.Subscribe(handler1);
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(1, calls);

        // Publish another event to verify the new subscription takes effect
        calls = 0;
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(2, calls);
    }

    [Test]
    public void Unsubscribe_During_Dispatch()
    {
        int calls = 0;

        System.Action<Ping> handler = null;
        handler = ping =>
        {
            calls++;
            eventBus.Unsubscribe(handler);
        };


        // Subscribe the first handler and publish an event to trigger it, should also unsubscribe itself during the dispatch
        eventBus.Subscribe(handler);
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(1, calls);

        // Publish another event to verify the handler has been unsubscribed
        calls = 0;
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Duplicate_Subscription_During_Dispatch()
    {
        int calls = 0;

        System.Action<Ping> handler = null;
        handler = ping =>
        {
            calls++;
            eventBus.Subscribe(handler);
        };

        // Subscribe the handler and publish an event to trigger it, should also attempt to subscribe itself again
        eventBus.Subscribe(handler);
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(1, calls);

        // Publish another event to verify the duplicate subscription does not cause multiple calls
        calls = 0;
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(1, calls);
    }


    [Test]
    public void Publish_Throws_And_Continues()
    {
        int calls = 0;

        System.Action<Ping> handler1 = ping =>
        {
            throw new System.Exception("Handler exception");
        };

        System.Action<Ping> handler2 = ping =>
        {
            calls++;
        };

        eventBus.Subscribe(handler1);
        eventBus.Subscribe(handler2);

        // Publish an event, handler1 will throw, but handler2 should still be called
        Assert.DoesNotThrow(() => eventBus.Publish(new Ping { Value = 1 }));
        Assert.AreEqual(1, calls);
    }

    [Test]
    public void Different_Event_Types_Do_Not_Interfere()
    {
        int pingCalls = 0;
        int pongCalls = 0;

        System.Action<Ping> pingHandler = ping => pingCalls++;
        System.Action<Pong> pongHandler = pong => pongCalls++;

        eventBus.Subscribe(pingHandler);
        eventBus.Subscribe(pongHandler);

        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(1, pingCalls);
        Assert.AreEqual(0, pongCalls);

        eventBus.Publish(new Pong { Value = 1 });
        Assert.AreEqual(1, pingCalls);
        Assert.AreEqual(1, pongCalls);
    }

}

