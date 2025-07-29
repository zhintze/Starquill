#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Crosstales.TrueRandom.EditorUtil;
using Crosstales.TrueRandom.Util;

namespace Crosstales.TrueRandom.EditorTask
{
   /// <summary>Automatically adds the necessary TrueRandom-prefabs to the current scene.</summary>
   [InitializeOnLoad]
   public class AutoInitialize
   {
      #region Variables

      private static Scene currentScene;

      #endregion


      #region Constructor

      static AutoInitialize()
      {
         EditorApplication.hierarchyChanged += hierarchyWindowChanged;
      }

      #endregion


      #region Private static methods

      private static void hierarchyWindowChanged()
      {
         if (currentScene != EditorSceneManager.GetActiveScene())
         {
            if (EditorConfig.PREFAB_AUTOLOAD)
            {
               if (!EditorHelper.isTrueRandomInScene)
                  EditorHelper.InstantiatePrefab(Constants.TRUERANDOM_SCENE_OBJECT_NAME);
            }

            currentScene = EditorSceneManager.GetActiveScene();
         }
      }

      #endregion
   }
}
#endif
// © 2016-2022 crosstales LLC (https://www.crosstales.com)