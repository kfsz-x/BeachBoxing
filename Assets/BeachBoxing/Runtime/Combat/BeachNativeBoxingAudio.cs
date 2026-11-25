using System;
using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Receives native clip events on the actual model Animator GameObject.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class BeachNativeBoxingAudio : MonoBehaviour
    {
        public AudioSource step1, step2, jump, land;
        public AudioSource[] swings = Array.Empty<AudioSource>();
        public AudioSource[] guardHits = Array.Empty<AudioSource>();
        public AudioSource[] healthHits = Array.Empty<AudioSource>();
        private int swingCursor, guardCursor, healthCursor;

        private void OnEnable()
        {
            GetComponent<Animator>().fireEvents = true;
            Tune(step1, 1.1f, .7f);
            Tune(step2, 1.1f, .7f);
            Tune(jump, 1.3f, .7f);
            Tune(land, 1.3f, .7f);
        }

        public void ExecuteEvent() => PlayNext(swings, ref swingCursor);
        public void PlayAudio1() { if (step1) step1.Play(); }
        public void PlayAudio2() { if (step2) step2.Play(); }
        public void PlayAudio3() { if (jump) jump.Play(); }
        public void PlayAudio4() { if (land) land.Play(); }

        public void Impact(bool blocked)
        {
            if (blocked) PlayNext(guardHits, ref guardCursor);
            else PlayNext(healthHits, ref healthCursor);
        }

        public void ResetImpactChannels() => guardCursor = healthCursor = 0;

        internal static void Tune(AudioSource source, float min, float max)
        {
            if (source) source.pitch = UnityEngine.Random.Range(min, max);
        }

        internal static void Play(AudioSource source, float min, float max)
        {
            if (!source) return;
            Tune(source, min, max);
            source.Play();
        }

        private static void PlayNext(AudioSource[] sources, ref int cursor)
        {
            if (sources.Length == 0) return;
            Play(sources[cursor], 1.2f, .8f);
            cursor = (cursor + 1) % sources.Length;
        }
    }
}
