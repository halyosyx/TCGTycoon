using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// The pack object in the player's hand, as the pack opening screen sees it while tearing it open.
    /// The screen owns the tear (input, progress, the commit); the pack only shows the pose it is given.
    /// </summary>
    public interface ITearablePack
    {
        /// <summary>The store product id of the pack.</summary>
        string ProductId { get; }

        /// <summary>
        /// The tear has started: the pack leaves the hand pose for the tearing pose (local to
        /// <c>camera</c>), blending as <see cref="PackTearPose.PoseBlend"/> grows.
        /// </summary>
        void BeginTear(Transform camera, Vector3 tearPosition, Quaternion tearRotation);

        void ApplyTear(PackTearPose pose);

        /// <summary>The tear is over (finished, skipped or stored): the empty wrapper goes away.</summary>
        void Discard();
    }
}
