namespace GameCreate3.DualWorld
{
    public readonly struct RealitySubmitResult
    {
        public RealitySubmitResult(string taskId, bool success, string failReason, int failCount, float taskDurationSec)
        {
            TaskId = taskId;
            Success = success;
            FailReason = failReason;
            FailCount = failCount;
            TaskDurationSec = taskDurationSec;
        }

        public string TaskId { get; }
        public bool Success { get; }
        public string FailReason { get; }
        public int FailCount { get; }
        public float TaskDurationSec { get; }
    }
}
