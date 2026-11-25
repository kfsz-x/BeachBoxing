using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Source beach jump, feet probe and coyote timing, converted to the existing .356 world scale.</summary>
    [DisallowMultipleComponent]
    public sealed class BeachNativeLocomotion : MonoBehaviour
    {
        public Rigidbody body;
        public Transform feet;
        public LayerMask groundMask = 8;
        public AudioSource jumpAudio, landAudio;
        public ParticleSystem walkingDust, landingParticle;
        public bool Grounded { get; private set; }
        public bool Roaming { get; private set; }
        private float coyote, cooldown, jumpRequest;
        private Vector3 desiredDirection;
        private bool landingPending;
        public bool Moving => desiredDirection.sqrMagnitude >= 1e-10f;

        public void SetRoaming(bool value, bool dynamicBody = false)
        {
            Roaming = value;
            jumpRequest = coyote = cooldown = 0;
            desiredDirection = Vector3.zero;
            landingPending = false;
            if (!body) return;
            if (value || dynamicBody) body.isKinematic = false;
            if (!body.isKinematic) body.velocity = Vector3.zero;
            body.useGravity = value || dynamicBody;
            body.isKinematic = !(value || dynamicBody);
            Grounded = value && ProbeGround();
            if (walkingDust) { var emission = walkingDust.emission; emission.rateOverTime = 0; }
        }

        public void RequestJump() => jumpRequest = 0.1f;
        public void ReleaseJump() => coyote = 0;
        public void SetDirection(Vector3 direction)
        {
            direction.y = 0;
            desiredDirection = direction.sqrMagnitude >= 1e-10f ? direction.normalized : Vector3.zero;
        }

        public void StopMovement()
        {
            desiredDirection = Vector3.zero;
            if (body && Roaming) body.velocity = new Vector3(0, body.velocity.y, 0);
        }

        private void FixedUpdate()
        {
            if (!Roaming || !body) return;
            body.velocity = new Vector3(desiredDirection.x * (35f * .356f), body.velocity.y,
                desiredDirection.z * (35f * .356f));
            if (Moving) transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(desiredDirection), Time.fixedDeltaTime * 25);
        }

        private bool ProbeGround() => feet && Physics.CheckSphere(feet.position, 0.4f * 0.356f,
            groundMask, QueryTriggerInteraction.Ignore);

        private void Update()
        {
            if (!Roaming || !body) return;
            float dt = Time.deltaTime;
            cooldown = Mathf.Max(0, cooldown - dt);
            Grounded = ProbeGround() && cooldown <= 0;
            if (!Grounded) landingPending = true;
            else if (landingPending)
            {
                landingPending = false;
                if (landAudio) landAudio.Play();
                if (landingParticle) landingParticle.Play();
            }
            if (walkingDust) { var emission = walkingDust.emission; emission.rateOverTime = Grounded && Moving ? 5 : 0; }
            coyote = Grounded ? 0.1f : Mathf.Max(0, coyote - dt);
            if (jumpRequest > 0 && cooldown <= 0 && (Grounded || coyote > 0))
            {
                body.velocity = new Vector3(body.velocity.x, 37f * 0.356f, body.velocity.z);
                cooldown = 0.1f;
                jumpRequest = coyote = 0;
                Grounded = false;
                landingPending = true;
                if (jumpAudio) jumpAudio.Play();
            }
            jumpRequest = Mathf.Max(0, jumpRequest - dt);
            if (!Grounded && body.velocity.y < 0)
                body.velocity += Vector3.down * (0.01f * 0.356f);
        }
    }

}
