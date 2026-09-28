using System;
using System.Collections.Generic;
using Game.Core.Common;

namespace Game.Core.Tests.TestUtilities
{
    /// <summary>An <see cref="IRng"/> that returns chosen values in order, for testing exact boundaries.</summary>
    public sealed class ScriptedRng : IRng
    {
        private readonly Queue<long> _values;

        public ScriptedRng(params long[] values)
        {
            _values = new Queue<long>(values);
        }

        public long NextLong(long maxExclusive)
        {
            if (_values.Count == 0)
            {
                throw new InvalidOperationException("ScriptedRng ran out of values.");
            }

            long value = _values.Dequeue();
            if (value < 0 || value >= maxExclusive)
            {
                throw new InvalidOperationException($"Scripted value {value} is outside [0, {maxExclusive}).");
            }

            return value;
        }

        public int NextInt(int maxExclusive) => (int)NextLong(maxExclusive);
    }
}
