using System.Collections.Generic;
using Game.Unity.Cards;
using UnityEngine;

namespace Game.Unity.Interaction
{
    /// <summary>
    /// Tints a group of renderers through one MaterialPropertyBlock (no material copies), e.g. a pack in
    /// its set colour, switched to the hover colour while the player aims at it. Shared by every
    /// interactable that tints.
    /// </summary>
    public sealed class RendererTint
    {
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<Renderer> _renderers;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public RendererTint(List<Renderer> renderers)
        {
            _renderers = renderers ?? new List<Renderer>();
        }

        public void Set(Color colour)
        {
            _block.SetColor(s_baseColorId, colour);
            foreach (Renderer target in _renderers)
            {
                if (target != null)
                {
                    target.SetPropertyBlock(_block);
                }
            }
        }

        /// <summary>
        /// The renderers a pack model tints: its wrapper (body and top strip), not the cards inside or its
        /// printed TextMeshPro label.
        /// </summary>
        public static List<Renderer> PackMeshes(GameObject root)
        {
            var renderers = new List<Renderer>();
            if (root.TryGetComponent(out BoosterPackView view))
            {
                view.CollectWrapperRenderers(renderers);
                return renderers;
            }

            foreach (MeshRenderer meshRenderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!meshRenderer.TryGetComponent(out TMPro.TextMeshPro _))
                {
                    renderers.Add(meshRenderer);
                }
            }

            return renderers;
        }
    }
}
