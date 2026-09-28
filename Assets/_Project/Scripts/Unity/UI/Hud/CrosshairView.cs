using Game.Unity.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Hud
{
    /// <summary>A small dot in the centre of the view, shown only while the player walks and aims.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CrosshairView : MonoBehaviour
    {
        private const string CrosshairName = "crosshair";

        private VisualElement _crosshair;
        private PlayerController _player;

        /// <summary>Wires the crosshair to the player. Called once by <c>GameBootstrap</c>.</summary>
        public void Initialize(PlayerController player)
        {
            VisualElement documentRoot = GetComponent<UIDocument>().rootVisualElement;
            _crosshair = documentRoot == null ? null : documentRoot.Q<VisualElement>(CrosshairName);
            if (player == null || _crosshair == null)
            {
                Debug.LogError($"{name}: {nameof(CrosshairView)} needs a player and a '{CrosshairName}' element.", this);
                return;
            }

            _player = player;
            _player.GameplayInputChanged += OnGameplayInputChanged;
            OnGameplayInputChanged(_player.IsInGameplay);
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.GameplayInputChanged -= OnGameplayInputChanged;
            }
        }

        private void OnGameplayInputChanged(bool isInGameplay)
        {
            _crosshair.style.display = isInGameplay ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
