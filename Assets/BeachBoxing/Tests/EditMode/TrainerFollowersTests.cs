using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class TrainerFollowersTests
    {
        [Test]
        public void EquipSwitchesPreplacedTrainersAndResetsOnlyTheNewFollower()
        {
            WithFollowers((view, state, player, models) =>
            {
                player.position = new Vector3(5f, 1f, 2f);
                state.equippedTrainer = 0;
                Apply(view, state, player);
                Assert.That(models[0].gameObject.activeSelf, Is.True);
                Assert.That(models[0].position, Is.EqualTo(player.position - player.forward * (7f * .356f)));
                Assert.That(models[0].localScale.x, Is.EqualTo(0.35956f).Within(0.00001f));

                Vector3 moved = player.position + Vector3.back;
                models[0].position = moved;
                Apply(view, state, player);
                Assert.That(models[0].position, Is.EqualTo(moved));

                state.equippedTrainer = 2;
                Apply(view, state, player);
                Assert.That(models[0].gameObject.activeSelf, Is.False);
                Assert.That(models[2].gameObject.activeSelf, Is.True);
                Assert.That(models[2].position, Is.EqualTo(player.position - player.forward * (7f * .356f)));

                Apply(view, null, player);
                foreach (var model in models) Assert.That(model.gameObject.activeSelf, Is.False);
            });
        }

        [Test]
        public void FollowUsesSeparateRoamingAndWorkoutDistances()
        {
            WithFollowers((view, state, player, models) =>
            {
                state.equippedTrainer = 0;
                Apply(view, state, player);
                models[0].position = Vector3.zero;
                player.position = new Vector3(2f, 9f, 0f);
                Step(view, 0.1f);
                Assert.That(models[0].position, Is.EqualTo(Vector3.zero));

                player.position = Vector3.right * 5f;
                Step(view, 0.1f);
                Assert.That(models[0].position.x, Is.EqualTo(0.712f).Within(0.00001f));

                var target = models[1];
                target.position = models[0].position + Vector3.forward;
                Apply(view, state, player, true, target);
                Step(view, 0.1f);
                Assert.That(models[0].position.z, Is.EqualTo(1f - .356f).Within(0.00001f));
            });
        }

        private static void WithFollowers(Action<Component, BeachPeripheralState, Transform, Transform[]> test)
        {
            Type type = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachTrainerFollowers");
            Assert.That(type, Is.Not.Null, "BeachTrainerFollowers is missing from the runtime assembly.");
            var group = new GameObject("Trainers");
            try
            {
                var view = group.AddComponent(type);
                var player = new GameObject("Player").transform;
                player.SetParent(group.transform, false);
                var models = new Transform[BeachPeripheralRules.TrainerCount];
                var data = new SerializedObject(view);
                data.FindProperty("groundMask").intValue = 0;
                var bindings = data.FindProperty("models");
                Assert.That(bindings, Is.Not.Null);
                bindings.arraySize = models.Length;
                for (int i = 0; i < models.Length; i++)
                {
                    var model = new GameObject("Trainer " + i);
                    model.transform.SetParent(group.transform, false);
                    model.SetActive(false);
                    models[i] = model.transform;
                    bindings.GetArrayElementAtIndex(i).FindPropertyRelative("root").objectReferenceValue = models[i];
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                var state = new BeachPeripheralState();
                state.trainerLevels[0] = 1;
                state.trainerLevels[2] = 4;
                test(view, state, player, models);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(group);
            }
        }

        private static void Apply(Component view, BeachPeripheralState state, Transform player,
            bool workoutActive = false, Transform workoutTarget = null)
        {
            MethodInfo method = view.GetType().GetMethod("Apply",
                new[] { typeof(BeachPeripheralState), typeof(Transform), typeof(bool), typeof(Transform) });
            Assert.That(method, Is.Not.Null);
            method.Invoke(view, new object[] { state, player, workoutActive, workoutTarget });
        }

        private static void Step(Component view, float seconds)
        {
            MethodInfo method = view.GetType().GetMethod("Step", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(view, new object[] { seconds });
        }
    }
}
