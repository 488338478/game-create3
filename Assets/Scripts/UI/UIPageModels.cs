using System;

namespace GameCreate3.UI
{
    public static class UIPageIds
    {
        public const string MainMenu = "main_menu";
        public const string Settings = "settings";
        public const string InGameHud = "in_game_hud";
        public const string PauseMenu = "pause_menu";
        public const string VictorySettlement = "victory_settlement";
        public const string FailureRetry = "failure_retry";
        public const string CGGallery = "cg_gallery";
        public const string ConfirmPopup = "confirm_popup";
        public const string SkipPrompt = "skip_prompt";
    }

    [Serializable]
    public sealed class UISettlementData
    {
        public bool cleared;
        public string title;
        public string message;
        public float clearTimeSeconds;
        public int score;
        public int retryCount;
    }

    [Serializable]
    public sealed class UIHUDData
    {
        public string objectiveText;
        public float normalizedProgress;
        public bool canPause = true;
    }
}
