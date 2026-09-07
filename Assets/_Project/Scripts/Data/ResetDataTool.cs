#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ResetDataTool
{
    [MenuItem("Tools/게임 데이터 완전 초기화")]
    public static void ResetData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        string path = Application.persistentDataPath;
        if (Directory.Exists(path))
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
        }

        Debug.Log($"[초기화 완료]");
    }
}
#endif