using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativePhoneCameraTests
    {
        [UnityTest]
        public IEnumerator LandscapePhoneSelectsTheSourcePhonePose()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            var flow = Object.FindObjectOfType<BeachGameFlow>();
            flow.enabled = false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var stage = typeof(BeachGameFlow).GetField("stage", flags).GetValue(flow);
            var type = stage.GetType();
            var phone = type.GetMethod("SetPhone", flags);
            Assert.That(phone, Is.Not.Null, "Source OnPhone selects the camera even in landscape.");
            type.GetMethod("ApplyProgress", flags).Invoke(stage, new object[] { new PlayerProgress { prestigeLevel = 1 } });
            var boxer = type.GetField("Player", flags).GetValue(stage);
            var player = (Transform)boxer.GetType().GetField("Root", flags).GetValue(boxer);
            var rig = (Transform)type.GetProperty("CameraPose", flags).GetValue(stage);
            Assert.That(Screen.width, Is.EqualTo(1920));
            Assert.That(Screen.height, Is.EqualTo(1080));
            phone.Invoke(stage, new object[] { true });
            type.GetMethod("FrameBeach", flags).Invoke(stage, new object[] { false });
            Assert.That(Vector3.Distance(rig.position - player.position, new Vector3(0, 18.53f, 32) * .356f), Is.LessThan(.001f));
            phone.Invoke(stage, new object[] { false });
            type.GetMethod("FrameBeach", flags).Invoke(stage, new object[] { false });
            Assert.That(Vector3.Distance(rig.position - player.position, new Vector3(0, 9.1f, 17.8f) * .356f), Is.LessThan(.001f));
        }
    }
}
