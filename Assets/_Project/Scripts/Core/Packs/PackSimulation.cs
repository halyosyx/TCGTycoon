using System;

namespace Game.Core.Packs
{
    /// <summary>
    /// Opens many packs and tallies them. It drives the real <see cref="PackOpener"/>, so a
    /// simulation rolls exactly like the game; there is no separate roller for tools or tests.
    /// </summary>
    public static class PackSimulation
    {
        public static PackTally Run(PackOpener opener, int packCount)
        {
            if (opener == null) throw new ArgumentNullException(nameof(opener));
            if (packCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(packCount), packCount, "Can't open a negative number of packs.");
            }

            var tally = new PackTally(opener.Config.Slots.Count);
            for (int i = 0; i < packCount; i++)
            {
                tally.Add(opener.Open());
            }

            return tally;
        }
    }
}
