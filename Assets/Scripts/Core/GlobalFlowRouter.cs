using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.Core
{
    public enum FlowNavigationKind
    {
        GoTo,
        Back,
        ResetToMainMenu
    }

    public readonly struct FlowNavigationEvent
    {
        public FlowNavigationKind Kind { get; }
        public string NodeId { get; }
        public object Payload { get; }

        public FlowNavigationEvent(FlowNavigationKind kind, string nodeId, object payload)
        {
            Kind = kind;
            NodeId = nodeId;
            Payload = payload;
        }
    }

    public sealed class GlobalFlowRouter : MonoBehaviour
    {
        public const string MainMenuNodeId = "MainMenu";

        [SerializeField] private bool dontDestroyOnLoad = true;

        private readonly Stack<FlowFrame> backStack = new Stack<FlowFrame>();

        private string currentNodeId = string.Empty;
        private object currentPayload;

        public static GlobalFlowRouter Instance { get; private set; }

        public string CurrentNodeId => currentNodeId;
        public object CurrentPayload => currentPayload;
        public int BackStackDepth => backStack.Count;

        public event Action<FlowNavigationEvent> OnNavigation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void GoTo(string nodeId, object payload = null)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                Debug.LogWarning("[GlobalFlowRouter] GoTo ignored: nodeId is empty.");
                return;
            }

            if (!string.IsNullOrEmpty(currentNodeId))
            {
                backStack.Push(new FlowFrame(currentNodeId, currentPayload));
            }

            currentNodeId = nodeId;
            currentPayload = payload;
            Raise(FlowNavigationKind.GoTo, currentNodeId, currentPayload);
        }

        public bool Back()
        {
            if (backStack.Count == 0)
            {
                return false;
            }

            var frame = backStack.Pop();
            currentNodeId = frame.NodeId;
            currentPayload = frame.Payload;
            Raise(FlowNavigationKind.Back, currentNodeId, currentPayload);
            return true;
        }

        public void ResetToMainMenu(object payload = null)
        {
            backStack.Clear();
            currentNodeId = MainMenuNodeId;
            currentPayload = payload;
            Raise(FlowNavigationKind.ResetToMainMenu, currentNodeId, currentPayload);
        }

        public string GetCurrentNode()
        {
            return currentNodeId;
        }

        public bool TryGetCurrentPayload<T>(out T value)
        {
            if (currentPayload is T cast)
            {
                value = cast;
                return true;
            }

            value = default;
            return false;
        }

        private void Raise(FlowNavigationKind kind, string nodeId, object payload)
        {
            OnNavigation?.Invoke(new FlowNavigationEvent(kind, nodeId, payload));
        }

        private readonly struct FlowFrame
        {
            public readonly string NodeId;
            public readonly object Payload;

            public FlowFrame(string nodeId, object payload)
            {
                NodeId = nodeId;
                Payload = payload;
            }
        }
    }
}
