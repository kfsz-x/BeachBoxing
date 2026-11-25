using System;
using UnityEngine;

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    public sealed class BeachMuscleView : MonoBehaviour
    {
        [Serializable]
        public struct Pose
        {
            public Vector3 position;
            public Vector3 scale;
            public Quaternion rotation;
        }

        [Serializable]
        public sealed class Binding
        {
            public Renderer renderer;
            public Pose start;
            public Pose end;
        }

        [SerializeField] private Binding[] chest = Array.Empty<Binding>();
        [SerializeField] private Binding[] arms = Array.Empty<Binding>();
        [SerializeField] private Binding[] back = Array.Empty<Binding>();
        [SerializeField] private Binding[] legs = Array.Empty<Binding>();

        public void Apply(PlayerProgress progress, int level)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            float chestFill = BeachProgressionRules.Fraction(progress.chest, level);
            float armsFill = BeachProgressionRules.Fraction(progress.arms, level);
            float backFill = BeachProgressionRules.Fraction(progress.back, level);
            float legsFill = BeachProgressionRules.Fraction(progress.legs, level);
            ApplyGroup(chest, chestFill);
            ApplyGroup(arms, armsFill);
            ApplyGroup(back, backFill);
            ApplyGroup(legs, legsFill);
        }

        private static void ApplyGroup(Binding[] bindings, float fraction)
        {
            foreach (var binding in bindings)
            {
                var part = binding.renderer.transform;
                part.localPosition = Vector3.Lerp(binding.start.position, binding.end.position, fraction);
                part.localRotation = Quaternion.Lerp(binding.start.rotation, binding.end.rotation, fraction);
                part.localScale = Vector3.Lerp(binding.start.scale, binding.end.scale, fraction);
                binding.renderer.enabled = true;
            }
        }

#if UNITY_EDITOR
        public void Bind(Binding[] chestParts, Binding[] armParts, Binding[] backParts, Binding[] legParts)
        {
            chest = chestParts;
            arms = armParts;
            back = backParts;
            legs = legParts;
        }
#endif
    }
}
