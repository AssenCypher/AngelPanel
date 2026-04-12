#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace AngelPanel.Editor
{
    public static class AP_CoreStorage
    {
        public static void EnsureDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static bool TrySave<T>(string path, T value)
        {
            if (string.IsNullOrWhiteSpace(path) || value == null)
            {
                return false;
            }

            try
            {
                EnsureDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(value, true));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryLoad<T>(string path, out T value) where T : class
        {
            value = null;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                value = JsonUtility.FromJson<T>(json);
                return value != null;
            }
            catch
            {
                value = null;
                return false;
            }
        }
    }
}
#endif
