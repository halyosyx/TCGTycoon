namespace Game.Core.Common
{
    /// <summary>Source of randomness for game rules. Implementations must be deterministic for a given seed.</summary>
    public interface IRng
    {
        /// <summary>Returns a uniformly distributed value in [0, <paramref name="maxExclusive"/>).</summary>
        long NextLong(long maxExclusive);

        /// <summary>Returns a uniformly distributed value in [0, <paramref name="maxExclusive"/>).</summary>
        int NextInt(int maxExclusive);
    }
}
