using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace BeachBoxing.Editor
{
    public static partial class BeachNativeCombatFeedbackRestore
    {
        private const string AudioArt = "Assets/BeachBoxing/OriginalArt/NativeCombatAudio/";
        private const string OriginalClips = "Assets/BeachBoxing/OriginalArt/AnimationClip/";
        private const string OpponentArt = "Assets/BeachBoxing/OriginalOpponents/";
        private const string LandingAsset = "Assets/BeachBoxing/OriginalArt/World/NativeLandingEffect.prefab";
        private static readonly string[] StepChannels = { "FootstepSFX", "Footstep2SFX", "JumpSFX", "LandSFX" };
        private static readonly string[] HitChannels = { "Audio Source", "Audio Source (1)", "Audio Source (2)" };

        [Serializable] private sealed class AudioData
        {
            public int sourceId, clipId, priority, rolloffMode;
            public float volume, pitch, spatialBlend, stereoPan, dopplerLevel, minDistance, maxDistance;
            public bool loop, mute, spatialize, spatializePostEffects, bypassEffects, bypassListenerEffects, bypassReverbZones;
            public Track rolloff, spatial, spread, reverb;
        }

        [Serializable] private sealed class ClipData
        {
            public string name, trailPath;
            public EventData[] events;
            public Track trail;
        }

        [Serializable] private sealed class EventData
        {
            public string functionName, data;
            public float time, floatParameter;
            public int intParameter, messageOptions;
        }

        [Serializable] private sealed class PcmProvenance { public PcmRecord[] records; }
        [Serializable] private sealed class PcmRecord
        {
            public int sourceId, channels, sampleRate, nativeFrames;
            public string path;
        }

        [Serializable] private sealed class OpponentAssets { public string[] prefabs; }

        private sealed class AudioPreparation
        {
            public AudioMixerGroup sfx, musicGroup;
            public AudioSource effects, music;
            public Transform victoryEffect, feet;
            public Animator playerFight, playerBeach;
            public ParticleSystem walkingDust, landing;
            public GameObject landingPrefab;
            public SerializedObject flowData, motor;
            public SerializedProperty stageSounds;
            public Dictionary<int, AudioClip> pcm;
            public Dictionary<int, AudioData> options;
            public string[] prefabPaths;
            public Transform[] opponents;
            public (ClipData record, AnimationClip clip)[] animations;
        }

        private static AudioPreparation PrepareAudio(BeachGameFlow flow, NativeOpponentCatalog catalog, Data data)
        {
            var settings = Need(flow.GetComponent<BeachNativeSettingsView>(), "Native Settings");
            var mixer = Need(Property(new SerializedObject(settings), "mixer").objectReferenceValue as AudioMixer,
                "Native Settings mixer");
            var flowData = new SerializedObject(flow);
            var stage = Property(flowData, "stage");
            var playerData = stage.FindPropertyRelative("Player");
            var playerRoot = ChildReference<Transform>(playerData, "Root");
            var motor = new SerializedObject(Need(playerRoot.GetComponent<BeachNativeLocomotion>(), "Native player locomotion"));
            foreach (var name in new[] { "jumpAudio", "landAudio", "walkingDust", "landingParticle" })
                Property(motor, name);
            var walk = Need(playerRoot.Find("Original WalkEffect"), "Player walk dust");
            var feet = Need(playerRoot.Find("Feet"), "Player Feet");
            var landing = feet.Find("LandParticle");
            var landingPrefab = Asset<GameObject>(LandingAsset);
            Need(landingPrefab.GetComponent<ParticleSystem>(), LandingAsset + " ParticleSystem");
            var prep = new AudioPreparation
            {
                sfx = Need(mixer.FindMatchingGroups("SFX").SingleOrDefault(group => group.name == "SFX"), "SFX mixer group"),
                musicGroup = Need(mixer.FindMatchingGroups("Music").SingleOrDefault(group => group.name == "Music"), "Music mixer group"),
                pcm = LoadPcm(),
                options = data.playerAudio.ToDictionary(source => source.sourceId),
                flowData = flowData,
                motor = motor,
                stageSounds = stage.FindPropertyRelative("soundClips"),
                effects = ChildReference<AudioSource>(stage, "effects"),
                music = ChildReference<AudioSource>(stage, "music"),
                victoryEffect = ChildReference<GameObject>(stage, "victoryEffect").transform,
                playerFight = ActorAnimator(ChildReference<Transform>(playerData, "model")),
                playerBeach = ActorAnimator(ChildReference<Transform>(playerData, "trainingModel")),
                feet = feet,
                walkingDust = Need(walk.GetComponent<ParticleSystem>(), walk.name),
                landing = landing ? Need(landing.GetComponent<ParticleSystem>(), "Source LandParticle3068") : null,
                landingPrefab = landingPrefab,
                opponents = catalog.Models,
                prefabPaths = JsonUtility.FromJson<OpponentAssets>(
                    Asset<TextAsset>(OpponentArt + "NativeOpponentAssets.json").text).prefabs
            };
            if (prep.stageSounds == null || !prep.stageSounds.isArray)
                throw new InvalidOperationException("Missing Stage soundClips array.");
            if (prep.prefabPaths.Length != prep.opponents.Length)
                throw new InvalidOperationException("Opponent prefab count does not match the preplaced roster.");
            for (int i = 0; i < prep.prefabPaths.Length; i++)
            {
                ValidateOpponentChannels(prep.opponents[i]);
                ValidateOpponentChannels(Asset<GameObject>(prep.prefabPaths[i]).transform);
            }
            var enemyArmature = ActorAnimator(prep.opponents[0]).transform;
            prep.animations = data.boxingClips.Select(record =>
                (record, PrepareAnimation(record, prep.playerFight.transform, enemyArmature))).ToArray();
            return prep;
        }

        private static void RestoreAudio(BeachGameFlow flow, BeachNativeCombatFeedback feedback,
            AudioPreparation prep, List<string> writtenAssets)
        {
            var sfx = prep.sfx;
            var pcm = prep.pcm;
            var options = prep.options;
            var sounds = prep.stageSounds;
            if (sounds.arraySize < 7) sounds.arraySize = 7;
            sounds.GetArrayElementAtIndex(1).objectReferenceValue = pcm[114];
            sounds.GetArrayElementAtIndex(5).objectReferenceValue = pcm[113];
            sounds.GetArrayElementAtIndex(6).objectReferenceValue = pcm[111];
            prep.flowData.ApplyModifiedProperties();

            Route(prep.effects, sfx);
            Route(prep.music, prep.musicGroup);
            var roles = new Dictionary<string, int>
            {
                ["Footstep"] = 364, ["Jump"] = 366, ["Landing"] = 362,
                ["Game Kick 1"] = 110, ["Game Punch (1)"] = 112
            };
            foreach (var source in flow.GetComponentsInChildren<AudioSource>(true))
            {
                if (source == prep.music || source.outputAudioMixerGroup == prep.musicGroup) continue;
                Route(source, sfx);
                if (!source.clip) continue;
                var clipPath = AssetDatabase.GetAssetPath(source.clip);
                if (!clipPath.StartsWith(OpponentArt + "AudioClip/", StringComparison.Ordinal)) continue;
                var name = System.IO.Path.GetFileNameWithoutExtension(clipPath);
                if (roles.TryGetValue(name, out int clipId)) BindClip(source, pcm[clipId], sfx);
            }

            var stageAudio = Child(flow.transform, "Native combat audio");
            feedback.playerGuardAudio = AuthoredSource(Child(stageAudio, "TakeBlockDamage [1917]").gameObject,
                options[1917], pcm[109], sfx);
            feedback.damageAudio = AuthoredSource(feedback.takeDamage, options[1911], pcm[359], sfx);
            feedback.healingAudio = AuthoredSource(feedback.healing, options[1904], pcm[106], sfx);
            var twinkle = AuthoredSource(Child(prep.victoryEffect, "Twinkling Stars [1908]").gameObject,
                options[1908], pcm[107], sfx);
            twinkle.playOnAwake = true;

            var fight = PlayerReceiver(prep.playerFight, sfx, pcm, options, 1903, 1902, 1909, 1916);
            var beach = PlayerReceiver(prep.playerBeach, sfx, pcm, options, 2984, 2981, 2988, 2995);
            fight.swings = new[] { 1901, 1906, 1912 }
                .Select(id => AuthoredSource(Child(stageAudio, "Spear Swing 1 [" + id + "]").gameObject,
                    options[id], pcm[108], sfx)).ToArray();
            EditorUtility.SetDirty(fight);
            RecordPrefabOverride(fight);

            BindBeachMovement(prep, beach);
            foreach (var path in prep.prefabPaths)
                RestorePrefabAudio(path, sfx, pcm, writtenAssets);
            feedback.opponentAudio = prep.opponents.Select(model => OpponentReceiver(model, sfx, pcm)).ToArray();
            RestoreAnimationEvents(prep.animations, writtenAssets);
        }

        private static Dictionary<int, AudioClip> LoadPcm()
        {
            var package = JsonUtility.FromJson<PcmProvenance>(Asset<TextAsset>(AudioArt + "Provenance.json").text);
            var clips = new Dictionary<int, AudioClip>(package.records.Length);
            foreach (var record in package.records)
            {
                var clip = Asset<AudioClip>(record.path);
                var importer = Need(AssetImporter.GetAtPath(record.path) as AudioImporter, record.path + " importer");
                var config = importer.defaultSampleSettings;
                if (config.compressionFormat != AudioCompressionFormat.PCM ||
                    config.loadType != AudioClipLoadType.DecompressOnLoad ||
                    config.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate || importer.forceToMono ||
                    clip.channels != record.channels || clip.frequency != record.sampleRate || clip.samples != record.nativeFrames)
                    throw new InvalidOperationException("Native PCM import disagrees with the source for " + record.path);
                if (!clip.GetData(new float[clip.channels * 512], 0))
                    throw new InvalidOperationException("Native PCM sample data is inaccessible for " + record.path);
                clips.Add(record.sourceId, clip);
            }
            return clips;
        }

        private static BeachNativeBoxingAudio PlayerReceiver(Animator animator, AudioMixerGroup sfx,
            Dictionary<int, AudioClip> clips, Dictionary<int, AudioData> options,
            int step1, int step2, int jump, int land)
        {
            var receiver = AttachReceiver(animator);
            receiver.step1 = PlayerChannel(animator.transform, "FootstepSFX", step1, sfx, clips, options);
            receiver.step2 = PlayerChannel(animator.transform, "Footstep2SFX", step2, sfx, clips, options);
            receiver.jump = PlayerChannel(animator.transform, "JumpSFX", jump, sfx, clips, options);
            receiver.land = PlayerChannel(animator.transform, "LandSFX", land, sfx, clips, options);
            EditorUtility.SetDirty(receiver);
            RecordPrefabOverride(receiver);
            return receiver;
        }

        private static AudioSource PlayerChannel(Transform armature, string name, int sourceId, AudioMixerGroup sfx,
            Dictionary<int, AudioClip> clips, Dictionary<int, AudioData> options)
        {
            var clipData = options[sourceId];
            var node = Child(armature, name);
            var existing = node.GetComponent<AudioSource>();
            if (!existing) return AuthoredSource(node.gameObject, clipData, clips[clipData.clipId], sfx);
            BindClip(existing, clips[clipData.clipId], sfx);
            return existing;
        }

        private static void RestorePrefabAudio(string path, AudioMixerGroup group, Dictionary<int, AudioClip> clips,
            List<string> writtenAssets)
        {
            var prefab = Asset<GameObject>(path);
            var animator = ActorAnimator(prefab.transform);
            var existing = animator.GetComponent<BeachNativeBoxingAudio>();
            if (animator.fireEvents && existing && existing.guardHits.Length == 3 && existing.healthHits.Length == 3 &&
                existing.swings.Length == 0 &&
                Matches(existing.step1, clips[364], group) && Matches(existing.step2, clips[364], group) &&
                Matches(existing.jump, clips[366], group) && Matches(existing.land, clips[362], group) &&
                existing.guardHits.All(source => Matches(source, clips[110], group)) &&
                existing.healthHits.All(source => Matches(source, clips[112], group))) return;

            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(path);
                OpponentReceiver(contents.transform, group, clips, editingPrefabAsset: true);
                PrefabUtility.SaveAsPrefabAsset(contents, path, out bool success);
                if (!success) throw new InvalidOperationException("SaveAsPrefabAsset reported failure.");
                writtenAssets.Add(path);
            }
            catch (Exception error) { throw new InvalidOperationException("Could not restore native audio prefab " + path, error); }
            finally { if (contents) PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static bool Matches(AudioSource source, AudioClip clip, AudioMixerGroup group)
            => source && source.clip == clip && source.outputAudioMixerGroup == group;

        private static BeachNativeBoxingAudio OpponentReceiver(Transform model, AudioMixerGroup sfx,
            Dictionary<int, AudioClip> clips, bool editingPrefabAsset = false)
        {
            var animator = ActorAnimator(model);
            var receiver = AttachReceiver(animator, editingPrefabAsset);
            receiver.step1 = Source(animator.transform, StepChannels[0], clips[364], sfx);
            receiver.step2 = Source(animator.transform, StepChannels[1], clips[364], sfx);
            receiver.jump = Source(animator.transform, StepChannels[2], clips[366], sfx);
            receiver.land = Source(animator.transform, StepChannels[3], clips[362], sfx);
            receiver.healthHits = EnemyChannels(model, "PunchHitSFX", clips[112], sfx);
            receiver.guardHits = EnemyChannels(model, "BlockHitSFX", clips[110], sfx);
            receiver.swings = Array.Empty<AudioSource>();
            EditorUtility.SetDirty(receiver);
            RecordPrefabOverride(receiver);
            return receiver;
        }

        private static Animator ActorAnimator(Transform model)
            => Need(model.GetComponentInChildren<Animator>(true), model.name + " Animator");

        private static void ValidateOpponentChannels(Transform model)
        {
            var armature = ActorAnimator(model).transform;
            foreach (var channel in StepChannels) AudioChannel(armature, channel);
            foreach (var group in new[] { "PunchHitSFX", "BlockHitSFX" })
            {
                var node = Need(model.Find(group), model.name + "/" + group);
                foreach (var channel in HitChannels) AudioChannel(node, channel);
            }
        }

        private static AudioSource[] EnemyChannels(Transform model, string name, AudioClip clip, AudioMixerGroup sfx)
        {
            var node = Need(model.Find(name), model.name + "/" + name);
            return HitChannels.Select(channel => Source(node, channel, clip, sfx)).ToArray();
        }

        private static AudioSource Source(Transform parent, string name, AudioClip clip, AudioMixerGroup group)
        {
            var source = AudioChannel(parent, name);
            BindClip(source, clip, group);
            return source;
        }

        private static AudioSource AudioChannel(Transform parent, string name)
        {
            var node = Need(parent.Find(name), parent.name + "/" + name);
            return Need(node.GetComponent<AudioSource>(), parent.name + "/" + name + " AudioSource");
        }

        private static BeachNativeBoxingAudio AttachReceiver(Animator animator, bool editingPrefabAsset = false)
        {
            animator.fireEvents = true;
            EditorUtility.SetDirty(animator);
            RecordPrefabOverride(animator);
            if (!editingPrefabAsset) return Component<BeachNativeBoxingAudio>(animator.gameObject);
            var receiver = animator.GetComponent<BeachNativeBoxingAudio>();
            return receiver ? receiver : animator.gameObject.AddComponent<BeachNativeBoxingAudio>();
        }

        private static AudioSource AuthoredSource(GameObject owner, AudioData original, AudioClip clip, AudioMixerGroup group)
        {
            var source = Component<AudioSource>(owner);
            source.playOnAwake = false;
            source.volume = original.volume;
            source.pitch = original.pitch;
            source.priority = original.priority;
            source.spatialBlend = original.spatialBlend;
            source.panStereo = original.stereoPan;
            source.loop = original.loop;
            source.dopplerLevel = original.dopplerLevel;
            source.minDistance = original.minDistance;
            source.maxDistance = original.maxDistance;
            source.mute = original.mute;
            source.spatialize = original.spatialize;
            source.spatializePostEffects = original.spatializePostEffects;
            source.bypassEffects = original.bypassEffects;
            source.bypassListenerEffects = original.bypassListenerEffects;
            source.bypassReverbZones = original.bypassReverbZones;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, Curve(original.rolloff));
            source.SetCustomCurve(AudioSourceCurveType.SpatialBlend, Curve(original.spatial));
            source.SetCustomCurve(AudioSourceCurveType.Spread, Curve(original.spread));
            source.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, Curve(original.reverb));
            source.rolloffMode = (AudioRolloffMode)original.rolloffMode;
            BindClip(source, clip, group);
            EditorUtility.SetDirty(source);
            RecordPrefabOverride(source);
            return source;
        }

        private static void BindClip(AudioSource source, AudioClip clip, AudioMixerGroup group)
        {
            if (source.clip != clip)
            {
                source.clip = clip;
                EditorUtility.SetDirty(source);
                RecordPrefabOverride(source);
            }
            Route(source, group);
        }

        private static void Route(AudioSource source, AudioMixerGroup group)
        {
            if (source.outputAudioMixerGroup == group) return;
            source.outputAudioMixerGroup = group;
            EditorUtility.SetDirty(source);
            RecordPrefabOverride(source);
        }

        private static void RecordPrefabOverride(Component component)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        private static AnimationClip PrepareAnimation(ClipData record, Transform playerAnimator, Transform enemyAnimator)
        {
            var clip = Asset<AnimationClip>(OriginalClips + record.name + ".anim");
            if (record.events == null || record.events.Length == 0)
                throw new InvalidOperationException("Missing source animation events for " + record.name);
            if (!string.IsNullOrEmpty(record.trailPath))
            {
                var targets = new[] { playerAnimator.Find(record.trailPath), enemyAnimator.Find(record.trailPath) };
                if (!targets.Any(target => target && target.GetComponent<TrailRenderer>()))
                    throw new InvalidOperationException("Native glove trail path is missing for " + record.name);
                if (record.trail == null || record.trail.keys == null || record.trail.keys.Length == 0)
                    throw new InvalidOperationException("Missing source glove trail keys for " + record.name);
            }
            return clip;
        }

        private static void RestoreAnimationEvents((ClipData record, AnimationClip clip)[] animations,
            List<string> writtenAssets)
        {
            foreach (var (record, clip) in animations)
            {
                var oldEvents = AnimationUtility.GetAnimationEvents(clip);
                var events = oldEvents
                    .Where(existing => !record.events.Any(original => original.functionName == existing.functionName))
                    .Concat(record.events.Select(original => new AnimationEvent
                    {
                        time = original.time, functionName = original.functionName, stringParameter = original.data,
                        floatParameter = original.floatParameter, intParameter = original.intParameter,
                        messageOptions = (SendMessageOptions)original.messageOptions
                    })).OrderBy(animationEvent => animationEvent.time).ToArray();
                bool addEvents = !oldEvents.Select(EventIdentity).SequenceEqual(events.Select(EventIdentity));
                bool hasTrail = !string.IsNullOrEmpty(record.trailPath);
                var binding = hasTrail
                    ? EditorCurveBinding.FloatCurve(record.trailPath, typeof(TrailRenderer), "m_Time")
                    : default;
                bool addTrail = hasTrail && AnimationUtility.GetEditorCurve(clip, binding) == null;
                if (!addEvents && !addTrail) continue;
                string path = AssetDatabase.GetAssetPath(clip);
                try
                {
                    if (addEvents) AnimationUtility.SetAnimationEvents(clip, events);
                    if (addTrail) AnimationUtility.SetEditorCurve(clip, binding, Curve(record.trail));
                    EditorUtility.SetDirty(clip);
                    AssetDatabase.SaveAssetIfDirty(clip);
                    writtenAssets.Add(path);
                }
                catch (Exception error)
                {
                    throw new InvalidOperationException("Could not restore native animation asset " + path, error);
                }
            }
        }

        private static (float, string, string, float, int, UnityEngine.Object, SendMessageOptions) EventIdentity(AnimationEvent evt)
            => (evt.time, evt.functionName, evt.stringParameter, evt.floatParameter,
                evt.intParameter, evt.objectReferenceParameter, evt.messageOptions);

        private static void BindBeachMovement(AudioPreparation prep, BeachNativeBoxingAudio receiver)
        {
            var values = prep.motor;
            values.FindProperty("jumpAudio").objectReferenceValue = receiver.jump;
            values.FindProperty("landAudio").objectReferenceValue = receiver.land;
            values.FindProperty("walkingDust").objectReferenceValue = prep.walkingDust;

            var landing = prep.landing;
            if (!landing)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prep.landingPrefab, prep.feet);
                Undo.RegisterCreatedObjectUndo(instance, Operation);
                instance.name = "LandParticle";
                instance.transform.localScale = prep.landingPrefab.transform.localScale * FightRules.ReferenceWorldScale;
                RecordPrefabOverride(instance.transform);
                landing = instance.GetComponent<ParticleSystem>();
            }

            values.FindProperty("landingParticle").objectReferenceValue = landing;
            values.ApplyModifiedProperties();
        }

        private static T ChildReference<T>(SerializedProperty owner, string field) where T : UnityEngine.Object
            => Need(owner?.FindPropertyRelative(field)?.objectReferenceValue as T, "Stage " + field);

        private static SerializedProperty Property(SerializedObject owner, string path)
            => owner.FindProperty(path) ?? throw new InvalidOperationException("Missing combat serialized property " + path);

        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child) return child;
            var objectToPlace = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(objectToPlace, Operation);
            objectToPlace.transform.SetParent(parent, false);
            return objectToPlace.transform;
        }
    }
}

