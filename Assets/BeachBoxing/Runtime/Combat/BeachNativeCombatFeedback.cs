using System;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    public sealed class BeachNativeCombatFeedback : MonoBehaviour
    {
        private const float DamageLifetime = .5f, EnergyLifetime = 1.5f, MarkerLifetime = 1f;
        private const float LowHealthPeriod = .6666667f, BlockHintPeriod = .5f;

        public GameObject screen, takeDamage, lowHealth, healing, outOfStamina, blockHint;
        public Image damageImage, lowHealthImage, healingImage;
        public Transform blockHintPulse;
        public AudioSource damageAudio, healingAudio, playerGuardAudio;
        public Transform[] markerRoots = Array.Empty<Transform>();
        public Text[] markerLabels = Array.Empty<Text>();
        public Transform[] markerSpawns = Array.Empty<Transform>();
        public BeachNativeBoxingAudio[] opponentAudio = Array.Empty<BeachNativeBoxingAudio>();

        [Serializable]
        public sealed class ScaleTrack
        {
            public AnimationCurve x, y, z;

            public Vector3 Evaluate(float time)
                => new Vector3(x.Evaluate(time), y.Evaluate(time), z.Evaluate(time));
        }

        public AnimationCurve damageAlpha, lowHealthAlpha, markerAnchoredY, markerAlpha;
        public ScaleTrack outOfStaminaScale, blockHintScale, markerScale;

        private float damageTime, lowHealthTime, healingTime, energyTime, blockHintTime;
        private int opponentIndex = -1, markerCursor;
        private float[] markerAges = Array.Empty<float>();

        public bool IsBound => screen && takeDamage && lowHealth && healing && outOfStamina && blockHint &&
            markerRoots.Length > 0 && markerLabels.Length == markerRoots.Length &&
            markerSpawns.Length == NativeOpponentCatalog.PlayableCount && opponentAudio.Length == markerSpawns.Length;

        private void Awake() => ResetEffects();

        private void Update() => AdvanceEffects(Time.deltaTime);

        private void OnDisable() => ResetEffects();

        public void SelectOpponent(int index)
        {
            opponentIndex = index;
            ResetEffects();
            if (index >= 0 && index < opponentAudio.Length && opponentAudio[index])
                opponentAudio[index].ResetImpactChannels();
        }

        public void Render(FightController fight, bool screenVisible, bool zeroStaminaPunchRequested)
        {
            if (fight == null)
            {
                SetScreen(false);
                return;
            }

            Render(fight.Player.Health, fight.Player.Stamina, fight.Player.Blocking,
                screenVisible, zeroStaminaPunchRequested);
        }

        private void Render(float health, float stamina, bool guarding, bool visible, bool emptyPunch)
        {
            SetScreen(visible);
            if (!visible || !screen) return;

            if (SetActive(lowHealth, health <= 15f))
            {
                lowHealthTime = 0;
                SetAlpha(lowHealthImage, lowHealthAlpha, 0);
            }

            if (SetActive(healing, guarding) && guarding)
            {
                healingTime = 0;
                SetAlpha(healingImage, lowHealthAlpha, 0);
                BeachNativeBoxingAudio.Play(healingAudio, 1.2f, 1f);
            }

            if (emptyPunch && SetActive(outOfStamina, true))
            {
                energyTime = 0;
                SetScale(outOfStamina.transform, outOfStaminaScale, 0);
            }

            if (SetActive(blockHint, stamina <= 15f))
            {
                blockHintTime = 0;
                SetScale(blockHintPulse, blockHintScale, 0);
            }
        }

        public void PlayerHit(bool blocked)
        {
            if (blocked)
            {
                if (playerGuardAudio) playerGuardAudio.Play();
                return;
            }

            if (!screen || !screen.activeInHierarchy || !takeDamage) return;
            SetActive(takeDamage, false);
            SetActive(takeDamage, true);
            damageTime = 0;
            SetAlpha(damageImage, damageAlpha, 0);
            BeachNativeBoxingAudio.Play(damageAudio, 1.2f, .8f);
        }

        public void EnemyHit(int index, bool blocked, int damage)
        {
            if (index != opponentIndex || index < 0 || index >= markerSpawns.Length) return;
            if (index < opponentAudio.Length && opponentAudio[index]) opponentAudio[index].Impact(blocked);
            if (blocked || !markerSpawns[index] || markerRoots.Length == 0) return;

            int slot = markerCursor;
            for (int offset = 0; offset < markerRoots.Length; offset++)
            {
                int candidate = (markerCursor + offset) % markerRoots.Length;
                if (markerRoots[candidate].gameObject.activeSelf) continue;
                slot = candidate;
                break;
            }

            var root = markerRoots[slot];
            var label = markerLabels[slot];
            var spawn = markerSpawns[index];
            markerCursor = (slot + 1) % markerRoots.Length;
            markerAges[slot] = 0;
            root.SetPositionAndRotation(spawn.position, spawn.rotation);
            root.localScale = Vector3.one * (3f * FightRules.ReferenceWorldScale);
            label.text = "-" + damage.ToString(System.Globalization.CultureInfo.InvariantCulture);
            label.transform.localScale = Vector3.zero;
            SampleMarker(label, 0);
            root.gameObject.SetActive(true);
        }

        public void ResetEffects()
        {
            SetScreen(false);
            if (markerAges.Length != markerRoots.Length) markerAges = new float[markerRoots.Length];
            for (int slot = 0; slot < markerRoots.Length; slot++)
            {
                markerAges[slot] = 0;
                if (markerRoots[slot]) SetActive(markerRoots[slot].gameObject, false);
            }
            markerCursor = 0;
        }

        private void SetScreen(bool visible)
        {
            if (!visible)
            {
                SetActive(takeDamage, false);
                SetActive(lowHealth, false);
                SetActive(healing, false);
                SetActive(outOfStamina, false);
                SetActive(blockHint, false);
                damageTime = lowHealthTime = healingTime = energyTime = blockHintTime = 0;
            }
            if (SetActive(screen, visible) && visible)
                BeachNativeBoxingAudio.Tune(playerGuardAudio, 1.2f, .8f);
        }

        private void AdvanceEffects(float seconds)
        {
            if (seconds <= 0) return;
            if (IsActive(takeDamage))
            {
                damageTime += seconds;
                if (damageTime >= DamageLifetime) SetActive(takeDamage, false);
                else SetAlpha(damageImage, damageAlpha, damageTime);
            }
            if (IsActive(lowHealth))
            {
                lowHealthTime = Mathf.Repeat(lowHealthTime + seconds, LowHealthPeriod);
                SetAlpha(lowHealthImage, lowHealthAlpha, lowHealthTime);
            }
            if (IsActive(healing))
            {
                healingTime = Mathf.Repeat(healingTime + seconds, LowHealthPeriod);
                SetAlpha(healingImage, lowHealthAlpha, healingTime);
            }
            if (IsActive(outOfStamina))
            {
                energyTime += seconds;
                if (energyTime >= EnergyLifetime) SetActive(outOfStamina, false);
                else SetScale(outOfStamina.transform, outOfStaminaScale, energyTime);
            }
            if (IsActive(blockHint))
            {
                blockHintTime = Mathf.Repeat(blockHintTime + seconds, BlockHintPeriod);
                SetScale(blockHintPulse, blockHintScale, blockHintTime);
            }
            for (int slot = 0; slot < markerRoots.Length; slot++)
            {
                var root = markerRoots[slot];
                if (!root || !root.gameObject.activeSelf) continue;
                markerAges[slot] += seconds;
                if (markerAges[slot] >= MarkerLifetime) root.gameObject.SetActive(false);
                else SampleMarker(markerLabels[slot], markerAges[slot]);
            }
        }

        private void SampleMarker(Text label, float elapsed)
        {
            var rect = label.rectTransform;
            SetScale(rect, markerScale, elapsed);
            if (markerAnchoredY != null)
            {
                var position = rect.anchoredPosition;
                position.y = markerAnchoredY.Evaluate(elapsed);
                rect.anchoredPosition = position;
            }
            SetAlpha(label, markerAlpha, elapsed);
        }

        private static bool IsActive(GameObject target) => target && target.activeInHierarchy;

        private static bool SetActive(GameObject target, bool value)
        {
            if (!target || target.activeSelf == value) return false;
            target.SetActive(value);
            return true;
        }

        private static void SetAlpha(Graphic image, AnimationCurve curve, float elapsed)
        {
            if (!image || curve == null) return;
            var color = image.color;
            color.a = curve.Evaluate(elapsed);
            image.color = color;
        }

        private static void SetScale(Transform target, ScaleTrack track, float elapsed)
        {
            if (target && track != null) target.localScale = track.Evaluate(elapsed);
        }
    }
}
