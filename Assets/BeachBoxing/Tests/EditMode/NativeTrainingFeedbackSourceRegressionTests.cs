using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class NativeTrainingFeedbackSourceRegressionTests
    {
        const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        static void WithFeedback(Action<Component, MethodInfo> test)
        {
            var type = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachNativeTrainingFeedback");
            Assert.That(type, Is.Not.Null);
            var sample = type.GetMethod("Sample", PublicInstance, null,
                new[] { typeof(float), typeof(float), typeof(float) }, null);
            Assert.That(sample, Is.Not.Null, "Sample must accept oldSpeed, absoluteTime and activeDt independently.");
            var go = new GameObject("Source feedback clock fixture");
            try { test(go.AddComponent(type), sample); }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        static void Call(Component c, string name, params object[] args) => c.GetType().GetMethod(name).Invoke(c,args);
        static int Face(Component c) => (int)c.GetType().GetProperty("FaceSourceId").GetValue(c);
        static void Sample(Component c, MethodInfo method, float speed, float absolute, float active) =>
            method.Invoke(c,new object[] { speed,absolute,active });

        [Test] public void FirstActiveFrameDoesNotEvaluateEvenAtLargeAbsoluteTime()
        {
            WithFeedback((c,sample) =>
            {
                Call(c,"Enter",Muscle.Chest);
                Sample(c,sample,.8f,100,.01f);
                Assert.That(Face(c),Is.EqualTo(43));
                Sample(c,sample,.8f,100.49f,.49f);
                Assert.That(Face(c),Is.EqualTo(45));
            });
        }
        [Test] public void ActiveTimerCarriesAcrossResetAndExitWhileIdleTimeDoesNotAccumulate()
        {
            WithFeedback((c,sample) =>
            {
                Call(c,"Enter",Muscle.Chest); Sample(c,sample,.8f,100,.3f);
                Call(c,"ResetBar"); Call(c,"Exit"); Call(c,"Tick",10f);
                Call(c,"Enter",Muscle.Arms); Sample(c,sample,1.2f,110,.19f);
                Assert.That(Face(c),Is.EqualTo(43));
                Sample(c,sample,1.2f,110.01f,.011f);
                Assert.That(Face(c),Is.EqualTo(46));
                Assert.That((float)c.GetType().GetProperty("MeanSpeed").GetValue(c),Is.EqualTo(1.2f).Within(.00001f),
                    "Absolute time still expires the old history independently of the active evaluation timer.");
            });
        }
        [Test] public void EvaluationResetsTimerToZeroAndSceneResetClearsTheCarry()
        {
            WithFeedback((c,sample) =>
            {
                Call(c,"Enter",Muscle.Chest); Sample(c,sample,1.2f,100,.6f);
                Assert.That(Face(c),Is.EqualTo(46));
                Sample(c,sample,.8f,101,.4f);
                Assert.That(Face(c),Is.EqualTo(46));
                Sample(c,sample,.8f,101.1f,.101f);
                Assert.That(Face(c),Is.EqualTo(45));
                Sample(c,sample,1.2f,102,.4f);
                Call(c,"ResetScene"); Call(c,"Enter",Muscle.Chest);
                Sample(c,sample,1.2f,200,.11f);
                Assert.That(Face(c),Is.EqualTo(43));
            });
        }
        [TestCase("ArmsParticle")] [TestCase("LegsParticle")]
        [TestCase("ChestParticle")] [TestCase("BackParticle")]
        public void ImporterPreservesOriginalParticleAndGainShadowSiblingOrder(string name)
        {
            var helper = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BeachBoxing.Editor.BeachNativeTrainingFeedbackRestore"))
                .FirstOrDefault(t => t != null);
            Assert.That(helper,Is.Not.Null);
            var dataType = helper.GetNestedType("Data",BindingFlags.NonPublic);
            var source = JsonUtility.FromJson(File.ReadAllText("Assets/BeachBoxing/OriginalArt/NativeTrainingFeedback/NativeFeedbackData.json"),dataType);
            var pools = (Array)dataType.GetField("pools").GetValue(source);
            var pool = pools.Cast<object>().First(p => (string)p.GetType().GetField("name").GetValue(p) == name);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            var fixture = new GameObject("Source reward draw order fixture",typeof(RectTransform),typeof(Canvas));
            var target = new GameObject("target",typeof(RectTransform)); target.transform.SetParent(fixture.transform,false);
            var clip = new AnimationClip();
            // PlaceSlot is owned by Apply's parent hierarchy transaction; mirror that owner in this fixture.
            Undo.RegisterFullObjectHierarchyUndo(fixture, "Source reward fixture transaction");
            try
            {
                var slot = (Component)helper.GetMethod("PlaceSlot",BindingFlags.Static|BindingFlags.NonPublic)
                    .Invoke(null,new object[] { fixture.transform,pool,0,target.transform,clip,null });
                Assert.That(Enumerable.Range(0,slot.transform.childCount).Select(i => slot.transform.GetChild(i).name).ToArray(),
                    Is.EqualTo(new[] { "Fade","TextHolder","Image","CollectCoinSFX" }));
                var holder = slot.transform.Find("TextHolder");
                Assert.That(Enumerable.Range(0,holder.childCount).Select(i => holder.GetChild(i).name).ToArray(),
                    Is.EqualTo(new[] { "Fade","Text (TMP)","Text (TMP) (1)" }),"Native black gain shadow must draw before its white foreground.");
            }
            finally
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                UnityEngine.Object.DestroyImmediate(fixture);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
    }
}
