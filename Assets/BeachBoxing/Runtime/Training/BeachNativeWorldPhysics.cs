using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Restores source project physics only while the reconstructed scene is alive.</summary>
    [DisallowMultipleComponent]
    public sealed class BeachNativeWorldPhysics : MonoBehaviour
    {
        public Collider[] authoredColliders = new Collider[0];
        public Rigidbody[] authoredBodies = new Rigidbody[0];
        private Vector3 previousGravity;
        private float previousFixedStep;
        private float previousContactOffset, previousDepenetration;
        private bool previousNpcCollision;
        private bool previousNpcSelfCollision, previousDefaultNpcCollision;

        private void Awake()
        {
            previousGravity = Physics.gravity;
            previousFixedStep = Time.fixedDeltaTime;
            previousContactOffset = Physics.defaultContactOffset;
            previousDepenetration = Physics.defaultMaxDepenetrationVelocity;
            previousNpcCollision = Physics.GetIgnoreLayerCollision(7, 8);
            previousNpcSelfCollision = Physics.GetIgnoreLayerCollision(8, 8);
            previousDefaultNpcCollision = Physics.GetIgnoreLayerCollision(0, 8);
            Physics.gravity = new Vector3(0, -80f * 0.356f, 0);
            Time.fixedDeltaTime = 0.033f;
            Physics.defaultContactOffset = .01f * .356f;
            Physics.defaultMaxDepenetrationVelocity = 10f * .356f;
            foreach (var collider in authoredColliders) if (collider) collider.contactOffset = .01f * .356f;
            foreach (var body in authoredBodies) if (body) body.maxDepenetrationVelocity = 10f * .356f;
            Physics.IgnoreLayerCollision(7, 8, true);
            Physics.IgnoreLayerCollision(8, 8, true);
            Physics.IgnoreLayerCollision(0, 8, false);
        }

        private void OnDestroy()
        {
            Physics.gravity = previousGravity;
            Time.fixedDeltaTime = previousFixedStep;
            Physics.defaultContactOffset = previousContactOffset;
            Physics.defaultMaxDepenetrationVelocity = previousDepenetration;
            Physics.IgnoreLayerCollision(7, 8, previousNpcCollision);
            Physics.IgnoreLayerCollision(8, 8, previousNpcSelfCollision);
            Physics.IgnoreLayerCollision(0, 8, previousDefaultNpcCollision);
        }
    }
}
