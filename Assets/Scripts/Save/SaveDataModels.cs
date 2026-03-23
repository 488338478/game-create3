using System;

namespace GameCreate3
{
    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public string version = "0.1.0";
        public string sceneName = string.Empty;
        public SerializableVector3 playerPosition;
        public VariableSnapshot variables = new VariableSnapshot();
        public string[] completedObjectives = Array.Empty<string>();
    }
}
