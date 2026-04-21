using System.IO;
using UnityEngine;

namespace GameCreate3
{
    public static class JsonSaveUtility
    {
        public static void Save<T>(string fileName, T payload)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Debug.LogWarning("[JsonSaveUtility] File name is empty.");
                return;
            }

            var fullPath = BuildPath(fileName);
            var json = JsonUtility.ToJson(payload, true);
            File.WriteAllText(fullPath, json);
        }

        public static bool TryLoad<T>(string fileName, out T payload)
        {
            payload = default;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var fullPath = BuildPath(fileName);
            if (!File.Exists(fullPath))
            {
                return false;
            }

            var json = File.ReadAllText(fullPath);
            payload = JsonUtility.FromJson<T>(json);
            return payload != null;
        }

        public static void Delete(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var fullPath = BuildPath(fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public static string BuildPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
