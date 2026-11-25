using System;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing
{
    /// <summary>Preplaced source UIParticle. Tick owns scaled lifetime/clip/audio; FixedUpdate alone owns flight.</summary>
    public sealed class BeachNativeRewardParticle : MonoBehaviour
    {
        public Transform target;
        public Text[] labels = Array.Empty<Text>();
        public AnimationClip clip;
        public AudioSource checkpointAudio, collectionAudio;
        public float checkpointPitchMin = 1.1f, checkpointPitchMax = .9f;
        public float collectionPitchMin = 1.1f, collectionPitchMax = .9f;
        public float FlightClock { get; private set; } = -.15f;
        public bool CollectionPlayed { get; private set; }
        float elapsed;
        public void Begin(Vector3 worldPosition, Quaternion worldRotation, int gain)
        {
            Cancel(); elapsed = 0; FlightClock = -.15f; CollectionPlayed = false;
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            foreach (var label in labels) if (label) label.text = "+" + gain;
            gameObject.SetActive(true);
            if (clip) clip.SampleAnimation(gameObject, 0);
            if (checkpointAudio) { checkpointAudio.pitch = UnityEngine.Random.Range(checkpointPitchMin, checkpointPitchMax); checkpointAudio.Play(); }
        }
        public void Tick(float dt)
        {
            if (!gameObject.activeSelf || dt <= 0) return;
            elapsed += dt;
            if (elapsed >= 1.5f) { Cancel(); return; }
            if (clip) clip.SampleAnimation(gameObject, elapsed);
            if (!CollectionPlayed && elapsed >= .9166667f)
            {
                CollectionPlayed = true;
                if (collectionAudio) { collectionAudio.gameObject.SetActive(true); collectionAudio.pitch = UnityEngine.Random.Range(collectionPitchMin, collectionPitchMax); collectionAudio.Play(); }
            }
        }
        void FixedUpdate() => FixedTick(Time.fixedDeltaTime);
        public void FixedTick(float dt)
        {
            if (!gameObject.activeSelf || !target || dt <= 0) return;
            FlightClock += dt;
            if (FlightClock > 0) transform.position = Vector3.Lerp(transform.position, target.position, dt * FlightClock * 12);
        }
        public void Cancel()
        {
            if (checkpointAudio) checkpointAudio.Stop();
            if (collectionAudio) { collectionAudio.Stop(); collectionAudio.gameObject.SetActive(false); }
            gameObject.SetActive(false);
        }
    }
}
