using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class BeachNativeNpcActor : MonoBehaviour
    {
        private const float WorldScale = BeachNativeNpcManager.WorldScale;
        private const float ArrivalDistance = 3f * WorldScale;
        private static readonly string[] Idle = { "ChestIdle", "ArmsIdle", "BackIdle", "LegsIdle" };
        private static readonly string[] MoveAnimation = { "ChestWorkout", "ArmsWorkout", "BackWorkout", "LegsWorkout", "Squatting" };

        [Serializable]
        public sealed class MusclePose
        {
            public Muscle group;
            public Renderer renderer;
            public BeachMuscleView.Pose start, end;
        }

        private enum ActionKind { Leave, Decide, FindStation, Interact, StartWorkout, ExitWorkout, TryJump, Grounded }

        private readonly struct ScheduledAction
        {
            internal readonly ActionKind kind;
            internal readonly double time;
            internal ScheduledAction(ActionKind kind, double time)
            {
                this.kind = kind;
                this.time = time;
            }
        }

        [SerializeField] private Animator rig;
        [SerializeField] private Rigidbody body;
        [SerializeField] private OriginalSdfText nameLabel, rankLabel, levelLabel;
        [SerializeField] private MusclePose[] muscles = Array.Empty<MusclePose>();

        private readonly List<ScheduledAction> actions = new List<ScheduledAction>(8);
        private readonly float[] muscleFill = new float[4];
        private bool goingToArena, goingToStation, tryJump;
        private int targetStation = -1;
        private string moveAnimation;

        internal int Level { get; private set; } = 1;
        internal int OccupiedStation { get; private set; } = -1;

        internal void Spawn(BeachNativeNpcManager manager, Vector3 position, Quaternion rotation, bool oldNpc)
        {
            ResetPrivateState();
            transform.localScale = Vector3.one * WorldScale;
            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);
            SetWorkoutPhysics(false);
            StopVelocity();
            if (rig && rig.runtimeAnimatorController)
            {
                rig.Rebind();
                rig.Update(0);
                SetBool("Grounded", true);
                SetBool("Carpet", false);
                ClearWorkout();
                SetBool("Walking", false);
                rig.SetFloat("WorkoutSpeed", .5f);
            }

            double now = manager.Clock;
            if (oldNpc)
            {
                double findAt = now + Random.Range(0f, .222f);
                Schedule(ActionKind.FindStation, findAt);
                Schedule(ActionKind.Interact, findAt + .1f);
            }
            else Schedule(ActionKind.Decide, now);
            string displayName = manager.GenerateName();
            Schedule(ActionKind.Leave, now + Random.Range(30, 300));
            Level = Random.Range(1, 11);
            if (nameLabel) nameLabel.text = displayName;
            if (rankLabel) rankLabel.text = BeachProgressionRules.RankTitle((Mathf.Min(Level, 6) - 1) * 5);
            if (levelLabel) levelLabel.text = "LVL " + Level;
            ApplyMusclePoses();

            Schedule(ActionKind.TryJump, now + Random.Range(.2f, 3f));
        }

        private void ApplyMusclePoses()
        {
            muscleFill[(int)Muscle.Chest] = Random.Range(.1f, 1f);
            muscleFill[(int)Muscle.Arms] = Random.Range(.1f, 1f);
            muscleFill[(int)Muscle.Legs] = Random.Range(.1f, 1f);
            muscleFill[(int)Muscle.Back] = Random.Range(.1f, 1f);
            foreach (var muscle in muscles)
            {
                float fill = muscleFill[(int)muscle.group];
                var part = muscle.renderer.transform;
                part.localPosition = Vector3.Lerp(muscle.start.position, muscle.end.position, fill);
                part.localRotation = Quaternion.Lerp(muscle.start.rotation, muscle.end.rotation, fill);
                part.localScale = Vector3.Lerp(muscle.start.scale, muscle.end.scale, fill);
                muscle.renderer.enabled = true;
            }
        }

        internal void Despawn(BeachNativeNpcManager manager)
        {
            manager.Release(this);
            ResetPrivateState();
            if (gameObject.activeSelf)
            {
                ClearWorkout();
                SetBool("Walking", false);
                StopVelocity();
                gameObject.SetActive(false);
            }
        }

        private void ResetPrivateState()
        {
            actions.Clear();
            goingToArena = goingToStation = tryJump = false;
            targetStation = OccupiedStation = -1;
            moveAnimation = null;
        }

        private void Schedule(ActionKind kind, double at)
        {
            int index = actions.Count;
            while (index > 0 && actions[index - 1].time > at) index--;
            actions.Insert(index, new ScheduledAction(kind, at));
        }

        internal void Advance(BeachNativeNpcManager manager)
        {
            double now = manager.Clock;
            while (actions.Count > 0 && now >= actions[0].time)
            {
                ActionKind next = actions[0].kind;
                actions.RemoveAt(0);
                switch (next)
                {
                    case ActionKind.Leave:
                        manager.Leave(this);
                        return;
                    case ActionKind.Decide:
                        Decide(manager);
                        break;
                    case ActionKind.FindStation:
                        FindStation(manager);
                        break;
                    case ActionKind.Interact:
                        if (OccupiedStation < 0 && !manager.TryOccupy(targetStation, this)) Decide(manager);
                        break;
                    case ActionKind.StartWorkout:
                        if (OccupiedStation >= 0) SetBool(moveAnimation, true);
                        break;
                    case ActionKind.ExitWorkout:
                        manager.Release(this);
                        break;
                    case ActionKind.TryJump:
                        tryJump = true;
                        Schedule(ActionKind.TryJump, now + Random.Range(1, 4));
                        break;
                    case ActionKind.Grounded:
                        SetBool("Grounded", true);
                        break;
                }
            }
        }

        private void Decide(BeachNativeNpcManager manager)
        {
            SetBool("Walking", true);
            if (Random.Range(0, 5) != 0)
            {
                FindStation(manager);
                if (targetStation >= 0)
                {
                    goingToArena = false;
                    goingToStation = true;
                }
                return;
            }
            SelectArena(manager.Arena);
        }

        private void FindStation(BeachNativeNpcManager manager)
        {
            targetStation = manager.FindStation();
            if (targetStation < 0)
            {
                // With no free station the original recurses until it rolls the arena branch.
                SetBool("Walking", true);
                SelectArena(manager.Arena);
                return;
            }
            var station = manager.GetStation(targetStation);
            Face(station.target ? station.target : station.playerPos);
        }

        private void SelectArena(Transform arena)
        {
            targetStation = -1;
            goingToStation = false;
            goingToArena = arena;
            Face(arena);
        }

        private void Face(Transform target)
        {
            if (!target) return;
            Vector3 offset = target.position - transform.position;
            offset.y = 0;
            if (offset.sqrMagnitude > 0) transform.rotation = Quaternion.LookRotation(offset);
        }

        internal void Move(BeachNativeNpcManager manager, float seconds)
        {
            if (goingToStation && !manager.GetStation(targetStation).Available) Decide(manager);
            if (goingToStation)
            {
                var station = manager.GetStation(targetStation);
                var target = station.target ? station.target : station.playerPos;
                if (Arrived(target))
                {
                    goingToStation = false;
                    SetBool("Walking", false);
                    Schedule(ActionKind.Interact, manager.Clock + Random.Range(.1f, .3f));
                }
                else Walk(manager.Clock, seconds);
            }
            else if (goingToArena)
            {
                if (Arrived(manager.Arena))
                {
                    goingToArena = false;
                    SetBool("Walking", false);
                    Schedule(ActionKind.Leave, manager.Clock + Random.Range(1, 3));
                }
                else Walk(manager.Clock, seconds);
            }
            tryJump = false;
        }

        private bool Arrived(Transform target)
        {
            Vector3 offset = target.position - transform.position;
            offset.y = 0;
            return offset.sqrMagnitude < ArrivalDistance * ArrivalDistance;
        }

        private void Walk(double now, float seconds)
        {
            if (tryJump)
            {
                if (body)
                {
                    Vector3 velocity = body.velocity;
                    velocity.y = 33 * WorldScale;
                    body.velocity = velocity;
                }
                SetBool("Grounded", false);
                Schedule(ActionKind.Grounded, now + .7f);
            }
            transform.position += transform.forward * (25 * WorldScale * seconds);
        }

        internal void EnterStation(int index, BeachNativeNpcManager.Station station, double now)
        {
            OccupiedStation = index;
            goingToStation = goingToArena = false;
            StopVelocity();
            SetWorkoutPhysics(true);
            transform.SetPositionAndRotation(station.playerPos.position, Quaternion.identity);
            SetBool("Walking", false);
            foreach (var idle in Idle) SetBool(idle, false);
            SetBool(station.idle, true);
            SetBool("WorkingOut", true);
            moveAnimation = station.move;
            float duration = Random.Range(10, 90);
            Schedule(ActionKind.StartWorkout, now + .2f);
            Schedule(ActionKind.ExitWorkout, now + duration);
            Schedule(ActionKind.Decide, now + duration + Random.Range(.3f, 1f));
        }

        internal void ExitStation(BeachNativeNpcManager.Station station)
        {
            OccupiedStation = -1;
            moveAnimation = null;
            if (station.exitPos) transform.SetPositionAndRotation(station.exitPos.position, Quaternion.identity);
            ClearWorkout();
            SetWorkoutPhysics(false);
            StopVelocity();
        }

        private void SetWorkoutPhysics(bool workingOut)
        {
            if (!body) return;
            body.useGravity = !workingOut;
            body.isKinematic = workingOut;
        }

        private void ClearWorkout()
        {
            foreach (var idle in Idle) SetBool(idle, false);
            foreach (var move in MoveAnimation) SetBool(move, false);
            SetBool("WorkingOut", false);
        }

        private void SetBool(string parameter, bool value)
        {
            if (rig && rig.runtimeAnimatorController && !string.IsNullOrEmpty(parameter)) rig.SetBool(parameter, value);
        }

        private void StopVelocity()
        {
            if (!body || body.isKinematic) return;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

#if UNITY_EDITOR
        public void Bind(Animator animator, OriginalSdfText nameText, OriginalSdfText titleText,
            OriginalSdfText levelText, MusclePose[] bindings)
        {
            rig = animator;
            nameLabel = nameText;
            rankLabel = titleText;
            levelLabel = levelText;
            muscles = bindings;
        }

        public void ConfigureBody()
        {
            gameObject.layer = 8;
            body = GetComponent<Rigidbody>();
            body.mass = 1;
            body.drag = 0;
            body.angularDrag = .05f;
            body.useGravity = true;
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            var capsule = GetComponent<CapsuleCollider>();
            capsule.enabled = true;
            capsule.direction = 1;
            // Match the player capsule at the same world scale. Keep the capsule base at the actor origin.
            capsule.radius = .267f / WorldScale;
            capsule.height = 1.784899f / WorldScale;
            capsule.center = Vector3.up * (capsule.height * .5f);
            capsule.isTrigger = false;
            capsule.sharedMaterial = null;
        }

        public void AlignModelToFloor()
        {
            if (!rig || rig.transform.parent.parent != transform) return;
            var model = rig.transform.parent;
            var position = model.localPosition;
            // The imported idle pose places the sole 0.173 world units above the origin at NPC scale.
            position.y = -.173f / WorldScale;
            model.localPosition = position;
        }
#endif
    }
}
