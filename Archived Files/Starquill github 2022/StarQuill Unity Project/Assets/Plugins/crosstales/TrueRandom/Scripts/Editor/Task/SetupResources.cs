#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Crosstales.TrueRandom.EditorUtil;

namespace Crosstales.TrueRandom.EditorTask
{
   /// <summary>Moves all resources to 'Editor Default Resources'.</summary>
   [InitializeOnLoad]
   public class SetupResources : Crosstales.Common.EditorTask.BaseSetupResources
   {
      #region Constructor

      static SetupResources()
      {
         Setup();
      }

      #endregion


      #region Public methods

      public static void Setup()
      {
#if !CT_DEVELOP
         string path = Application.dataPath;
         string assetpath = "Assets" + EditorConfig.ASSET_PATH;

         string sourceFolder = path + EditorConfig.ASSET_PATH + "Icons/";
         string source = assetpath + "Icons/";

         string targetFolder = path + "/Editor Default Resources/crosstales/TrueRandom/";
         string target = "Assets/Editor Default Resources/crosstales/TrueRandom/";
         string metafile = assetpath + "Icons.meta";

         setupResources(source, sourceFolder, target, targetFolder, metafile);
#endif
      }

      #endregion
   }
}
#endif
// © 2016-2022 crosstales LLC (https://www.crosstales.com)