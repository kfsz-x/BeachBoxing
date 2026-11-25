using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing
{
    /// <summary>Presentation only. Caller samples old speed before the workout advances, then observes its new fill.</summary>
    public sealed class BeachNativeTrainingFeedback : MonoBehaviour
    {
        [Serializable] public sealed class MuscleBinding { public Muscle muscle; public Renderer[] renderers = Array.Empty<Renderer>(); }
        [Serializable] public sealed class RewardPool { public Muscle muscle; public BeachNativeRewardParticle[] slots = Array.Empty<BeachNativeRewardParticle>(); [NonSerialized] public int cursor; }
        public MuscleBinding[] muscles = Array.Empty<MuscleBinding>();
        public RewardPool[] pools = Array.Empty<RewardPool>();
        public Renderer face;
        public Material normalFace, angryFace, strugglingFace;
        public ParticleSystem sweat;
        public ParticleSystem[] workoutParticles = Array.Empty<ParticleSystem>();
        public AudioSource[] workoutAudio = Array.Empty<AudioSource>();
        public Image fillBar;
        public Transform particleSpawn, parentCanvas;
        public Color skinColor = new Color(1, .6196078658f, .4941176474f, 0);
        public Color redColor = new Color(1, .1764705926f, .1764705926f, 0);
        struct SpeedSample { public float speed, time; }
        readonly List<SpeedSample> history = new List<SpeedSample>();
        MaterialPropertyBlock colorBlock;
        Action<float,float> shake;
        bool working;
        float evaluationClock;
        int audioIndex, particleIndex;
        public float MeanSpeed { get; private set; }
        public int HistoryCount => history.Count;
        public int FaceSourceId { get; private set; } = 43;
        public int SweatRate { get; private set; }
        public bool HasBoundFeedback => face && normalFace && angryFace && strugglingFace && sweat && fillBar &&
            particleSpawn && parentCanvas && pools.Length == 4 && Array.TrueForAll(pools, p => p != null && p.slots.Length > 0) &&
            workoutAudio.Length == 5 && workoutParticles.Length >= 3;
        public void Bind(Action<float,float> callback) => shake = callback;
        public void Enter(Muscle muscle) { working = true; ResetSkin(); }
        public void Sample(float oldSpeed, float absoluteTime, float activeDt)
        {
            if (!working) return;
            history.Add(new SpeedSample { speed = oldSpeed, time = absoluteTime });
            history.RemoveAll(s => absoluteTime - s.time > .5f);
            evaluationClock += Mathf.Max(0, activeDt);
            if (evaluationClock < .5f) return;
            evaluationClock = 0;
            float sum = 0; foreach (var sample in history) sum += sample.speed;
            MeanSpeed = history.Count == 0 ? 0 : sum / history.Count;
            if (MeanSpeed < .6f) SetMood(43, 0);
            else if (MeanSpeed < 1f) SetMood(45, 20);
            else if (MeanSpeed < 1.5f) SetMood(46, 50);
            else if (MeanSpeed < 2.3f) SetMood(46, 90);
        }
        public void Observe(Muscle muscle, float fill, PlayerProgress progress, bool pulse)
        {
            if (!working) return;
            if (progress != null && BeachProgressionRules.Fraction(progress.Get(muscle), progress.prestigeLevel) < 1)
            {
                float t = Mathf.Clamp01(Mathf.Abs(Mathf.Clamp01(fill) - .5f) * 2);
                SetColor(muscle, Color.Lerp(redColor, skinColor, t));
            }
            if (!pulse) return;
            if (fillBar) fillBar.color = Color.yellow;
            if (workoutAudio.Length > 0)
            {
                var audio = workoutAudio[audioIndex]; audioIndex = (audioIndex + 1) % workoutAudio.Length;
                if (audio) { audio.pitch = UnityEngine.Random.Range(1.2f, .8f); audio.Play(); }
            }
        }
        public void Completed(Muscle muscle, int gain)
        {
            shake?.Invoke(.08f, .14f);
            if (workoutParticles.Length >= 3)
            { var effect = workoutParticles[particleIndex]; particleIndex = (particleIndex + 1) % 3; if (effect) effect.Play(); }
            if (particleSpawn && parentCanvas)
                foreach (var pool in pools)
                {
                    if (pool.muscle != muscle || pool.slots.Length == 0) continue;
                    var slot = pool.slots[pool.cursor]; pool.cursor = (pool.cursor + 1) % pool.slots.Length;
                    slot.Begin(particleSpawn.position + new Vector3(UnityEngine.Random.Range(-70,70), UnityEngine.Random.Range(-70,70), 0), parentCanvas.rotation, gain);
                    break;
                }
            ResetBar();
        }
        public void Tick(float dt)
        {
            if (dt <= 0) return;
            if (working && fillBar) fillBar.color = Color.Lerp(fillBar.color, new Color(1, .75f, .2f, 1), dt * 7);
            foreach (var pool in pools) foreach (var slot in pool.slots) if (slot && slot.gameObject.activeSelf) slot.Tick(dt);
        }
        public void ResetBar() => ResetSkin();
        public void Exit() { working = false; ResetSkin(); SetMood(43, 0); }
        public void ResetScene()
        {
            Exit(); history.Clear(); MeanSpeed = 0; evaluationClock = 0; audioIndex = particleIndex = 0;
            foreach (var pool in pools) { pool.cursor = 0; foreach (var slot in pool.slots) if (slot) slot.Cancel(); }
        }
        void SetMood(int source, int rate)
        {
            FaceSourceId = source; SweatRate = rate;
            if (face) face.sharedMaterial = source == 43 ? normalFace : source == 45 ? angryFace : strugglingFace;
            if (sweat) { var emission = sweat.emission; emission.rateOverTime = rate; if (rate > 0 && !sweat.isPlaying) sweat.Play(); }
        }
        void ResetSkin() { foreach (var binding in muscles) SetColor(binding.muscle, skinColor); }
        void SetColor(Muscle muscle, Color value)
        {
            if (colorBlock == null) colorBlock = new MaterialPropertyBlock();
            foreach (var binding in muscles) if (binding.muscle == muscle)
                foreach (var renderer in binding.renderers) if (renderer)
                { renderer.GetPropertyBlock(colorBlock); colorBlock.SetColor("_Color", value); renderer.SetPropertyBlock(colorBlock); }
        }
    }
}
