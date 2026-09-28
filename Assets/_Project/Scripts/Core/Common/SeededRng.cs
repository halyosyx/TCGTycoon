using System;

namespace Game.Core.Common
{
    /// <summary>
    /// Deterministic random number generator (SplitMix64). Its whole state is one <see cref="ulong"/>,
    /// so it can be saved and restored exactly, and it produces the same sequence on every runtime,
    /// unlike <see cref="System.Random"/>, whose state can't be read.
    /// </summary>
    public sealed class SeededRng : IRng
    {
        // SplitMix64 reference constants (Steele, Lea and Flood, 2014).
        private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
        private const ulong MixMultiplierA = 0xBF58476D1CE4E5B9UL;
        private const ulong MixMultiplierB = 0x94D049BB133111EBUL;
        private const int ShiftA = 30;
        private const int ShiftB = 27;
        private const int ShiftC = 31;

        private ulong _state;

        /// <summary>Creates a generator. Equal seeds give equal sequences.</summary>
        public SeededRng(long seed)
        {
            _state = unchecked((ulong)seed);
        }

        /// <summary>The generator's complete state. <see cref="FromState"/> resumes from it exactly.</summary>
        public ulong State => _state;

        /// <summary>Recreates a generator exactly where another one's <see cref="State"/> left off.</summary>
        public static SeededRng FromState(ulong state) => new SeededRng(unchecked((long)state));

        public long NextLong(long maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Must be greater than zero.");
            }

            ulong bound = (ulong)maxExclusive;

            // Rejection sampling: values below the threshold would make "value % bound" favour small
            // results, so they're redrawn. For game-sized bounds a redraw almost never happens.
            ulong threshold = unchecked(0UL - bound) % bound;
            while (true)
            {
                ulong value = NextUInt64();
                if (value >= threshold)
                {
                    return (long)(value % bound);
                }
            }
        }

        public int NextInt(int maxExclusive) => (int)NextLong(maxExclusive);

        private ulong NextUInt64()
        {
            unchecked
            {
                _state += GoldenGamma;
                ulong mixed = _state;
                mixed = (mixed ^ (mixed >> ShiftA)) * MixMultiplierA;
                mixed = (mixed ^ (mixed >> ShiftB)) * MixMultiplierB;
                return mixed ^ (mixed >> ShiftC);
            }
        }
    }
}
