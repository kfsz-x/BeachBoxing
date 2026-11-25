using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    public sealed class BeachNativeNpcManager : MonoBehaviour
    {
        public const float WorldScale = .356f;
        private const double FpsWindow = 3;
        private const float MinimumFps = 40;

        [Serializable]
        public sealed class Station
        {
            public int sourceId;
            public int playerMuscle = -1;
            public bool playerOnly;
            public bool unavailableAtStart;
            public Transform target, playerPos, exitPos, holder;
            public GameObject pivot;
            public Vector3 holderRestScale = Vector3.one;
            public Vector3 holderRestPosition, holderPivotOffset;
            public string idle, move;

            [NonSerialized] internal BeachNativeNpcActor npc;
            [NonSerialized] internal bool player;
            internal bool Available => playerPos && !unavailableAtStart && !npc && !player;

            internal void ShowOccupied(bool occupied, int level = 1)
            {
                if (pivot) pivot.SetActive(!occupied);
                if (!holder) return;
                float scale = occupied ? BeachProgressionRules.RootScale(level) : 1f;
                holder.localScale = holderRestScale * scale;
                holder.localPosition = holderRestPosition + holderPivotOffset * (1f - scale);
            }
        }

        private readonly struct FpsSample
        {
            internal readonly double time;
            internal readonly float fps;
            internal FpsSample(double time, float fps)
            {
                this.time = time;
                this.fps = fps;
            }
        }

        [SerializeField] private BeachNativeNpcActor[] actors = Array.Empty<BeachNativeNpcActor>();
        [SerializeField] private Station[] stations = Array.Empty<Station>();
        [SerializeField] private Transform[] oldSpawnPoints = Array.Empty<Transform>();
        [SerializeField] private Transform newSpawnPoint, arena;
        [SerializeField] private string[] fakeNames = Array.Empty<string>();
        [SerializeField] private string[] adjectives = Array.Empty<string>();
        [SerializeField] private string[] nouns = Array.Empty<string>();
        [SerializeField] private GameObject connectedAlert, disconnectedAlert;

        private readonly Queue<FpsSample> samples = new Queue<FpsSample>();
        private readonly List<double> replacements = new List<double>(3);
        private readonly List<int> availableStations = new List<int>(7);
        private double clock, windowElapsed, sampleTotal;
        private bool phone, beachActive, spawned;
        private int failures;

        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var actor in actors)
                    if (actor && actor.gameObject.activeSelf) count++;
                return count;
            }
        }

        internal Transform Arena => arena;
        internal double Clock => clock;

        public void ResetScene(bool phone)
        {
            beachActive = false;
            this.phone = phone;
            foreach (var actor in actors)
                if (actor) actor.Despawn(this);
            foreach (var station in stations)
            {
                if (!station.player && !station.npc) continue;
                station.player = false;
                station.npc = null;
                station.ShowOccupied(false);
            }
            replacements.Clear();
            samples.Clear();
            clock = windowElapsed = sampleTotal = 0;
            failures = 0;
            spawned = false;
            SetAlertVisible(connectedAlert, false);
            SetAlertVisible(disconnectedAlert, false);
        }

        public void SetBeachActive(bool active)
        {
            beachActive = active;
            if (active) return;
            SetAlertVisible(connectedAlert, false);
            SetAlertVisible(disconnectedAlert, false);
        }

        public bool IsPlayerStationAvailable(Muscle muscle)
        {
            Station station = PlayerStation(muscle);
            return station == null || station.Available;
        }

        public void PlayerEntered(Muscle muscle, int level)
        {
            var station = PlayerStation(muscle);
            if (station == null || !station.Available) return;
            station.player = true;
            station.ShowOccupied(true, level);
        }

        public void PlayerExited(Muscle muscle)
        {
            var station = PlayerStation(muscle);
            if (station == null || !station.player) return;
            station.player = false;
            station.ShowOccupied(false);
        }

        private Station PlayerStation(Muscle muscle)
        {
            foreach (var station in stations)
                if (station.playerMuscle == (int)muscle) return station;
            return null;
        }

        private void Update()
        {
            float seconds = Time.deltaTime;
            if (seconds > 0) Tick(seconds, 1f / seconds);
        }

        private void FixedUpdate()
        {
            if (!beachActive || phone) return;
            foreach (var actor in actors)
                if (actor && actor.gameObject.activeInHierarchy) actor.Move(this, Time.fixedDeltaTime);
        }

        private void Tick(float seconds, float fps)
        {
            if (!beachActive || phone || seconds <= 0) return;
            clock += seconds;

            foreach (var actor in actors)
                if (actor && actor.gameObject.activeSelf) actor.Advance(this);
            SpawnReplacements();
            CheckFps(seconds, fps);
        }

        private void CheckFps(float seconds, float fps)
        {
            if (failures > 2) return;
            if (fps > 0 && !float.IsInfinity(fps))
            {
                samples.Enqueue(new FpsSample(clock, fps));
                sampleTotal += fps;
            }
            while (samples.Count > 0 && clock - samples.Peek().time > FpsWindow)
                sampleTotal -= samples.Dequeue().fps;
            windowElapsed += seconds;
            if (windowElapsed < FpsWindow) return;
            windowElapsed = 0;

            if (samples.Count == 0 || sampleTotal / samples.Count < MinimumFps)
            {
                failures++;
                foreach (var actor in actors)
                {
                    if (!actor || !actor.gameObject.activeSelf) continue;
                    actor.Despawn(this);
                }

                // No more checks after the third failure. Scheduled replacements belong to the spawner.
                if (failures > 2) samples.Clear();
                return;
            }

            if (spawned) return;
            spawned = true;
            foreach (var actor in actors)
            {
                if (!actor || oldSpawnPoints.Length == 0) continue;
                var point = oldSpawnPoints[Random.Range(0, oldSpawnPoints.Length)];
                actor.Spawn(this, point.position, Quaternion.Euler(0, Random.Range(0, 360), 0), true);
            }
        }

        private void SpawnReplacements()
        {
            while (replacements.Count > 0 && clock >= replacements[0])
            {
                BeachNativeNpcActor slot = null;
                foreach (var actor in actors)
                {
                    if (!actor || actor.gameObject.activeSelf) continue;
                    slot = actor;
                    break;
                }
                if (slot && newSpawnPoint)
                    slot.Spawn(this, newSpawnPoint.position, Quaternion.identity, false);
                replacements.RemoveAt(0);
            }
        }

        internal void Leave(BeachNativeNpcActor actor)
        {
            actor.Despawn(this);
            double at = clock + Random.Range(3, 10);
            int index = replacements.Count;
            while (index > 0 && replacements[index - 1] > at) index--;
            replacements.Insert(index, at);
        }

        internal int FindStation()
        {
            availableStations.Clear();
            for (int i = 0; i < stations.Length; i++)
                if (!stations[i].playerOnly && stations[i].Available) availableStations.Add(i);
            return availableStations.Count == 0 ? -1 : availableStations[Random.Range(0, availableStations.Count)];
        }

        internal Station GetStation(int index) => stations[index];

        internal bool TryOccupy(int index, BeachNativeNpcActor actor)
        {
            if (index < 0 || !actor || !actor.gameObject.activeSelf || actor.OccupiedStation >= 0) return false;
            Station station = stations[index];
            if (!station.Available || station.playerOnly) return false;
            station.npc = actor;
            actor.EnterStation(index, station, clock);
            station.ShowOccupied(true);
            return true;
        }

        internal void Release(BeachNativeNpcActor actor)
        {
            int index = actor.OccupiedStation;
            if (index < 0 || stations[index].npc != actor) return;
            Station station = stations[index];
            station.npc = null;
            station.ShowOccupied(false);
            actor.ExitStation(station);
        }

        internal string GenerateName()
        {
            bool hasCombo = adjectives.Length > 0 && nouns.Length > 0;
            if (fakeNames.Length > 0 && (!hasCombo || Random.Range(1, 4) == 1))
            {
                string picked = PickUsableFakeName();
                if (!string.IsNullOrEmpty(picked)) return picked;
            }
            if (!hasCombo) return string.Empty;
            return adjectives[Random.Range(0, adjectives.Length)] + " " + nouns[Random.Range(0, nouns.Length)];
        }

        private string PickUsableFakeName()
        {
            int count = 0;
            for (int i = 0; i < fakeNames.Length; i++)
                if (IsUsableFakeName(fakeNames[i])) count++;
            if (count == 0) return string.Empty;
            int target = Random.Range(0, count);
            for (int i = 0; i < fakeNames.Length; i++)
            {
                if (!IsUsableFakeName(fakeNames[i])) continue;
                if (target-- == 0) return fakeNames[i].Trim();
            }
            return string.Empty;
        }

        public static bool IsUsableFakeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string trimmed = value.Trim();
            if (trimmed.Contains("...") || trimmed.Contains("__")) return false;
            string translated = BeachChineseText.Translate(trimmed);
            if (string.IsNullOrWhiteSpace(translated) || translated.Length > 12) return false;
            int letters = 0;
            bool hasCjk = false;
            for (int i = 0; i < translated.Length; i++)
            {
                char c = translated[i];
                if (c >= '\u4e00' && c <= '\u9fff')
                {
                    hasCjk = true;
                    letters++;
                }
                else if (char.IsLetter(c))
                {
                    letters++;
                }
            }
            return hasCjk ? letters >= 1 : letters >= 2;
        }

        private static void SetAlertVisible(GameObject alert, bool visible)
        {
            if (alert && alert.activeSelf != visible) alert.SetActive(visible);
        }

#if UNITY_EDITOR
        public void Bind(BeachNativeNpcActor[] pool, Station[] equipment, Transform[] oldPoints, Transform newPoint,
            Transform arenaPoint, string[] names, string[] nameAdjectives, string[] nameNouns,
            GameObject connected, GameObject disconnected)
        {
            actors = pool;
            stations = equipment;
            oldSpawnPoints = oldPoints;
            newSpawnPoint = newPoint;
            arena = arenaPoint;
            fakeNames = names;
            adjectives = nameAdjectives;
            nouns = nameNouns;
            connectedAlert = connected;
            disconnectedAlert = disconnected;
            SetAlertVisible(connected, false);
            SetAlertVisible(disconnected, false);
        }
#endif
    }
}
