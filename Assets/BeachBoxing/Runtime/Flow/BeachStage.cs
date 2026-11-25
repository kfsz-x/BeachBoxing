using UnityEngine;
using UnityCore;
using Object = UnityEngine.Object;

namespace BeachBoxing
{
    [System.Serializable]
    internal sealed class BeachStage
    {
        [SerializeField] private Vector3 ring;
        [SerializeField] private Transform beach, arena;
        [SerializeField] private Camera camera;
        [SerializeField] private AudioSource effects;
        [SerializeField] private AudioSource music;
        [SerializeField] private Transform[] equipment = new Transform[4];
        [SerializeField] private Transform cameraPose;
        private Transform CameraPose => cameraPose ? cameraPose : camera.transform;
        private bool snap;
        [SerializeField] public BoxerView Player, Enemy;

        #if UNITY_EDITOR
        public BeachStage(Transform root, TrainingStation[] stations, Vector3 entrance, Vector3 ring)
        {
            this.ring = ring;
            beach = Group(root, "Training beach", Vector3.zero);
            arena = Group(root, "Arena", ring);
            camera = root.GetComponentInChildren<Camera>();
            if (!camera)
            {
                camera = Group(root, "Gameplay camera", Vector3.zero).gameObject.AddComponent<Camera>();
                camera.gameObject.AddComponent<AudioListener>();
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.42f, 0.87f, 0.89f);
            camera.fieldOfView = 57;
            var sky = OriginalArt.Get<Material>("Look/Sky");
            if (sky && sky.shader && sky.shader.name.StartsWith("Skybox"))
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.gameObject.AddComponent<Skybox>().material = sky;
            }
            var light = root.GetComponentInChildren<Light>();
            if (!light) light = Group(root, "Sun", Vector3.zero).gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(42, -25, 0);

            var gymMat = OriginalArt.Get<Material>("Look/Gym Checkers");
            MakeSand(beach);
            PlaceBackdrop(beach);
            PlaceBackdrop(arena);
            effects = root.gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            music = root.gameObject.AddComponent<AudioSource>();
            music.clip = OriginalArt.Get<AudioClip>("Audio/Music");
            music.loop = true;
            music.playOnAwake = false;
            foreach (var station in stations)
            {
                var gym = Group(beach, station.Muscle.ToString(), station.Position);
                AddSurface(gym, "Floor pad", Vector3.up * 0.02f, new Vector3(2.8f, 0.06f, 2.5f), gymMat);
                equipment[(int)station.Muscle] = OriginalArt.Place(gym, station.Muscle.ToString(), "Original equipment", new Vector3(0, 0.03f, 0.25f));
            }
            OriginalArt.Place(beach, "Glow", "Ring entry", entrance);
            MakeRing(Group(beach, "Preview ring", new Vector3(0, 0, 11)));
            MakeSand(arena);
            MakeRing(arena);
            Player = new BoxerView(root, "Player", "Player");
            Enemy = new BoxerView(root, "Opponent", "Ramez");
            beachSpawn = Group(root, "Beach spawn", new Vector3(0, 0, -4));
            playerSpawn = Group(arena, "Player spawn", Vector3.back * 2.2f);
            enemySpawn = Group(arena, "Enemy spawn", Vector3.forward * 2.2f);
            ringEntry = beach.Find("Ring entry");
            beachBounds = Group(beach, "Movement bounds", new Vector3(0, 0, 1));
            beachBounds.localScale = new Vector3(18, 1, 18);
            ringBounds = Group(arena, "Movement bounds", Vector3.zero);
            ringBounds.localScale = new Vector3(6.8f, 1, 6.8f);
            soundClips = new AudioClip[Sounds.Length];
            for (int i = 0; i < Sounds.Length; i++) soundClips[i] = OriginalArt.Get<AudioClip>("Audio/" + Sounds[i]);
        }

        #endif

        [SerializeField] private Transform beachSpawn, playerSpawn, enemySpawn, ringEntry;
        [SerializeField] private Transform beachBounds, ringBounds;
        [SerializeField] private Vector3 beachCameraOffset = new Vector3(0, 5, -7.4f);
        [SerializeField] private Vector3 workoutCameraOffset = new Vector3(2.4f, 2.5f, -3.6f);
        [SerializeField] private AudioClip[] soundClips;
        private static readonly string[] Sounds = { "Training", "Countdown", "Block", "Punch", "HeavyPunch", "Victory", "Defeat" };

        [SerializeField] private Terrain trainingTerrain;
        [SerializeField] private Vector3 beachLookOffset = new Vector3(0, 0.86f, 0);

        [SerializeField] private float walkSpeed = 35f * FightRules.ReferenceWorldScale;

        [SerializeField] private ParticleSystem walkEffect, ambientEffect, sweatEffect;
        [SerializeField] private GameObject victoryEffect;
        [SerializeField] private ParticleSystem[] victoryParticles;
        [SerializeField] private AudioClip footstep;
        private float stepWait;
        private bool nativeAudioEvents;
        private bool phoneMode;
        private int cameraScenePrestige = 1;
        private int playerPrestige = 1;
        [SerializeField] private BeachNativeCombatCamera nativeFightCamera;
        [SerializeField] private BeachNativeBeachCamera nativeBeachCamera;
        [SerializeField] private Vector3[] workoutCameraPositionsLevelTen = new Vector3[4];
        private Vector3 BeachCameraLocalPosition
        {
            get
            {
                var position = phoneMode ? phoneCameraOffset : beachCameraOffset;
                position.z += (phoneMode ? cameraScenePrestige / 2 : cameraScenePrestige) * .356f;
                return position;
            }
        }
        public void SetPhone(bool value)
        {
            phoneMode = value;
            snap = true;
            if (nativeBeachCamera && nativeBeachCamera.Initialized)
                nativeBeachCamera.SetIdlePose(BeachCameraLocalPosition, beachCameraAngles);
        }

        private void StopExerciseEffects()
        {
            if (walkEffect) walkEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (sweatEffect) sweatEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void ResetVictoryEffect()
        {
            if (!victoryEffect) return;
            foreach (var particle in victoryParticles) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            victoryEffect.SetActive(false);
        }

        public void WalkPlayer(Vector2 input, float seconds)
        {
            Vector3 forward = camera.transform.forward;
            forward.y = 0;
            Vector3 right = camera.transform.right;
            right.y = 0;
            Vector3 direction = forward.normalized * input.y + right.normalized * input.x;
            if (locomotion) locomotion.SetDirection(direction);
            else BeachMotor.Walk(Player.Root, new Vector2(direction.x, direction.z), walkSpeed, seconds, BeachBounds);
            if (!locomotion) GroundPlayer();
            bool moving = input.sqrMagnitude >= 1e-10f && (!locomotion || locomotion.Grounded);
            if (walkEffect)
            {
                var emission = walkEffect.emission;
                emission.rateOverTime = moving ? 5 : 0;
                if (moving && !walkEffect.isPlaying) walkEffect.Play();
                if (!moving && walkEffect.isPlaying) walkEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            stepWait -= seconds;
            if (!nativeAudioEvents && moving && stepWait <= 0 && footstep && CoreRoot.Current.Audio.SfxEnabled)
            {
                effects.PlayOneShot(footstep, 0.15f * CoreRoot.Current.Audio.SfxVolume);
                stepWait = 0.32f;
            }
        }

        public void GroundPlayer()
        {
            if (!trainingTerrain) return;
            var point = Player.Root.position;
            float height = trainingTerrain.SampleHeight(point) + trainingTerrain.transform.position.y;
            if (Physics.Raycast(point + Vector3.up * 1.2f, Vector3.down, out var ground, 3f,
                ~(1 << 8), QueryTriggerInteraction.Ignore) && ground.normal.y > 0.6f)
                height = Mathf.Max(height, ground.point.y);
            point.y = height + 0.02f;
            Player.Root.position = point;
        }

        [SerializeField] private float[] trainingAmbient = new float[27];
        [SerializeField] private float[] combatAmbient = new float[27];

        [SerializeField] private Transform trainingWorld, combatWorld;
        [SerializeField] private Cubemap sourceReflection;

        public void PreviewSceneLighting() => ApplyAmbient(trainingWorld && trainingWorld.gameObject.activeInHierarchy);

        private void ApplyAmbient(bool training)
        {
            if (sourceReflection)
            {
                RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
                RenderSettings.customReflection = sourceReflection;
            }
            var world = training ? trainingWorld : combatWorld;
            if (world)
            {
                var point = world.position;
                Shader.SetGlobalVector("_OriginalWorldNormalization", new Vector4(point.x, point.y, point.z, world.lossyScale.x));
            }
            var values = training ? trainingAmbient : combatAmbient;
            if (values == null || values.Length != 27) return;
            var probe = new UnityEngine.Rendering.SphericalHarmonicsL2();
            for (int channel = 0; channel < 3; channel++)
                for (int coefficient = 0; coefficient < 9; coefficient++)
                    probe[channel, coefficient] = values[channel * 9 + coefficient];
            RenderSettings.ambientProbe = probe;
        }

        [SerializeField] private LineRenderer tutorialLine;
        [SerializeField] private Transform tutorialHead;
        [SerializeField] private Transform tutorialArmsTarget;

        public void Guide(PlayerProgress progress, bool roaming)
        {
            if (!tutorialLine) return;
            var target = BeachTutorialRules.Target(progress);
            tutorialLine.enabled = roaming && target != BeachTutorialTarget.None;
            if (!tutorialLine.enabled) return;
            tutorialLine.SetPosition(0, target == BeachTutorialTarget.Arms ?
                (tutorialArmsTarget ? tutorialArmsTarget.position : equipment[(int)Muscle.Arms].position) : ringEntry.position);
            tutorialLine.SetPosition(1, tutorialHead ? tutorialHead.position : Player.Root.position + Vector3.up);
        }

        public void Initialize()
        {
            Player.CacheRestPoses();
            Enemy.CacheRestPoses();
            nativeAudioEvents = Player.Root.GetComponentInChildren<BeachNativeBoxingAudio>(true) != null;
            locomotion = Player.Root.GetComponent<BeachNativeLocomotion>();
            stationTriggers = new SphereCollider[4];
            stationGlows = new GameObject[4];
            nearbyTriggers = new Collider[16];
            for (int i = 0; i < equipment.Length; i++)
            {
                stationTriggers[i] = equipment[i].GetComponent<SphereCollider>();
                var glow = equipment[i].Find("GlowRing");
                stationGlows[i] = glow ? glow.gameObject : null;
            }
            ringTrigger = ringEntry.GetComponent<CapsuleCollider>();
            actorCollider = Player.Root.GetComponent<CapsuleCollider>();
            if (!actorCollider)
            {
                var body = Player.Root.Find("PlayerCollider");
                if (body) actorCollider = body.GetComponent<CapsuleCollider>();
            }
            muscleView = Player.Root.GetComponent<BeachMuscleView>();
            var titleCanvas = Player.Root.Find("PlayerTitleCanvas") ?? Player.Root.Find("Original title canvas");
            if (titleCanvas) titleLabels = titleCanvas.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            SyncAudioState();
        }

        public void SyncAudioState()
        {
            if (!music || CoreRoot.Current == null) return;
            music.volume = 0.4f * CoreRoot.Current.Audio.MusicVolume;
            if (CoreRoot.Current.Audio.MusicEnabled)
            {
                if (music.clip && !music.isPlaying) music.Play();
            }
            else if (music.isPlaying)
            {
                music.Stop();
            }
        }

        public Vector3 Entrance => ringEntry.position;
        [System.NonSerialized] private SphereCollider[] stationTriggers;
        [System.NonSerialized] private BeachNativeLocomotion locomotion;
        public bool PlayerGrounded => !locomotion || locomotion.Grounded;
        public void StopPlayerMovement()
        {
            if (locomotion) locomotion.StopMovement();
        }
        public void StopPlayerVelocity()
        {
            StopPlayerMovement();
            var body = Player.Root.GetComponent<Rigidbody>();
            if (body && !body.isKinematic) body.velocity = Vector3.zero;
        }
        public void JumpPlayer(bool pressed, bool released)
        {
            if (!locomotion) return;
            if (pressed) locomotion.RequestJump();
            if (released) locomotion.ReleaseJump();
        }
        [System.NonSerialized] private GameObject[] stationGlows;
        [System.NonSerialized] private Collider[] nearbyTriggers;
        private CapsuleCollider ringTrigger, actorCollider;
        private BeachMuscleView muscleView;
        private UnityEngine.UI.Text[] titleLabels;
        private float growthCameraHeight;

        public int NearbyStationIndex()
        {
            // Original FixedUpdate queries feet + up in native units, on layer 6.
            const float normalization = 0.356f;
            var query = Player.Root.position + Vector3.up * ((1f + 0.060997963f * Player.Root.lossyScale.y) * normalization);
            int count = Physics.OverlapSphereNonAlloc(query, normalization, nearbyTriggers, 1 << 6, QueryTriggerInteraction.Collide);
            for (int hit = 0; hit < count; hit++)
                for (int index = 0; index < stationTriggers.Length; index++)
                    if (nearbyTriggers[hit] == stationTriggers[index]) return index;
            return -1;
        }

        public bool RingContainsPlayer()
        {
            if (!ringTrigger || !ringTrigger.gameObject.activeInHierarchy) return false;
            if (actorCollider)
                return Physics.ComputePenetration(actorCollider, actorCollider.transform.position, actorCollider.transform.rotation,
                    ringTrigger, ringTrigger.transform.position, ringTrigger.transform.rotation, out _, out _);
            var foot = Player.Root.position + Vector3.up * 0.3777153f;
            return Vector3.Distance(ringTrigger.ClosestPoint(foot), foot) <= 0.267f * Player.Root.localScale.x;
        }

        public void ShowStationGlow(int selected)
        {
            for (int i = 0; i < stationGlows.Length; i++)
                if (stationGlows[i]) stationGlows[i].SetActive(i == selected);
        }

        public Transform TrainerWorkoutTarget(Muscle muscle) => equipment[(int)muscle].Find("TrainerPos");

        public void ApplyProgress(PlayerProgress progress)
        {
            float size = BeachProgressionRules.RootScale(progress.prestigeLevel);
            playerPrestige = progress.prestigeLevel;
            Player.Root.localScale = Vector3.one * size;
            float localTargetHeight = BeachProgressionRules.CameraTargetY(progress.prestigeLevel) * .356f;
            growthCameraHeight = localTargetHeight * size;
            if (nativeFightCamera && nativeFightCamera.target)
                nativeFightCamera.target.localPosition = Vector3.up * localTargetHeight;
            if (muscleView) muscleView.Apply(progress, progress.prestigeLevel);
            if (titleLabels == null) return;
            foreach (var label in titleLabels)
            {
                if (label.name.IndexOf("Name", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var localized = label as OriginalSdfText;
                    if (localized) localized.translateText = false;
                    label.text = progress.DisplayName;
                }
                else if (label.name.IndexOf("Level", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    label.text = "lv" + progress.prestigeLevel;
                else if (label.name.IndexOf("Rank", System.StringComparison.OrdinalIgnoreCase) >= 0 || label.text == "Rookie")
                    label.text = BeachProgressionRules.RankTitle(progress.unlockedOpponent);
            }
        }
        public Vector3 StationPosition(Muscle muscle) => equipment[(int)muscle].parent.position;
        public Rect BeachBounds => Bounds(beachBounds);
        public Rect RingBounds => Bounds(ringBounds);
        private static Rect Bounds(Transform marker) => Rect.MinMaxRect(marker.position.x - marker.localScale.x / 2,
            marker.position.z - marker.localScale.z / 2, marker.position.x + marker.localScale.x / 2,
            marker.position.z + marker.localScale.z / 2);

        [SerializeField] private Vector3 phoneCameraOffset = new Vector3(0, 6.59668f, 11.392f);
        [SerializeField] private Vector3 beachCameraAngles = new Vector3(22.639f, -180, 0);
        [SerializeField] private Vector3 fightCameraOffset = new Vector3(-3.0972f, 1.7444f, 2.47064f);
        [SerializeField] private Vector3 fightCameraAngles = new Vector3(8.383f, -214.616f, 0);
        [SerializeField] private Vector3 phoneFightCameraOffset = new Vector3(-12.77f, 5.96f, 12.84f) * FightRules.ReferenceWorldScale;
        [SerializeField] private Vector3[] workoutCameraPositions = new Vector3[4];
        [SerializeField] private Vector3[] workoutCameraAngles = new Vector3[4];
        private Muscle currentWorkout;
        [SerializeField] private Transform[] workoutPoses = new Transform[4];
        [SerializeField] private Transform[] workoutExits = new Transform[4];
        [SerializeField] private GameObject[] machinePivots = new GameObject[4];

        public void BeginWorkout(TrainingStation station)
        {
            if (locomotion) locomotion.SetRoaming(false);
            currentWorkout = station.Muscle;
            var origin = workoutPoses[(int)station.Muscle];
            Player.Root.SetPositionAndRotation(origin.position, origin.rotation);
            if (nativeBeachCamera) nativeBeachCamera.BeginWorkout(workoutCameraPositions[(int)station.Muscle],
                workoutCameraPositionsLevelTen[(int)station.Muscle], workoutCameraAngles[(int)station.Muscle]);
            StopExerciseEffects();
            if (sweatEffect) sweatEffect.Play();
            SetMachinePivot(station.Muscle, false);
            Player.Workout(station.Muscle, 0);
            Player.RestartWorkout(station.Muscle);
        }

        public void ResetWorkout(Muscle muscle) => Player.RestartWorkout(muscle);

        public void EndWorkout(TrainingStation station)
        {
            StopExerciseEffects();
            SetMachinePivot(station.Muscle, true);
            var exit = workoutExits[(int)station.Muscle];
            Player.Root.SetPositionAndRotation(exit.position, workoutPoses[(int)station.Muscle].rotation);
            if (nativeBeachCamera) nativeBeachCamera.ExitWorkout();
            Player.Walk(0);
            if (locomotion) locomotion.SetRoaming(true);
        }

        private void SetMachinePivot(Muscle muscle, bool active)
        {
            var pivot = machinePivots[(int)muscle];
            if (pivot) pivot.SetActive(active);
        }

        public void EnterBeach(int prestigeLevel = 1)
        {
            cameraScenePrestige = prestigeLevel;
            ResetVictoryEffect();
            if (ambientEffect) ambientEffect.Play();
            ApplyAmbient(true);
            arena.gameObject.SetActive(false);
            beach.gameObject.SetActive(true);
            Enemy.Root.gameObject.SetActive(false);
            Player.Root.SetPositionAndRotation(beachSpawn.position, beachSpawn.rotation);
            if (!locomotion) GroundPlayer();
            if (locomotion) locomotion.SetRoaming(true);
            Player.Walk(0, !locomotion || locomotion.Grounded);
            if (nativeBeachCamera) nativeBeachCamera.Initialize(BeachCameraLocalPosition, beachCameraAngles);
            snap = true;
        }

        public void EnterRing(string opponent)
        {
            if (locomotion) locomotion.SetRoaming(false, dynamicBody: true);
            fightCameraTracking = false;
            ResetVictoryEffect();
            StopExerciseEffects();
            if (ambientEffect) ambientEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ApplyAmbient(false);
            beach.gameObject.SetActive(false);
            arena.gameObject.SetActive(true);
            Enemy.Root.gameObject.SetActive(true);
            var enemyBody = Enemy.Root.GetComponent<Rigidbody>();
            if (enemyBody) { enemyBody.isKinematic = false; enemyBody.useGravity = true; enemyBody.velocity = Vector3.zero; }
            Enemy.UseModel(opponent);
            Player.PrepareForFight();
            Enemy.PrepareForFight();
            Player.Root.position = playerSpawn.position;
            Enemy.Root.position = enemySpawn.position;
            if (nativeFightCamera) nativeFightCamera.Initialize(phoneMode, playerPrestige);
            snap = true;
        }

        public void FrameBeach(bool training)
        {
            if (nativeBeachCamera)
            {
                nativeBeachCamera.Step(Time.deltaTime, playerPrestige);
                return;
            }
            var offset = training ? workoutCameraPositions[(int)currentWorkout] :
                phoneMode ? phoneCameraOffset : beachCameraOffset;
            if (!training) offset.z += (phoneMode ? cameraScenePrestige / 2 : cameraScenePrestige) * .356f;
            var angles = training ? workoutCameraAngles[(int)currentWorkout] : beachCameraAngles;
            MoveCameraPose(Player.Root.position + offset + Vector3.up * growthCameraHeight, Quaternion.Euler(angles));
        }

        private bool fightCameraTracking;
        private Vector3 fightFocus, fightLocalOffset;

        public void FrameFight(float countdown, bool canMove = true)
        {
            if (nativeFightCamera)
            {
                nativeFightCamera.Step(Time.deltaTime, countdown <= 0, canMove);
                if (countdown <= 0) return;
            }
            if (countdown > 0)
            {
                fightCameraTracking = false;
                MoveCameraPose(arena.position + new Vector3(1.5308f, 8.74336f, -11.87972f), Quaternion.Euler(45, 0, 0));
                return;
            }
            Vector3 reference = new Vector3(-4.74f, 0, -9.35f) * FightRules.ReferenceWorldScale;
            Vector3 separation = Enemy.Root.position - Player.Root.position;
            separation.y = 0;
            float heading = Vector3.SignedAngle(reference, separation, Vector3.up);
            Quaternion orbit = Quaternion.Euler(0, heading, 0);
            float distance = Mathf.Max(1, separation.magnitude / reference.magnitude);
            Vector3 center = (Player.Root.position + Enemy.Root.position) * 0.5f;
            var offset = phoneMode ? phoneFightCameraOffset : fightCameraOffset;
            Quaternion rotation = orbit * Quaternion.Euler(fightCameraAngles);
            Vector3 position = center + orbit * ((offset - reference * 0.5f) * distance);
            if (!fightCameraTracking)
            {
                fightFocus = center;
                fightLocalOffset = Quaternion.Inverse(CameraPose.rotation) * (CameraPose.position - center);
                fightCameraTracking = true;
            }
            float blend = snap ? 1 : 1 - Mathf.Exp(-9 * Time.deltaTime);
            fightFocus = Vector3.Lerp(fightFocus, center, blend);
            fightLocalOffset = Vector3.Lerp(fightLocalOffset, Quaternion.Inverse(rotation) * (position - center), blend);
            rotation = Quaternion.Slerp(CameraPose.rotation, rotation, blend);
            // Interpolate the rig's local offset so an orbit turn follows an arc, not a chord through the fighters.
            CameraPose.SetPositionAndRotation(fightFocus + rotation * fightLocalOffset, rotation);
            snap = false;
        }

        private void MoveCameraPose(Vector3 position, Quaternion rotation)
        {
            float blend = snap ? 1 : Mathf.Clamp01(14 * Time.deltaTime);
            CameraPose.position = Vector3.Lerp(CameraPose.position, position, blend);
            CameraPose.rotation = Quaternion.Slerp(CameraPose.rotation, rotation, blend);
            snap = false;
        }

        private static void MakeRing(Transform root)
        {
            OriginalArt.Place(root, "Ring", "Original ring", new Vector3(0, -1.5f, 0));
        }

        private static void MakeSand(Transform root)
        {
            var material = OriginalArt.Get<Material>("Look/Beach Floor");
            AddSurface(root, "Sand", new Vector3(0, -0.09f, 1), new Vector3(38, 0.15f, 32), material);
        }

        private static void PlaceBackdrop(Transform root)
        {
            OriginalArt.Place(root, "Decor", "Original decor", new Vector3(-9, -0.38f, 6));
            OriginalArt.Place(root, "Sea", "Original sea", new Vector3(0, -0.6f, 24));
        }

        public void Feedback(FightController fight, bool playerHit, bool enemyHit, bool nativeAudio = false)
        {
            if (playerHit)
            {
                if (!nativeAudio) Play(fight.Player.LastImpactWasBlocked ? "Block" : "Punch", 0.5f);
                Player.ShowImpact(fight.Player.LastImpactWasBlocked);
            }
            if (enemyHit)
            {
                if (!nativeAudio) Play(fight.Enemy.LastImpactWasBlocked ? "Block" : "HeavyPunch", 0.5f);
                Enemy.ShowImpact(fight.Enemy.LastImpactWasBlocked);
            }
        }

        public const float CountdownSoundVolume = 0.55f;
        public const float VictorySoundVolume = 0.85f;
        public const float DefeatSoundVolume = 0.85f;

        public void OpeningPresentationSound() => Play("Countdown", CountdownSoundVolume);
        public void ResultPresentationSound(bool victory) => Play(victory ? "Victory" : "Defeat", victory ? VictorySoundVolume : DefeatSoundVolume);

        public void VictoryPresentationParticles()
        {
            if (!victoryEffect) return;
            victoryEffect.SetActive(true);
            foreach (var particle in victoryParticles) particle.Play();
        }

        public void Play(string sound, float volume)
        {
            int index = System.Array.IndexOf(Sounds, sound);
            var clip = index >= 0 && soundClips != null && index < soundClips.Length ? soundClips[index] : null;
            if (clip && CoreRoot.Current.Audio.SfxEnabled)
                effects.PlayOneShot(clip, volume * CoreRoot.Current.Audio.SfxVolume);
        }

        internal static Transform Group(Transform parent, string name, Vector3 local)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = local;
            return group;
        }

        private static void AddSurface(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var part = Group(parent, name, position);
            part.localScale = size;
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }


    }

    [System.Serializable]
    internal sealed class BoxerView
    {
        private static readonly int Idle = Animator.StringToHash("Boxing_Idle");
        private static readonly int WalkForward = Animator.StringToHash("Boxing_WalkForward");
        private static readonly int WalkBackward = Animator.StringToHash("Boxing_WalkBackward");
        private static readonly int Victory = Animator.StringToHash("PhotoShoot");
        private static readonly int Block = Animator.StringToHash("Boxing_Blocking");
        private static readonly int Jab = Animator.StringToHash("Boxing_Jab");
        private static readonly int Straight = Animator.StringToHash("Boxing_Straight");
        private static readonly int Knockout = Animator.StringToHash("Boxing_Die");
        private static readonly int Hit = Animator.StringToHash("Boxing_TakeDamage");
        private static readonly int GuardHit = Animator.StringToHash("Boxing_BlockDamage");
        private static readonly int BeachIdle = Animator.StringToHash("Player_Idle");
        private static readonly int BeachWalk = Animator.StringToHash("Player_Walk");
        private static readonly int[] Exercises =
        {
            Animator.StringToHash("Player_ChestWorkout"), Animator.StringToHash("Player_ArmsWorkout"),
            Animator.StringToHash("Player_BackWorkout"), Animator.StringToHash("Player_Squat")
        };

        [SerializeField] private Transform model;
        [SerializeField] private Transform trainingModel;
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem hitEffect, guardEffect;
        private string modelName;
        private int state;
        private float previousPunch = float.PositiveInfinity;
        private Vector3 previousPosition;
        private bool trainingController;
        private int previousImpacts;
        private float takingDamageUntil;
        private bool deathTriggered;
        private Vector2 animationAxes;
        private bool hasAnimationAxes;
        private System.Collections.Generic.Dictionary<Animator, Pose> restPoses;
        [SerializeField] public Transform Root;

        private struct Pose
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        [SerializeField] private Transform[] models;
        [SerializeField] private RuntimeAnimatorController workoutController, boxingController;

        public void CacheRestPoses()
        {
            if (restPoses == null) restPoses = new System.Collections.Generic.Dictionary<Animator, Pose>();
            CacheModelRestPose(model);
            if (models != null)
            {
                foreach (var candidate in models) CacheModelRestPose(candidate);
            }
        }

        private void CacheModelRestPose(Transform candidate)
        {
            if (!candidate) return;
            var anim = candidate.GetComponentInChildren<Animator>(true);
            if (!anim || restPoses.ContainsKey(anim)) return;
            restPoses[anim] = new Pose
            {
                localPosition = anim.transform.localPosition,
                localRotation = anim.transform.localRotation
            };
        }

        private void RestoreRestPose(Transform candidate)
        {
            if (!candidate) return;
            var anim = candidate.GetComponentInChildren<Animator>(true);
            if (!anim) return;
            if (restPoses != null && restPoses.TryGetValue(anim, out var pose))
            {
                anim.transform.localPosition = pose.localPosition;
                anim.transform.localRotation = pose.localRotation;
            }
        }

        public void PrepareForFight()
        {
            CacheRestPoses();
            if (trainingModel)
            {
                trainingModel.gameObject.SetActive(false);
                RestoreRestPose(model);
                if (model) model.gameObject.SetActive(true);
                animator = model ? model.GetComponentInChildren<Animator>(true) : animator;
            }
            else
            {
                RestoreRestPose(model);
            }
            if (animator)
            {
                if (boxingController) animator.runtimeAnimatorController = boxingController;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (animator.isActiveAndEnabled)
                {
                    animator.Rebind();
                    animator.Update(0f);
                }
            }
            trainingController = false;
            state = 0;
            previousPunch = float.PositiveInfinity;
            previousImpacts = 0;
            takingDamageUntil = 0;
            deathTriggered = false;
            hasAnimationAxes = false;
        }

        #if UNITY_EDITOR
        public BoxerView(Transform parent, string name, string prefab)
        {
            Root = BeachStage.Group(parent, name, Vector3.zero);
            string[] names = prefab == "Player" ? new[] { "Player" } : new[] { "Ramez", "Kyle" };
            models = new Transform[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                models[i] = OriginalArt.Place(Root, names[i], "Original " + names[i], Vector3.zero);
                models[i].localScale *= 0.38f;
                foreach (var renderer in models[i].GetComponentsInChildren<Renderer>(true))
                    if (renderer.transform.name.IndexOf("muscle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        renderer.enabled = false;
            }
            workoutController = OriginalArt.Get<RuntimeAnimatorController>("Animations/Armature");
            boxingController = OriginalArt.Get<RuntimeAnimatorController>("Animations/BoxingArmature");
            UseModel(prefab);
        }
        #endif

        public void UseModel(string prefab)
        {
            previousImpacts = 0;
            takingDamageUntil = 0;
            deathTriggered = false;
            state = 0;
            previousPunch = float.PositiveInfinity;
            CacheRestPoses();
            foreach (var candidate in models)
            {
                bool active = candidate.name == "Original " + prefab;
                if (active) RestoreRestPose(candidate);
                candidate.gameObject.SetActive(active);
            }
            model = System.Array.Find(models, candidate => candidate.gameObject.activeSelf);
            if (!model) throw new System.InvalidOperationException("Missing scene boxer: " + prefab);
            modelName = prefab;
            animator = model.GetComponentInChildren<Animator>(true);
            hitEffect = guardEffect = null;
            if (!animator) return;
            animator.runtimeAnimatorController = boxingController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            trainingController = false;
            foreach (var effect in model.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (effect.name == "HitParticle") hitEffect = effect;
                if (effect.name == "BlockHitParticle") guardEffect = effect;
            }
        }

        public void ShowImpact(bool blocked)
        {
            var effect = blocked ? guardEffect : hitEffect;
            if (!effect) effect = hitEffect ? hitEffect : guardEffect;
            if (!effect) return;
            effect.gameObject.SetActive(true);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play();
        }

        public void Walk(float speed, bool grounded = true)
        {
            SetController(true);
            animator.SetBool("Grounded", grounded);
            animator.SetBool("Walking", speed > 0.000001f);
            animator.SetBool("WorkingOut", false);
            foreach (string name in new[] { "ChestIdle", "ArmsIdle", "BackIdle", "LegsIdle", "ChestWorkout", "ArmsWorkout", "BackWorkout", "LegsWorkout", "Squatting" }) animator.SetBool(name, false);
        }

        public void Workout(Muscle muscle, float speed)
        {
            SetController(true);
            animator.SetBool("Grounded", true);
            animator.SetBool("Walking", false);
            animator.SetBool("WorkingOut", true);
            string[] names = { "Chest", "Arms", "Back", "Legs" };
            for (int i = 0; i < names.Length; i++)
            {
                animator.SetBool(names[i] + "Idle", i == (int)muscle);
                animator.SetBool(names[i] + "Workout", i == (int)muscle);
            }
            animator.SetFloat("WorkoutSpeed", speed);
        }

        public void RestartWorkout(Muscle muscle)
        {
            SetController(true);
            string part = new[] { "Chest", "Arms", "Back", "Legs" }[(int)muscle];
            animator.SetFloat("WorkoutSpeed", 0);
            animator.SetBool(part + "Workout", false);
            animator.SetTrigger("Restart" + part + "Workout");
        }

        public void Fight(FighterState fighter, float flash)
        {
            SetController(false);
            Vector3 motion = Root.position - previousPosition;
            previousPosition = Root.position;
            var axes = hasAnimationAxes ? animationAxes : new Vector2(Vector3.Dot(motion, Root.right), Vector3.Dot(motion, Root.forward));
            bool alive = fighter.Health > 0;
            animator.SetBool("Blocking", alive && fighter.Blocking);
            animator.SetBool("WalkBackward", alive && (axes.x > 0 || axes.y < 0));
            animator.SetBool("WalkForward", alive && (axes.x < 0 || axes.y > 0));
            if (fighter.PunchTime < previousPunch && alive)
                animator.SetTrigger(fighter.LeftPunch ? "Punch1" : "Punch2");
            if (fighter.ImpactCount != previousImpacts)
            {
                animator.SetTrigger(fighter.LastImpactWasBlocked ? "BlockDamage" : "TakeDamage");
                if (trainingModel) takingDamageUntil = Time.time + .3f;
            }
            if (trainingModel) animator.SetBool("TakingDamage", Time.time < takingDamageUntil);
            animator.SetBool("Dead", !alive);
            if (!alive && !deathTriggered) { animator.SetTrigger("Die"); deathTriggered = true; }
            previousImpacts = fighter.ImpactCount;
            previousPunch = fighter.PunchTime;
        }

        public void FightIntent(FighterState fighter, float flash, Vector2 sourceAxes)
        {
            animationAxes = sourceAxes;
            hasAnimationAxes = true;
            Fight(fighter, flash);
        }

        public void Result(bool won)
        {
            var body = Root.GetComponent<Rigidbody>();
            if (body && !won && !body.isKinematic) body.isKinematic = true;
            SetController(false);
            animator.SetBool("Blocking", false);
            animator.SetBool("WalkForward", false);
            animator.SetBool("WalkBackward", false);
            if (!won && !deathTriggered) { animator.SetTrigger("Die"); deathTriggered = true; }
            animator.SetBool("Dead", !won);
            if (trainingModel) animator.SetBool("TakingDamage", Time.time < takingDamageUntil);
        }

        private void SetController(bool training)
        {
            if (!animator || trainingController == training) return;
            var controller = training ? workoutController : boxingController;
            if (trainingModel)
            {
                trainingModel.gameObject.SetActive(training);
                if (!training) RestoreRestPose(model);
                model.gameObject.SetActive(!training);
                animator = (training ? trainingModel : model).GetComponentInChildren<Animator>(true);
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            if (controller) animator.runtimeAnimatorController = controller;
            trainingController = training;
            state = 0;
            previousPunch = float.PositiveInfinity;
            previousImpacts = 0;
            takingDamageUntil = 0;
            deathTriggered = false;
            hasAnimationAxes = false;
        }

        private void Play(int next, float speed = 1, bool restart = false)
        {
            if (!animator) return;
            animator.speed = speed;
            if (next == state && !restart) return;
            if (!animator.HasState(0, next)) return;
            animator.CrossFadeInFixedTime(next, 0.045f, 0, 0);
            state = next;
        }
    }
}
