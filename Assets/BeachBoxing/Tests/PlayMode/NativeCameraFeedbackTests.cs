using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeCameraFeedbackTests
    {
        [UnityTest]
        public IEnumerator OriginalPunchCurvesAreAdditiveAndReturnToTheRigPose()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            var type = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.BeachNativeCameraFeedback");
            Assert.That(type, Is.Not.Null, "Source camera punch curves and shake must be restored.");
            var feedback = UnityEngine.Object.FindObjectOfType(type) as MonoBehaviour;
            Assert.That(feedback, Is.Not.Null, "Camera feedback must be preplaced.");
            feedback.enabled = false;
            var view = feedback.GetComponent<Camera>();
            var rig = view.transform.parent;
            Assert.That(rig, Is.Not.Null);
            type.GetMethod("Punch").Invoke(feedback, new object[] { true });
            type.GetMethod("Step").Invoke(feedback, new object[] { .1f });
            Assert.That(view.fieldOfView, Is.EqualTo(58.2f).Within(.0001f));
            Assert.That(Mathf.DeltaAngle(0, view.transform.localEulerAngles.x), Is.EqualTo(1).Within(.0001f));
            Assert.That(Mathf.DeltaAngle(0, view.transform.localEulerAngles.y), Is.EqualTo(-1).Within(.0001f));
            rig.position += Vector3.right * 3;
            type.GetMethod("Step").Invoke(feedback, new object[] { .166668f });
            Assert.That(view.fieldOfView, Is.EqualTo(60).Within(.0001f));
            Assert.That(Quaternion.Angle(view.transform.localRotation, Quaternion.identity), Is.LessThan(.0001f));
            Assert.That(view.transform.position, Is.EqualTo(rig.position));
        }
    }
}
