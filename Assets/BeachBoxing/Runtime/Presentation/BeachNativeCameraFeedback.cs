using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Source punch camera curves on a preplaced child of the gameplay camera rig.</summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class BeachNativeCameraFeedback : MonoBehaviour
    {
        public Camera view;
        public AnimationCurve fov, pitchLeft, yawLeft, pitchRight, yawRight;
        public float punchDuration = .26666668f;
        private Vector3 basePosition, shakeOffset;
        private Quaternion baseRotation;
        private float baseFov, elapsed, shakeRemaining = -1, shakeIntensity;
        private bool punching, left;

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            baseFov = view.fieldOfView;
        }

        public void Punch(bool leftHand)
        {
            left = leftHand;
            elapsed = 0;
            punching = true;
            Shake(.1f, .23f);
        }

        public void Shake(float intensity, float duration)
        {
            shakeIntensity = intensity * FightRules.ReferenceWorldScale;
            shakeRemaining = duration;
        }

        private void FixedUpdate()
        {
            if (shakeRemaining >= 0)
            {
                shakeOffset = Random.insideUnitSphere * shakeIntensity;
                shakeRemaining -= Time.fixedDeltaTime;
            }
            else shakeOffset = Vector3.zero;
        }

        private void LateUpdate() => Step(Time.deltaTime);

        public void Step(float seconds)
        {
            Vector3 angles = Vector3.zero;
            float fovOffset = 0;
            if (punching)
            {
                elapsed = Mathf.Min(punchDuration, elapsed + Mathf.Max(0, seconds));
                var pitch = left ? pitchLeft : pitchRight;
                var yaw = left ? yawLeft : yawRight;
                angles.x = pitch.Evaluate(elapsed) - pitch.Evaluate(0);
                angles.y = yaw.Evaluate(elapsed) - yaw.Evaluate(0);
                fovOffset = fov.Evaluate(elapsed) - fov.Evaluate(0);
                if (elapsed >= punchDuration) punching = false;
            }
            transform.localPosition = basePosition + shakeOffset;
            transform.localRotation = baseRotation * Quaternion.Euler(angles);
            view.fieldOfView = baseFov + fovOffset;
        }

        public void ResetEffects()
        {
            punching = false;
            elapsed = 0;
            shakeRemaining = -1;
            shakeOffset = Vector3.zero;
            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
            if (view) view.fieldOfView = baseFov;
        }
    }
}
