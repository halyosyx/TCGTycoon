using UnityEngine;

namespace Game.Unity.Definitions
{
    /// <summary>Authoring asset for economy numbers that aren't prices: today only the starting cash.</summary>
    [CreateAssetMenu(menuName = "TCG/Economy Config", fileName = "EconomyConfig")]
    public sealed class EconomyConfigDefinition : ScriptableObject
    {
        public const string StartingCashField = nameof(_startingCashCents);

        [SerializeField, Min(0), Tooltip("Cash at the start of a run, in cents. GDD: 50000 ($500).")]
        private long _startingCashCents = 50_000;

        public long StartingCashCents => _startingCashCents;
    }
}
