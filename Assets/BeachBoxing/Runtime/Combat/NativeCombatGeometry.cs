using UnityEngine;

namespace BeachBoxing
{
    [System.Serializable]
    public sealed class NativeCombatGeometry
    {
        public Vector3 bodyCenter, punchCenter;
        public float bodyRadius, bodyHeight, punchRadius, deadBodyRadius;
        public int bodyColliderId, punchColliderId, deadBodyColliderId;

        // All 45 source prefabs have exactly these shapes before their root prestige scale.
        public static NativeCombatGeometry Enemy(int punchId = 0, int bodyId = 0, int deadId = 0) =>
            new NativeCombatGeometry
            {
                bodyCenter = new Vector3(0, 2.589999914f, 0), bodyRadius = 2.25f,
                bodyHeight = 7.171898842f, deadBodyRadius = 1f,
                punchCenter = new Vector3(-1.108800028f, 1.907822974f, 1.048319885f),
                punchRadius = 2.449387097f,
                punchColliderId = punchId, bodyColliderId = bodyId, deadBodyColliderId = deadId
            };

        public static NativeCombatGeometry Player() => new NativeCombatGeometry
        {
            bodyCenter = new Vector3(0, 2.557898045f, 0), bodyRadius = .75f,
            bodyHeight = 5.01376009f, deadBodyRadius = .75f, bodyColliderId = 1953,
            punchCenter = new Vector3(-1.108800028f, 3.434477f, 1.048319885f),
            // level1:1952 is detached. ActivatePunchCollider copies position only.
            punchRadius = 2.092314959f, punchColliderId = 1952
        };
    }
}
