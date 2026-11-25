using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Independent source beach rig follow and one-second machine-local camera transition.</summary>
    [DisallowMultipleComponent]
    public sealed class BeachNativeBeachCamera : MonoBehaviour
    {
        static readonly Vector3 SourceFollowPosition = new Vector3(-1.53f, 4.36f, 32.91f);
        public Transform player, target, view, trainingWorld;
        public bool Initialized { get; private set; }
        Vector3 followPosition, localPosition, idlePosition, machinePosition, machineLevelTen;
        Quaternion localRotation, idleRotation, machineRotation;
        float startingTime;
        bool startingWorkout;

        public void Initialize(Vector3 position, Vector3 euler)
        {
            followPosition = trainingWorld ? trainingWorld.TransformPoint(SourceFollowPosition) : player.position;
            localPosition = idlePosition = position;
            localRotation = idleRotation = Quaternion.Euler(euler);
            startingWorkout = false;
            startingTime = 0;
            Initialized = true;
            WritePose();
        }

        public void SetIdlePose(Vector3 position, Vector3 euler)
        {
            idlePosition = position;
            idleRotation = Quaternion.Euler(euler);
            followPosition = target.position;
            ExitWorkout();
        }

        public void BeginWorkout(Vector3 position, Vector3 positionLevelTen, Vector3 euler)
        {
            machinePosition = position;
            machineLevelTen = positionLevelTen;
            machineRotation = Quaternion.Euler(euler);
            startingTime = 0;
            startingWorkout = true;
        }

        public void ExitWorkout()
        {
            startingWorkout = false;
            startingTime = 0;
            localPosition = idlePosition;
            localRotation = idleRotation;
            WritePose();
        }

        public void Step(float seconds, int prestige)
        {
            if (!Initialized || !target || !view) return;
            seconds = Mathf.Max(0, seconds);
            followPosition = Vector3.Lerp(followPosition, target.position, seconds * 14);
            if (startingWorkout)
            {
                float fraction = (Mathf.Clamp(prestige, 1, 10) - 1) / 9f;
                localPosition = Vector3.Lerp(localPosition, Vector3.Lerp(machinePosition, machineLevelTen, fraction), seconds * 5);
                localRotation = Quaternion.Lerp(localRotation, machineRotation, seconds * 5);
                startingTime += seconds;
                if (startingTime >= 1) startingWorkout = false;
            }
            WritePose();
        }

        void WritePose()
        {
            if (view) view.SetPositionAndRotation(followPosition + localPosition, localRotation);
        }
    }
}
