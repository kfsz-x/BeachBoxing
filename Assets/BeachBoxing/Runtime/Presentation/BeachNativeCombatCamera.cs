using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Source Boxing.Update orbit and CameraFollow3016, using prebound scene transforms.</summary>
    [DisallowMultipleComponent]
    public sealed class BeachNativeCombatCamera : MonoBehaviour
    {
        public Transform player, enemy, target, view, arena;
        public Vector3 sourceOrigin = new Vector3(14, 6.9f, -4.9f);
        public Vector3 initialRigPosition = new Vector3(14, 10.9f, -4.9f);
        public Vector3 desktopLocalOffset = new Vector3(-8.7f, 4.9f, 6.94f);
        // Use the same proven inside-the-ropes over-the-shoulder framing as desktop so mobile never clips behind ring posts.
        public Vector3 phoneWorldOperand = new Vector3(-8.7f, 4.9f, 6.94f);
        public Vector3 levelTenLocalOffset = new Vector3(-12, 1.1f, 9.3f);
        public Vector3 idleEuler = new Vector3(8.383f, -214.616f, 0);
        Vector3 followPosition, localOffset;
        Quaternion rigRotation;
        bool initialized;

        public void Initialize(bool phone, int prestige)
        {
            float fraction = (Mathf.Clamp(prestige, 1, 10) - 1) / 9f;
            localOffset = Vector3.Lerp(phone ? phoneWorldOperand : desktopLocalOffset,
                levelTenLocalOffset, fraction) * FightRules.ReferenceWorldScale;
            followPosition = arena.TransformPoint((initialRigPosition - sourceOrigin) * FightRules.ReferenceWorldScale);
            rigRotation = Quaternion.identity;
            initialized = true;
        }

        public void Step(float seconds, bool active, bool canMove)
        {
            if (!initialized || !player || !enemy || !target || !view) return;
            var direction = enemy.position - player.position;
            direction.y = 0;
            if (canMove && direction.sqrMagnitude >= .0001f * FightRules.ReferenceWorldScale * FightRules.ReferenceWorldScale)
                rigRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180, 0);
            if (!active) return;
            followPosition = Vector3.Lerp(followPosition, target.position, Mathf.Max(0, seconds) * 8);
            view.SetPositionAndRotation(followPosition + rigRotation * localOffset,
                rigRotation * Quaternion.Euler(idleEuler));
        }
    }
}
