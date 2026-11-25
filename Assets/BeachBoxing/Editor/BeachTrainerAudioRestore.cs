using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace BeachBoxing.Editor
{
    public static class BeachTrainerAudioRestore
    {
        private const string Operation = "Bind trainer footstep audio";
        private const string ClipPath = "Assets/BeachBoxing/OriginalArt/NativeCombatAudio/native-combat-0-364.mp3";

        [MenuItem("Beach Boxing/Bind Trainer Footstep Audio")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before binding trainer audio.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            var followers = flow ? flow.GetComponent<BeachTrainerFollowers>() : null;
            if (!followers) throw new InvalidOperationException("Open the populated trainer scene first.");
            var models = new SerializedObject(followers).FindProperty("models");
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                "Assets/BeachBoxing/OriginalArt/AudioMixerController/Main.mixer");
            var sfx = mixer ? mixer.FindMatchingGroups("SFX").SingleOrDefault() : null;
            if (!clip || !sfx || models.arraySize < 2)
                throw new InvalidOperationException("The footstep clip, SFX mixer and two trainers must be imported.");

            var animators = new Animator[2];
            var children = new Transform[2, 2];
            for (int index = 0; index < 2; index++)
            {
                animators[index] = models.GetArrayElementAtIndex(index).FindPropertyRelative("animator").objectReferenceValue as Animator;
                if (!animators[index]) throw new InvalidOperationException("Missing trainer Animator " + index);
                children[index, 0] = animators[index].transform.Find("FootstepSFX");
                children[index, 1] = animators[index].transform.Find("Footstep2SFX");
                if (!children[index, 0] || !children[index, 1])
                    throw new InvalidOperationException("Missing preplaced footstep anchors on trainer " + index);
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(Operation);
            try
            {
                for (int index = 0; index < 2; index++)
                {
                    var animator = animators[index];
                    var receiver = animator.GetComponent<BeachNativeBoxingAudio>();
                    if (!receiver) receiver = Undo.AddComponent<BeachNativeBoxingAudio>(animator.gameObject);
                    Undo.RecordObject(receiver, Operation);
                    Undo.RecordObject(animator, Operation);
                    receiver.step1 = BindChannel(children[index, 0], clip, sfx);
                    receiver.step2 = BindChannel(children[index, 1], clip, sfx);
                    animator.fireEvents = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(receiver);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                }
                EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static AudioSource BindChannel(Transform anchor, AudioClip clip, AudioMixerGroup mixer)
        {
            var source = anchor.GetComponent<AudioSource>();
            if (!source) source = Undo.AddComponent<AudioSource>(anchor.gameObject);
            Undo.RecordObject(source, Operation);
            source.clip = clip;
            source.outputAudioMixerGroup = mixer;
            source.playOnAwake = false;
            source.loop = false;
            source.volume = .16f;
            source.pitch = 1;
            source.spatialBlend = 1;
            source.minDistance = .356f;
            source.maxDistance = 35.6f;
            source.rolloffMode = AudioRolloffMode.Custom;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
                new AnimationCurve(new Keyframe(0, 1, 0, 0), new Keyframe(1, 0, 0, 0)));
            PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            return source;
        }
    }
}
