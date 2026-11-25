using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachBoxing.Tests
{
    public sealed class NativeCameraRestoreTests
    {
        const string ScenePath = "Assets/BeachBoxing/Scenes/BeachBoxing.unity";

        [TestCase(true)]
        [TestCase(false)]
        public void BothRestoreOrdersLeaveTheOpeningTrackOnTheBasePose(bool nativeCameraFirst)
        {
            WithScene(flow =>
            {
                if (nativeCameraFirst) ApplyNativeRestore();
                WriteOpeningTrack(flow);
                if (!nativeCameraFirst) ApplyNativeRestore();

                var data = new SerializedObject(flow);
                var stage = data.FindProperty("stage");
                var rig = stage.FindPropertyRelative("cameraPose").objectReferenceValue;
                var camera = (Camera)stage.FindPropertyRelative("camera").objectReferenceValue;
                var openingTarget = data.FindProperty("presentation").FindPropertyRelative("introCamera")
                    .FindPropertyRelative("target").objectReferenceValue;
                Assert.That(rig, Is.Not.Null);
                Assert.That(openingTarget, Is.SameAs(rig));
                Assert.That(camera.nearClipPlane, Is.EqualTo(.00356f).Within(.000001f));
                Assert.That(camera.farClipPlane, Is.EqualTo(712f).Within(.0001f));
            });
        }

        [Test]
        public void MissingTrainingWorldAbortsBeforeCreatingOrReparentingTheRig()
        {
            WithScene(flow =>
            {
                var data = new SerializedObject(flow);
                var stage = data.FindProperty("stage");
                var camera = (Camera)stage.FindPropertyRelative("camera").objectReferenceValue;
                stage.FindPropertyRelative("trainingWorld").objectReferenceValue = null;
                stage.FindPropertyRelative("cameraPose").objectReferenceValue = null;
                data.ApplyModifiedProperties();
                var originalParent = camera.transform.parent;
                int objectCount = flow.GetComponentsInChildren<Transform>(true).Length;

                var failure = TestDelegateException(ApplyNativeRestore);

                Assert.That(camera.transform.parent, Is.SameAs(originalParent));
                Assert.That(flow.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount));
                Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            });
        }

        static Exception TestDelegateException(TestDelegate run)
        {
            try { run(); }
            catch (TargetInvocationException error) { return error.InnerException; }
            return null;
        }

        static Type RestoreType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("BeachBoxing.Editor." + name)).First(item => item != null);

        static void ApplyNativeRestore() => RestoreType("BeachNativeCameraRestore")
            .GetMethod("Apply", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);

        static void WriteOpeningTrack(BeachGameFlow flow)
        {
            var restore = RestoreType("BeachTransitionRestore");
            var evidenceType = restore.GetNestedType("Evidence", BindingFlags.NonPublic);
            var evidencePath = Path.Combine(Application.dataPath, "../docs/beach-boxing/reference/presentation-timeline.json");
            var evidence = JsonUtility.FromJson(File.ReadAllText(evidencePath), evidenceType);
            var data = new SerializedObject(flow);
            restore.GetMethod("WriteIntroCamera", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[]
            {
                data.FindProperty("stage"), data.FindProperty("presentation").FindPropertyRelative("introCamera"),
                evidenceType.GetField("introCamera").GetValue(evidence), flow.transform
            });
            data.ApplyModifiedProperties();
        }

        static void WithScene(Action<BeachGameFlow> run)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                var flow = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BeachGameFlow>(true)).Single();
                Undo.RegisterCompleteObjectUndo(flow, "Camera restore test fixture");
                run(flow);
            }
            finally
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
