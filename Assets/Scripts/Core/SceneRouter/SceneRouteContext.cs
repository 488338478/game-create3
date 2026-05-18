namespace GameCreate3.Core.SceneRouting
{
    /// <summary>
    /// 一次场景切换的上下文。<see cref="SceneRouter"/> 在 OnBeforeChange / OnAfterChange 时下发。
    /// </summary>
    public readonly struct SceneRouteContext
    {
        public string FromScene { get; }
        public string ToRouteId { get; }
        public string ToScene { get; }
        public object Payload { get; }
        public bool UseLoading { get; }

        public SceneRouteContext(string fromScene, string toRouteId, string toScene, object payload, bool useLoading)
        {
            FromScene = fromScene;
            ToRouteId = toRouteId;
            ToScene = toScene;
            Payload = payload;
            UseLoading = useLoading;
        }
    }
}
