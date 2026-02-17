using UnityEngine;
using UnityEditor;

public static class ClearSave
{
    [MenuItem("Tools/Clear Save Data")]
    public static void Execute()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs cleared!");
    }
}
