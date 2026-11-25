using UnityEngine;

namespace BeachBoxing
{
    public sealed class TrainingCycle
    {
        public float Progress { get; private set; }

        public int Advance(float seconds, float repetitionsPerSecond)
        {
            if (seconds <= 0 || repetitionsPerSecond <= 0) return 0;
            Progress += seconds * repetitionsPerSecond;
            int completed = Mathf.FloorToInt(Progress);
            Progress -= completed;
            return completed;
        }

        public float Speed { get; private set; } = 0.3f;

        public int AdvanceWorkout(float seconds, bool pressed, bool held, bool touch, bool accelerated = false)
        {
            if (seconds <= 0) return 0;
            if (pressed) Speed = touch ? 5f : 3.5f;
            Speed = Mathf.Lerp(Speed, accelerated ? 1.3f : 0.5f, seconds * (touch ? 16f : 25f));
            Progress += seconds * Speed;
            if (Progress < 0.99f) return 0;
            Progress = 0;
            return 1;
        }

        public void Reset()
        {
            Progress = 0;
        }

        public void ResetScene()
        {
            Progress = 0;
            Speed = .3f;
        }
    }

    public sealed class TrainingStation
    {
        public readonly Muscle Muscle;
        public readonly Vector3 Position;
        public readonly int Gain;

        public TrainingStation(Muscle muscle, Vector3 position, int gain = 11)
        {
            Muscle = muscle;
            Position = position;
            Gain = gain;
        }
    }
}
