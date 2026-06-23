namespace GameCreate3.Level3
{
    public static class Level3Events
    {
        public const string Phase1 = "phase.1";
        public const string Phase2 = "phase.2";
        public const string Phase3 = "phase.3";

        public const string PlayerHit = "player.hit";
        public const string PlayerDefeated = "player.defeated";

        public const string ParrySuccess = "parry.success";

        public const string FollowerChanged = "follower.changed";
        public const string FollowerPrefix = "follower.";
        public const string Follower2000 = "follower.2000";
        public const string Follower4000 = "follower.4000";
        public const string Follower6000 = "follower.6000";
        public const string Follower8000 = "follower.8000";

        public const string AnimalRevealPrefix = "animal.reveal.";
        public const string InteractAnimalPrefix = "interact.animal.";

        public const string SequenceCorrect = "sequence.correct";
        public const string SequenceWrong = "sequence.wrong";
        public const string SequenceComplete = "sequence.complete";

        public const string LevelComplete = "level.complete";
        public const string LevelFail = "level.fail";

        public static string Follower(int threshold) => $"{FollowerPrefix}{threshold}";
        public static string AnimalReveal(int index) => $"{AnimalRevealPrefix}{index}";
    }
}
