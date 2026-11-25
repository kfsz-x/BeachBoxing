namespace BeachBoxing
{
    public enum BeachTutorialTarget { None, Arms, Ring }

    public static class BeachTutorialRules
    {
        public static bool UpdateProgress(PlayerProgress progress)
        {
            if (progress.completedTutorial != 0 || progress.arms < 51) return false;
            progress.completedTutorial = 1;
            return true;
        }

        public static BeachTutorialTarget Target(PlayerProgress progress) => progress.unlockedOpponent > 0
            ? BeachTutorialTarget.None : progress.completedTutorial == 0 ? BeachTutorialTarget.Arms : BeachTutorialTarget.Ring;

        public static bool OnBeachEntered(PlayerProgress progress, bool phone)
        {
            if (progress.unlockedOpponent <= 0 || progress.keepTrainingHint != 0) return false;
            progress.keepTrainingHint = 1;
            return !phone;
        }
    }
}
