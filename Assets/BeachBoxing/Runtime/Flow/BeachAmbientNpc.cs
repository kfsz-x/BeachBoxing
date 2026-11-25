using UnityEngine;

namespace BeachBoxing
{
    /// <summary>A preplaced native NPC, limited to the original stationary workout presentation.</summary>
    public sealed class BeachAmbientNpc : MonoBehaviour
    {
        public Animator Rig;
        public string Workout = "Chest";
        public GameObject StaticPivot;
        private float startDelay;

        private void OnEnable()
        {
            if (!Rig) return;
            Rig.applyRootMotion = false;
            Rig.SetBool("Grounded", true);
            Rig.SetBool("Walking", false);
            foreach (string muscle in new[] { "Chest", "Arms", "Back", "Legs" })
            {
                Rig.SetBool(muscle + "Idle", muscle == Workout);
                Rig.SetBool(muscle + "Workout", false);
            }
            Rig.SetBool("WorkingOut", true);
            Rig.SetFloat("WorkoutSpeed", 0.5f);
            if (StaticPivot) StaticPivot.SetActive(false);
            // NPC.InteractWithWorkoutStation invokes StartWorkoutStationAnim after 0.2 seconds.
            startDelay = 0.2f;
        }

        private void Update()
        {
            if (!Rig || startDelay <= 0) return;
            startDelay -= Time.deltaTime;
            if (startDelay <= 0) Rig.SetBool(Workout + "Workout", true);
        }

        private void OnDisable()
        {
            if (StaticPivot) StaticPivot.SetActive(true);
        }
    }
}
