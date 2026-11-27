using NUnit.Framework;
using UnityEngine;
using Foundation.Systems.Game;
using System.Collections.Generic;


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

    public struct AssetPing
    {
        public int Value;
    }

    public sealed class AssetPingChannel : AssetEventChannel<AssetPing>
    {
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

        // Handlers for Ping and Pong events, these should only respond to their respective event types.
        System.Action<Ping> pingHandler = ping => pingCalls++;
        System.Action<Pong> pongHandler = pong => pongCalls++;

        eventBus.Subscribe(pingHandler);
        eventBus.Subscribe(pongHandler);

        // Publish a Ping event, only the Ping handler should be called.
        eventBus.Publish(new Ping { Value = 1 });
        Assert.AreEqual(1, pingCalls);
        Assert.AreEqual(0, pongCalls);

        // Publish a Pong event, only the Pong handler should be called.
        eventBus.Publish(new Pong { Value = 1 });
        Assert.AreEqual(1, pingCalls);
        Assert.AreEqual(1, pongCalls);
    }

    [Test]
    public void Asset_Channels_Are_Isolated_From_Each_Other_And_Type_Routes()
    {

        int firstChannelCalls = 0;
        int secondChannelCalls = 0;
        int typeRouteCalls = 0;

        // Create two separate asset channels and subscribe handlers to them, as well as a type-based handler.
        AssetPingChannel firstChannel = ScriptableObject.CreateInstance<AssetPingChannel>();
        AssetPingChannel secondChannel = ScriptableObject.CreateInstance<AssetPingChannel>();

        try
        {
            // Subscribe handlers for the first and second asset channels.
            eventBus.Subscribe(firstChannel, (AssetPing _) => firstChannelCalls++);
            eventBus.Subscribe(secondChannel, (AssetPing _) => secondChannelCalls++);

            // Subscribe a type-based handler for AssetPing events.
            eventBus.Subscribe<AssetPing>((AssetPing _) => typeRouteCalls++);

            // Publish events to the first and second channels, as well as a type-based event.
            eventBus.Publish(firstChannel, new AssetPing { Value = 1 });
            Assert.AreEqual(1, firstChannelCalls);
            Assert.AreEqual(0, secondChannelCalls);
            Assert.AreEqual(0, typeRouteCalls);

            // Publish an event to the second channel, only the second channel handler should be called.
            eventBus.Publish(secondChannel, new AssetPing { Value = 2 });
            Assert.AreEqual(1, firstChannelCalls);
            Assert.AreEqual(1, secondChannelCalls);
            Assert.AreEqual(0, typeRouteCalls);

            // Publish a type-based event, only the type-based handler should be called.
            eventBus.Publish(new AssetPing { Value = 3 });
            Assert.AreEqual(1, firstChannelCalls);
            Assert.AreEqual(1, secondChannelCalls);
            Assert.AreEqual(1, typeRouteCalls);
        }
        finally
        {
            Object.DestroyImmediate(firstChannel);
            Object.DestroyImmediate(secondChannel);
        }
    }

    [Test]
    public void Unsubscribe_From_Asset_Channel_Stops_Future_Delivery()
    {
        int calls = 0;

        // Create an asset channel and a handler for it.
        AssetPingChannel channel = ScriptableObject.CreateInstance<AssetPingChannel>();
        System.Action<AssetPing> handler = _ => calls++;

        try
        {
            // Subscribe the handler to the channel.
            eventBus.Subscribe(channel, handler);

            // Publish an event to the channel, the handler should be called.
            eventBus.Publish(channel, new AssetPing());

            // Unsubscribe the handler from the channel.
            eventBus.Unsubscribe(channel, handler);

            // Publish another event to the channel, the handler should not be called.
            eventBus.Publish(channel, new AssetPing());
            Assert.AreEqual(1, calls);
        }
        finally
        {
            Object.DestroyImmediate(channel);
        }
    }

    [Test]
    public void Queued_Asset_Events_Are_Dispatched_In_Order_Only_On_Their_Channel()
    {
        var firstValues = new List<int>();
        var secondValues = new List<int>();

        // Create two asset channels for testing queued events.
        AssetPingChannel firstChannel = ScriptableObject.CreateInstance<AssetPingChannel>();
        AssetPingChannel secondChannel = ScriptableObject.CreateInstance<AssetPingChannel>();

        try
        {
            // Subscribe handlers to the channels.
            eventBus.Subscribe(firstChannel, ping => firstValues.Add(ping.Value));
            eventBus.Subscribe(secondChannel, ping => secondValues.Add(ping.Value));

            // Publish queued events to the first channel.
            eventBus.PublishQueued(firstChannel, new AssetPing { Value = 1 });
            eventBus.PublishQueued(firstChannel, new AssetPing { Value = 2 });
            
            
            Assert.IsEmpty(firstValues);
            Assert.IsEmpty(secondValues);
            
            // Dispatch queued events to the subscribed handlers.
            eventBus.DispatchQueued();
            CollectionAssert.AreEqual(new[] { 1, 2 }, firstValues);
            
            //Second channel should still have no events delivered since none were queued for it.
            Assert.IsEmpty(secondValues);
        }
        finally
        {
            Object.DestroyImmediate(firstChannel);
            Object.DestroyImmediate(secondChannel);
        }
    }
}

