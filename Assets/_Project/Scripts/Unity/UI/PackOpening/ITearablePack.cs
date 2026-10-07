using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// The pack object in the player's hand, as the pack opening screen sees it while opening it. The
    /// screen owns the whole sequence (zoom, anchor, rip, timing, the commit); the pack only shows the
    /// pose it is given, and hands itself back to the hand or the pool.
    /// </summary>
    public interface ITearablePack
    {
        /// <summary>The store product id of the pack.</summary>
        string ProductId { get; }

        /// <summary>
        /// The zoom has started: the pack leaves the hand pose for the centre anchor (local to
        /// <paramref name="camera"/>), blending as <see cref="PackTearPose.ZoomBlend"/> grows and scaling up
        /// to <paramref name="anchorScale"/>.
        /// </summary>
        void BeginZoom(Transform camera, Vector3 anchorPosition, Quaternion anchorRotation, float anchorScale);

        void ApplyPose(PackTearPose pose);

        /// <summary>Backed out of the zoom: the pack is in the hand again, sealed, exactly as before.</summary>
        void ReturnToHand();

        /// <summary>The opening is over (finished, skipped or stored): the empty wrapper goes away.</summary>
        void Discard();
    }
}
