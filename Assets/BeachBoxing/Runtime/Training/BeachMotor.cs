using UnityEngine;

namespace BeachBoxing
{
    public sealed class BeachMotor
    {
        private const int MovementMask = ~(1 << 8); // Native Player collides with every layer except NPC.
        private static readonly RaycastHit[] SelfFilteredHits = new RaycastHit[16];

        public static void Walk(Transform actor, Vector2 input, float speed, float delta, Rect bounds)
        {
            var forward = new Vector3(input.x, 0, input.y);
            if (forward.sqrMagnitude >= 1e-10f) forward.Normalize();
            Translate(actor, forward * (speed * delta), bounds);
            if (forward.sqrMagnitude >= 1e-10f)
                actor.rotation = Quaternion.Lerp(actor.rotation, Quaternion.LookRotation(forward), 25 * delta);
        }

        public static void Translate(Transform actor, Vector3 displacement, Rect bounds, bool enemy = false)
        {
            var body = actor.GetComponent<Rigidbody>();
            if (body && !body.isKinematic) body.velocity = new Vector3(0, body.velocity.y, 0);
            float distance = displacement.magnitude;
            if (distance > 0.0001f)
            {
                GetActorCapsule(actor, enemy, out var lower, out var upper, out float radius);
                var direction = displacement / distance;
                if (CastAgainstWorld(actor, lower, upper, radius, direction, distance + 0.01f, out var hit))
                {
                    float step = hit.collider.bounds.max.y - actor.position.y;
                    bool lowStep = step > 0 && step <= 0.45f;
                    bool clearAbove = lowStep && !CastAgainstWorld(actor, lower + Vector3.up * 0.45f,
                        upper + Vector3.up * 0.45f, radius, direction, distance + 0.01f, out _);
                    if (clearAbove) displacement.y += step + 0.02f;
                    else displacement = displacement.normalized * Mathf.Max(0, hit.distance - 0.01f);
                }
            }
            var point = actor.position + displacement;
            point.x = Mathf.Clamp(point.x, bounds.xMin, bounds.xMax);
            point.z = Mathf.Clamp(point.z, bounds.yMin, bounds.yMax);
            actor.position = point;
        }

        internal static void GetActorCapsule(Transform actor, bool enemy, out Vector3 lower, out Vector3 upper, out float radius)
        {
            var capsule = actor.GetComponent<CapsuleCollider>();
            if (!IsBodyCapsule(capsule))
            {
                var body = actor.Find("PlayerCollider");
                capsule = body ? body.GetComponent<CapsuleCollider>() : null;
            }

            if (IsBodyCapsule(capsule))
            {
                SetCapsule(capsule.transform, capsule.center, capsule.direction, capsule.radius, capsule.height,
                    out lower, out upper, out radius);
                return;
            }

            float scale = FightRules.ReferenceWorldScale;
            float localRadius = (enemy ? 2.25f : 0.75f) * scale;
            float localHeight = 2 * ((enemy ? 1.335949f : 1.75688f) * scale + localRadius);
            var localCenter = Vector3.up * ((enemy ? 2.59f : 2.557898f) * scale);
            SetCapsule(actor, localCenter, 1, localRadius, localHeight, out lower, out upper, out radius);
        }

        private static bool IsBodyCapsule(CapsuleCollider capsule) =>
            capsule && capsule.enabled && capsule.gameObject.activeInHierarchy && !capsule.isTrigger;

        private static void SetCapsule(Transform body, Vector3 localCenter, int axis, float localRadius, float localHeight,
            out Vector3 lower, out Vector3 upper, out float radius)
        {
            var scale = body.lossyScale;
            float radiusScale = Mathf.Max(Mathf.Abs(scale[(axis + 1) % 3]), Mathf.Abs(scale[(axis + 2) % 3]));
            radius = localRadius * radiusScale;
            float segment = Mathf.Max(0, localHeight * Mathf.Abs(scale[axis]) * 0.5f - radius);
            var localAxis = Vector3.zero;
            localAxis[axis] = 1;
            var offset = body.TransformDirection(localAxis) * segment;
            var center = body.TransformPoint(localCenter);
            lower = center - offset;
            upper = center + offset;
        }

        private static bool CastAgainstWorld(Transform actor, Vector3 lower, Vector3 upper, float radius,
            Vector3 direction, float range, out RaycastHit hit)
        {
            if (!Physics.CapsuleCast(lower, upper, radius, direction, out hit, range, MovementMask, QueryTriggerInteraction.Ignore))
                return false;
            if (!hit.collider.transform.IsChildOf(actor)) return true;

            var hits = SelfFilteredHits;
            int count = Physics.CapsuleCastNonAlloc(lower, upper, radius, direction, hits, range, MovementMask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length)
            {
                hits = Physics.CapsuleCastAll(lower, upper, radius, direction, range, MovementMask, QueryTriggerInteraction.Ignore);
                count = hits.Length;
            }

            hit = default;
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i];
                // The singular cast ignores initial overlaps. The all-hits queries return them at the origin.
                if (candidate.distance == 0 && candidate.point == Vector3.zero) continue;
                if (candidate.distance >= closest || candidate.collider.transform.IsChildOf(actor)) continue;
                closest = candidate.distance;
                hit = candidate;
            }
            return hit.collider != null;
        }
    }
}
