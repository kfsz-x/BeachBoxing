using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class MuscleViewTests
    {
        [Test]
        public void LocalMorphUsesEachMusclesOwnFillAndUpdatesBothModels()
        {
            Type type = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachMuscleView");
            Assert.That(type, Is.Not.Null, "BeachMuscleView is missing from the runtime assembly.");
            var root = new GameObject("Player");
            try
            {
                var view = root.AddComponent(type);
                var data = new SerializedObject(view);
                data.Update();
                var training = NewRenderer(root.transform, "Training chest");
                var boxing = NewRenderer(root.transform, "Boxing chest");
                var arms = NewRenderer(root.transform, "Training arms");
                boxing.gameObject.SetActive(false);
                Bind(data.FindProperty("chest"), training, boxing);
                Bind(data.FindProperty("arms"), arms);
                data.ApplyModifiedPropertiesWithoutUndo();

                MethodInfo apply = type.GetMethod("Apply", new[] { typeof(PlayerProgress), typeof(int) });
                Assert.That(apply, Is.Not.Null);
                apply.Invoke(view, new object[] { new PlayerProgress { chest = 500, arms = 2000 }, 1 });

                AssertPose(training, 0.5f);
                AssertPose(boxing, 0.5f);
                AssertPose(arms, 1f);
                Assert.That(boxing.gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static MeshRenderer NewRenderer(Transform parent, string name)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.enabled = false;
            return renderer;
        }

        private static void Bind(SerializedProperty group, params Renderer[] renderers)
        {
            Assert.That(group, Is.Not.Null);
            group.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                var binding = group.GetArrayElementAtIndex(i);
                binding.FindPropertyRelative("renderer").objectReferenceValue = renderers[i];
                SetPose(binding.FindPropertyRelative("start"), Vector3.zero, Vector3.one, Quaternion.identity);
                SetPose(binding.FindPropertyRelative("end"), Vector3.up * 2f, Vector3.one * 3f,
                    Quaternion.Euler(0f, 90f, 0f));
            }
        }

        private static void SetPose(SerializedProperty pose, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            pose.FindPropertyRelative("position").vector3Value = position;
            pose.FindPropertyRelative("scale").vector3Value = scale;
            pose.FindPropertyRelative("rotation").quaternionValue = rotation;
        }

        private static void AssertPose(Renderer part, float fill)
        {
            Assert.That(part.enabled, Is.True);
            Assert.That(part.transform.localPosition, Is.EqualTo(Vector3.up * (2f * fill)));
            Assert.That(part.transform.localScale, Is.EqualTo(Vector3.one * (1f + 2f * fill)));
            Assert.That(Quaternion.Angle(part.transform.localRotation, Quaternion.Euler(0f, 90f * fill, 0f)),
                Is.LessThan(0.01f));
        }
    }
}
