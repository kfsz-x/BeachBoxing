using System;
using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Plays prebound source UI tracks; modal open/close controllers retain their own state owners.</summary>
    [DisallowMultipleComponent]
    public sealed class BeachNativeUiMotion : MonoBehaviour
    {
        [Serializable]
        public sealed class Binding
        {
            public int sourceId;
            public Transform target;
            public AnimationClip clip;
            public float duration;
            public bool loop, unscaled;
            [NonSerialized] public float elapsed;
            [NonSerialized] public bool active;
        }

        public Binding[] bindings = Array.Empty<Binding>();

        private void LateUpdate() => Step(Time.deltaTime, Time.unscaledDeltaTime);

        private void Step(float scaledSeconds, float unscaledSeconds)
        {
            foreach (var binding in bindings)
            {
                if (binding == null) continue;
                if (!binding.target || !binding.clip || !binding.target.gameObject.activeInHierarchy)
                {
                    binding.active = false;
                    binding.elapsed = 0;
                    continue;
                }
                if (!binding.active) binding.elapsed = 0;
                binding.active = true;
                binding.elapsed += Mathf.Max(0, binding.unscaled ? unscaledSeconds : scaledSeconds);
                float duration = binding.duration > 0 ? binding.duration : binding.clip.length;
                float time = binding.loop && duration > 0 ? Mathf.Repeat(binding.elapsed, duration) : Mathf.Min(binding.elapsed, duration);
                binding.clip.SampleAnimation(binding.target.gameObject, time);
            }
        }

        private void OnDisable()
        {
            foreach (var binding in bindings)
                if (binding != null) { binding.active = false; binding.elapsed = 0; }
        }
    }
}
