using System;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing
{
    /// <summary>Clip time is independent of wall time, including the original knockout slow motion.</summary>
    public sealed class BeachPresentationTimeline
    {
        public const float OpeningDuration = 4.5f;
        public const float SceneLoadDelay = .18f;
        public const float KnockoutTimeScale = .1f;
        public float OpeningTime { get; private set; }
        public float ResultTime { get; private set; }
        public float TransitionTime { get; private set; }
        public float SceneTime { get; private set; }
        public float TimeScale { get; private set; } = 1f;
        public bool OpeningActive { get; private set; }
        public bool ResultActive { get; private set; }
        public bool TransitionActive { get; private set; }
        private bool openingSoundPlayed, resultSoundPlayed, victoryParticlesPlayed;
        public bool OpeningComplete => OpeningTime >= OpeningDuration;
        public bool TransitionComplete => TransitionActive && TransitionTime >= SceneLoadDelay - .000001f;

        public void Reset()
        {
            OpeningTime = ResultTime = TransitionTime = SceneTime = 0;
            TimeScale = 1;
            OpeningActive = ResultActive = TransitionActive = false;
            openingSoundPlayed = resultSoundPlayed = victoryParticlesPlayed = false;
        }

        public void BeginOpening()
        {
            Reset();
            OpeningActive = true;
        }

        public void BeginResult(float timeScale)
        {
            OpeningActive = TransitionActive = false;
            ResultActive = true;
            ResultTime = 0;
            resultSoundPlayed = victoryParticlesPlayed = false;
            TimeScale = Math.Max(0, timeScale);
        }

        public void BeginTransition()
        {
            OpeningActive = ResultActive = false;
            TransitionActive = true;
            TransitionTime = 0;
            TimeScale = 1;
        }

        public void Advance(float unscaledSeconds, bool paused)
        {
            if (paused || unscaledSeconds <= 0 || float.IsNaN(unscaledSeconds) || float.IsInfinity(unscaledSeconds)) return;
            float seconds = unscaledSeconds * TimeScale;
            SceneTime += seconds;
            if (OpeningActive) OpeningTime = Math.Min(OpeningDuration, OpeningTime + seconds);
            if (ResultActive)
            {
                ResultTime += seconds;
                // Original EnemyManager.Update runs this after either fighter wins/dies.
                // The frame's scaled delta was computed with the previous timeScale.
                TimeScale = Mathf.Lerp(TimeScale, 1, seconds);
            }
            if (TransitionActive) TransitionTime += seconds;
        }

        public bool ConsumeOpeningSound() => Once(OpeningActive, OpeningTime, .11666667f, ref openingSoundPlayed);
        public bool ConsumeResultSound() => Once(ResultActive, ResultTime, 1.3166667f, ref resultSoundPlayed);
        public bool ConsumeVictoryParticles() => Once(ResultActive, ResultTime, 2f, ref victoryParticlesPlayed);

        private static bool Once(bool active, float time, float trigger, ref bool consumed)
        {
            if (!active || consumed || time < trigger) return false;
            consumed = true;
            return true;
        }
    }

    [Serializable]
    public sealed class BeachPresentationCubicCurve
    {
        [Serializable]
        public sealed class Key { public float time, a, b, c, d; }
        public Key[] keys;

        public float Evaluate(float seconds)
        {
            if (keys == null || keys.Length == 0) return 0;
            if (seconds <= keys[0].time) return keys[0].d;
            int index = keys.Length - 1;
            if (seconds >= keys[index].time) return keys[index].d;
            for (int next = 1; next < keys.Length; next++)
                if (seconds < keys[next].time) { index = next - 1; break; }
            var key = keys[index];
            float delta = seconds - key.time;
            return ((key.a * delta + key.b) * delta + key.c) * delta + key.d;
        }
    }

    [Serializable]
    public sealed class BeachPresentationCameraTrack
    {
        public Transform target, reference;
        public Vector3 pivot;
        public BeachPresentationCubicCurve[] curves;
        public bool IsBound => target && reference && curves != null && curves.Length == 9;

        public void Sample(float seconds)
        {
            if (!IsBound) return;
            Vector3 child = new Vector3(curves[0].Evaluate(seconds), curves[1].Evaluate(seconds), curves[2].Evaluate(seconds));
            Quaternion orbit = Quaternion.Euler(curves[3].Evaluate(seconds), curves[4].Evaluate(seconds), curves[5].Evaluate(seconds));
            Quaternion rotation = Quaternion.Euler(curves[6].Evaluate(seconds), curves[7].Evaluate(seconds), curves[8].Evaluate(seconds));
            target.SetPositionAndRotation(reference.TransformPoint(pivot + orbit * child), reference.rotation * orbit * rotation);
        }
    }

    [Serializable]
    public sealed class BeachPresentationScaleTrack
    {
        public Transform target;
        public AnimationCurve x, y, z;

        public void Sample(float seconds)
        {
            if (target && x != null && y != null && z != null)
                target.localScale = new Vector3(x.Evaluate(seconds), y.Evaluate(seconds), z.Evaluate(seconds));
        }
    }

    /// <summary>Only updates preplaced scene objects. The Editor migration binds original curve data.</summary>
    [Serializable]
    public sealed class BeachPresentationView
    {
        public GameObject openingRoot;
        public Text legacyCountdown;
        public BeachPresentationScaleTrack[] openingTracks, victoryTracks, defeatTracks;
        public Image fade;
        public AnimationCurve fadeIn, fadeOut;
        public BeachPresentationCameraTrack introCamera = new BeachPresentationCameraTrack();
        public bool IsBound => fade != null;

        public void FrameOpeningCamera(float seconds)
        {
            if (introCamera != null) introCamera.Sample(seconds);
        }

        public void Render(BeachPresentationTimeline timeline, bool victory)
        {
            if (!IsBound) return;
            if (legacyCountdown) legacyCountdown.gameObject.SetActive(false);
            if (openingRoot) openingRoot.SetActive(timeline.OpeningActive && !timeline.OpeningComplete);
            if (timeline.OpeningActive) Sample(openingTracks, timeline.OpeningTime);
            if (timeline.ResultActive) Sample(victory ? victoryTracks : defeatTracks, timeline.ResultTime);
            float alpha = timeline.TransitionActive ? fadeOut.Evaluate(timeline.TransitionTime) : fadeIn.Evaluate(timeline.SceneTime);
            fade.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));
            fade.raycastTarget = timeline.TransitionActive;
            fade.gameObject.SetActive(alpha > 0.0001f || timeline.TransitionActive);
        }

        private static void Sample(BeachPresentationScaleTrack[] tracks, float seconds)
        {
            if (tracks == null) return;
            foreach (var track in tracks) track?.Sample(seconds);
        }
    }
}
