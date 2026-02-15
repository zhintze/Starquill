using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using Starquill.Data;
using Starquill.Managers;

public static class SetupGameManager
{
    [MenuItem("Tools/Setup GameManager")]
    public static void Execute()
    {
        // Ensure config directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Config"))
            AssetDatabase.CreateFolder("Assets/Data", "Config");

        // Create EconomyConfig asset if it doesn't exist
        var configPath = "Assets/Data/Config/DefaultEconomyConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<EconomyConfig>(configPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<EconomyConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            Debug.Log("Created DefaultEconomyConfig.asset");
        }

        // Create AdvantageMatrix asset if it doesn't exist
        var matrixPath = "Assets/Data/Config/DefaultAdvantageMatrix.asset";
        var matrix = AssetDatabase.LoadAssetAtPath<AdvantageMatrix>(matrixPath);
        if (matrix == null)
        {
            matrix = ScriptableObject.CreateInstance<AdvantageMatrix>();
            AssetDatabase.CreateAsset(matrix, matrixPath);
            Debug.Log("Created DefaultAdvantageMatrix.asset");
        }

        AssetDatabase.SaveAssets();

        // Remove existing GameManager objects to avoid duplicates
        var existing = GameObject.Find("GameManager");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log("Removed existing GameManager");
        }

        // Create GameManager GameObject
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();

        // Assign config references via serialized fields
        var so = new SerializedObject(gm);
        so.FindProperty("economyConfig").objectReferenceValue = config;
        so.FindProperty("advantageMatrix").objectReferenceValue = matrix;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(go);

        // Fix EventSystem — ensure it has StandaloneInputModule for Canvas UI buttons
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                EditorUtility.SetDirty(eventSystem.gameObject);
                Debug.Log("Added StandaloneInputModule to EventSystem");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("GameManager setup complete");
    }
}
