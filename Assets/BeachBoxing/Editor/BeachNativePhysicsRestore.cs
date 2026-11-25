using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeachBoxing.Editor
{
    public static class BeachNativePhysicsRestore
    {
        [MenuItem("Beach Boxing/Restore original actor physics")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated Beach Boxing scene.");
            var stage = new SerializedObject(flow).FindProperty("stage");
            var player = stage.FindPropertyRelative("Player").FindPropertyRelative("Root").objectReferenceValue as Transform;
            if (!flow.GetComponent<BeachNativeWorldPhysics>()) Undo.AddComponent<BeachNativeWorldPhysics>(flow.gameObject);
            var motor = player.GetComponent<BeachNativeLocomotion>();
            if (!motor) motor = Undo.AddComponent<BeachNativeLocomotion>(player.gameObject);
            Undo.RecordObject(motor, "Bind source jump physics");
            var body = player.GetComponent<Rigidbody>();
            Undo.RecordObject(body, "Restore source rigidbody");
            body.mass = 1;
            body.drag = 0;
            body.angularDrag = 0.05f;
            body.useGravity = true;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var shape = player.Find("PlayerCollider");
            if (!shape)
            {
                shape = new GameObject("PlayerCollider").transform;
                Undo.RegisterCreatedObjectUndo(shape.gameObject, "Preplace native player capsule");
                shape.SetParent(player, false);
                shape.localPosition = Vector3.up * (2.5578980446f * 0.356f);
                var capsule = Undo.AddComponent<CapsuleCollider>(shape.gameObject);
                capsule.radius = 0.75f * 0.356f;
                capsule.height = 5.0137600899f * 0.356f;
                capsule.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicMaterial>("Assets/BeachBoxing/OriginalArt/PhysicMaterial/Slippery.physicMaterial");
            }
            var feet = player.Find("Feet");
            if (!feet)
            {
                feet = new GameObject("Feet").transform;
                Undo.RegisterCreatedObjectUndo(feet.gameObject, "Preplace native feet probe");
                feet.SetParent(player, false);
                feet.localPosition = Vector3.up * (0.060997963f * 0.356f);
            }
            motor.body = body;
            motor.feet = feet;
            motor.groundMask = 8;
            player.gameObject.layer = 7;
            shape.gameObject.layer = 7;
            var enemy = stage.FindPropertyRelative("Enemy").FindPropertyRelative("Root").objectReferenceValue as Transform;
            var enemyBody = enemy.GetComponent<Rigidbody>();
            Undo.RecordObject(enemyBody, "Restore source enemy rigidbody36543");
            enemyBody.mass = 3;
            enemyBody.drag = 0;
            enemyBody.angularDrag = .05f;
            enemyBody.useGravity = true;
            enemyBody.isKinematic = false;
            enemyBody.interpolation = RigidbodyInterpolation.None;
            enemyBody.constraints = RigidbodyConstraints.FreezeRotation;
            enemyBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            enemy.gameObject.layer = 0;
            EditorUtility.SetDirty(enemyBody);
            var serializedFlow = new SerializedObject(flow);
            var beachSpawn = stage.FindPropertyRelative("beachSpawn").objectReferenceValue as Transform;
            Undo.RecordObject(beachSpawn, "Restore native beach spawn facing");
            beachSpawn.rotation = Quaternion.identity;
            var playerSpawn = stage.FindPropertyRelative("playerSpawn").objectReferenceValue as Transform;
            var enemySpawn = stage.FindPropertyRelative("enemySpawn").objectReferenceValue as Transform;
            Undo.RecordObject(playerSpawn, "Restore native opening player height");
            var arena = stage.FindPropertyRelative("arena").objectReferenceValue as Transform;
            playerSpawn.position = arena.TransformPoint(new Vector3(4.48916022f, .01779993f, 2.32112f));
            var head = player.Find("Tutorial head");
            if (!head)
            {
                head = new GameObject("Tutorial head").transform;
                Undo.RegisterCreatedObjectUndo(head.gameObject, "Preplace source tutorial head1444");
                head.SetParent(player, false);
                head.localPosition = Vector3.up * (3.3279981613f * 0.356f);
            }
            serializedFlow.FindProperty("stage").FindPropertyRelative("tutorialHead").objectReferenceValue = head;
            var arms = serializedFlow.FindProperty("stage").FindPropertyRelative("equipment").GetArrayElementAtIndex((int)Muscle.Arms).objectReferenceValue as Transform;
            var armsTarget = arms.Find("ArmsMachine (1)/Cube (9)");
            if (armsTarget) serializedFlow.FindProperty("stage").FindPropertyRelative("tutorialArmsTarget").objectReferenceValue = armsTarget;
            serializedFlow.ApplyModifiedProperties();
            var line = serializedFlow.FindProperty("stage").FindPropertyRelative("tutorialLine").objectReferenceValue as LineRenderer;
            if (line)
            {
                Undo.RecordObject(line, "Restore source TutorialLine3042");
                Undo.RecordObject(line.transform, "Restore source identity line space");
                line.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                line.transform.localScale = Vector3.one;
                const string lineMaterialPath = "Assets/BeachBoxing/OriginalArt/World/NativeArrowLine2021.mat";
                var lineMaterial = AssetDatabase.LoadAssetAtPath<Material>(lineMaterialPath);
                if (!lineMaterial)
                {
                    lineMaterial = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/BeachBoxing/OriginalArt/Material/ArrowLine.mat"));
                    lineMaterial.name = "NativeArrowLine2021";
                    lineMaterial.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/BeachBoxing/OriginalArt/Shader/NativeArrowLine2021.shader");
                    lineMaterial.SetVector("_NativeTextureScale", new Vector4(.2f / .356f, 1, 0, 0));
                    AssetDatabase.CreateAsset(lineMaterial, lineMaterialPath);
                }
                line.sharedMaterial = lineMaterial;
                line.useWorldSpace = false;
                line.loop = false;
                line.widthMultiplier = 3f * 0.356f;
                line.widthCurve = AnimationCurve.Constant(0, 1, 1);
                line.colorGradient = new Gradient();
                line.startColor = line.endColor = Color.white;
                line.textureMode = LineTextureMode.Tile;
                line.numCornerVertices = line.numCapVertices = 0;
                line.alignment = LineAlignment.View;
                line.generateLightingData = false;
                line.shadowBias = .5f;
                EditorUtility.SetDirty(line);
            }
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(motor);
            var trainers = flow.GetComponent<BeachTrainerFollowers>();
            if (trainers)
            {
                var models = new SerializedObject(trainers).FindProperty("models");
                for (int i = 0; i < models.arraySize; i++)
                {
                    var root = models.GetArrayElementAtIndex(i).FindPropertyRelative("root").objectReferenceValue as Transform;
                    if (!root) continue;
                    var trainerBody = root.GetComponent<Rigidbody>();
                    if (!trainerBody) trainerBody = Undo.AddComponent<Rigidbody>(root.gameObject);
                    Undo.RecordObject(trainerBody, "Restore source trainer gravity");
                    trainerBody.mass = 1;
                    trainerBody.drag = 0;
                    trainerBody.angularDrag = .05f;
                    trainerBody.useGravity = true;
                    trainerBody.isKinematic = false;
                    trainerBody.interpolation = RigidbodyInterpolation.None;
                    trainerBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    trainerBody.constraints = (RigidbodyConstraints)122;
                    var trainerCapsule = root.GetComponent<CapsuleCollider>();
                    if (!trainerCapsule) trainerCapsule = Undo.AddComponent<CapsuleCollider>(root.gameObject);
                    Undo.RecordObject(trainerCapsule, "Restore source trainer capsule");
                    trainerCapsule.center = Vector3.up * -.3549976349f;
                    trainerCapsule.radius = .5f;
                    trainerCapsule.height = 5.8154392242f;
                    trainerCapsule.direction = 1;
                    trainerCapsule.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicMaterial>("Assets/BeachBoxing/OriginalArt/PhysicMaterial/Slippery.physicMaterial");
                    root.gameObject.layer = 8;
                    EditorUtility.SetDirty(trainerBody);
                    EditorUtility.SetDirty(trainerCapsule);
                }
            }
            var physics = flow.GetComponent<BeachNativeWorldPhysics>();
            Undo.RecordObject(physics, "Prebind normalized native collider and body settings");
            physics.authoredColliders = flow.GetComponentsInChildren<Collider>(true);
            physics.authoredBodies = flow.GetComponentsInChildren<Rigidbody>(true);
            EditorUtility.SetDirty(physics);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
        }
    }
}
