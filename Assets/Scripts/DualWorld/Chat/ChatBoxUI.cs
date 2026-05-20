using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    /// <summary>
    /// scene 根驻留的右屏聊天框：持滚动 log + Submit 按钮。
    /// log 生命周期 = scene 卸载时随之销毁（跨 SubLevel / 跨 workspace 持续）。
    /// </summary>
    public sealed class ChatBoxUI : MonoBehaviour
    {
        [SerializeField] private ChatTaskPanelUI logPanel;
        [SerializeField] private Button submitButton;

        public event Action SubmitRequested;

        private void Awake()
        {
            if (logPanel == null) logPanel = GetComponentInChildren<ChatTaskPanelUI>(true);
            if (submitButton != null) submitButton.onClick.AddListener(HandleSubmitClicked);
        }

        private void OnDestroy()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(HandleSubmitClicked);
        }

        public void SetTaskHeader(ChatTaskDefinition def)
        {
            if (logPanel != null) logPanel.SetTaskHeader(def);
        }

        public void Append(ChatLogEntry entry)
        {
            if (logPanel != null) logPanel.Append(entry);
        }

        public void Clear()
        {
            if (logPanel != null) logPanel.Clear();
        }

        public void SetSubmitInteractable(bool v)
        {
            if (submitButton != null) submitButton.interactable = v;
        }

        public void SetSubmitVisible(bool v)
        {
            if (submitButton != null) submitButton.gameObject.SetActive(v);
        }

        private void HandleSubmitClicked()
        {
            SubmitRequested?.Invoke();
        }
    }
}
