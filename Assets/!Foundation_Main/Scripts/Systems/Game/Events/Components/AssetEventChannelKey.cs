using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Foundation.Systems.Game
{
    internal readonly struct AssetEventChannelKey : IEquatable<AssetEventChannelKey>
    {
        private readonly UnityEngine.Object asset;
        private readonly Type eventType;

        public AssetEventChannelKey(UnityEngine.Object asset, Type eventType)
        {
            this.asset = asset;
            this.eventType = eventType;
        }

        public bool Equals(AssetEventChannelKey other)
        {
            return object.ReferenceEquals(asset, other.asset) && eventType == other.eventType;
        }

        public override bool Equals(object obj)
        {
            return obj is AssetEventChannelKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (RuntimeHelpers.GetHashCode(asset) * 397) ^ eventType.GetHashCode();
            }
        }

    }
}
