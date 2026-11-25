using System;
using UnityEngine;

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    public sealed class BeachTrainerFollowers : MonoBehaviour
    {
        private const float WorldScale = 0.356f;
        private const float WalkSpeed = 20f * WorldScale;
        private const float RoamingDistance = 7f * WorldScale;
        private const float WorkoutDistance = WorldScale;
        private static readonly int Walking = Animator.StringToHash("Walking");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Cheer = Animator.StringToHash("Cheer");
        private static readonly int Cheer2 = Animator.StringToHash("Cheer2");

        [Serializable]
        public sealed class Model
        {
            public Transform root;
            public Animator animator;
            public Transform title;
            public OriginalSdfText levelLabel;
            [NonSerialized] internal Rigidbody body;
            [NonSerialized] internal CapsuleCollider capsule;
        }

        [SerializeField] private Model[] models = Array.Empty<Model>();
        [SerializeField] private Camera viewCamera;
        [SerializeField] private LayerMask groundMask = 1 << 3;

        private Model active;
        private Transform player, workoutTarget;
        private int displayedLevel;
        private float cheerDelay;
        private bool hasCheerAnimations;

        public void Apply(BeachPeripheralState state, Transform playerRoot, bool workoutActive,
            Transform trainerWorkoutTarget = null)
        {
            int id = state?.equippedTrainer ?? -1;
            var levels = state?.trainerLevels;
            int level = (uint)id < BeachPeripheralRules.TrainerCount && levels?.Length == BeachPeripheralRules.TrainerCount
                ? levels[id] : 0;
            Model next = playerRoot && level > 0 && id < models.Length ? models[id] : null;
            if (next?.root == null) next = null;

            workoutTarget = workoutActive ? trainerWorkoutTarget : null;
            if (active != next || player != playerRoot)
            {
                if (active?.root) active.root.gameObject.SetActive(false);
                active = next;
                player = playerRoot;
                displayedLevel = 0;
                cheerDelay = 0f;
                hasCheerAnimations = false;
                if (active != null)
                {
                    active.body = active.root.GetComponent<Rigidbody>();
                    active.capsule = active.root.GetComponent<CapsuleCollider>();
                    if (active.body)
                    {
                        if (!active.body.isKinematic)
                        {
                            active.body.velocity = Vector3.zero;
                            active.body.angularVelocity = Vector3.zero;
                        }
                        active.body.useGravity = false;
                        active.body.isKinematic = true;
                    }
                    active.root.SetPositionAndRotation(player.position - player.forward * RoamingDistance, Quaternion.identity);
                    active.root.gameObject.SetActive(true);
                    var animator = active.animator;
                    if (animator && animator.runtimeAnimatorController)
                    {
                        animator.SetBool(Walking, false);
                        foreach (var parameter in animator.parameters)
                        {
                            if (parameter.nameHash == Grounded) animator.SetBool(Grounded, true);
                            if (parameter.nameHash == Cheer2) hasCheerAnimations = true;
                        }
                    }
                }
            }

            if (active == null || displayedLevel == level) return;
            displayedLevel = level;
            active.root.localScale = Vector3.one * (WorldScale * (1f + level * 0.01f));
            PlaceOnGround();
            if (active.levelLabel) active.levelLabel.text = $"LVL {level} Trainer";
        }

        private void LateUpdate() => Step(Time.deltaTime);

        private void Step(float seconds)
        {
            if (active == null || !player) return;
            var root = active.root;
            bool workingOut = workoutTarget;
            var direction = (workingOut ? workoutTarget : player).position - root.position;
            direction.y = 0f;
            float minimumDistance = workingOut ? WorkoutDistance : RoamingDistance;
            bool walking = direction.sqrMagnitude >= minimumDistance * minimumDistance;
            var animator = active.animator;
            if (animator && animator.runtimeAnimatorController) animator.SetBool(Walking, walking);

            if (walking)
            {
                root.rotation = Quaternion.LookRotation(direction);
                float distance = Mathf.Min(WalkSpeed * Mathf.Max(0f, seconds), direction.magnitude - minimumDistance);
                root.position += root.forward * distance;
            }
            else if (workingOut)
            {
                var towardPlayer = player.position - root.position;
                towardPlayer.y = 0f;
                if (towardPlayer.sqrMagnitude > 1e-10f) root.rotation = Quaternion.LookRotation(towardPlayer);
                if (seconds > 0f && cheerDelay <= 0f && hasCheerAnimations)
                {
                    animator.SetTrigger(UnityEngine.Random.Range(0, 4) == 0 ? Cheer : Cheer2);
                    cheerDelay = UnityEngine.Random.Range(2, 9);
                }
            }

            cheerDelay -= seconds;
            PlaceOnGround();
            if (active.title && viewCamera) active.title.LookAt(viewCamera.transform.position);
        }

        private void PlaceOnGround()
        {
            var root = active.root;
            // Probe from the player's surface height so a previously fallen trainer can recover.
            Vector3 origin = new Vector3(root.position.x, player.position.y + 3f, root.position.z);
            if (!Physics.Raycast(origin, Vector3.down, out var ground, 8f, groundMask, QueryTriggerInteraction.Ignore)) return;
            float offset = 0f;
            if (active.capsule)
            {
                var capsule = active.capsule;
                float bottom = capsule.center.y - capsule.height * .5f;
                offset = -bottom * root.lossyScale.y;
            }
            var position = root.position;
            position.y = ground.point.y + offset + .01f;
            root.position = position;
        }

#if UNITY_EDITOR
        public void Bind(Model[] sourceModels, Camera camera)
        {
            models = sourceModels;
            viewCamera = camera;
        }
#endif
    }
}
