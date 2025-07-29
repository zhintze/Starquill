using UnityEngine;

namespace Crosstales.TrueRandom.Util
{
   /// <summary>Setup the project to use True Random.</summary>
#if UNITY_EDITOR
   [UnityEditor.InitializeOnLoadAttribute]
#endif
   public class SetupProject
   {
      #region Constructor

      static SetupProject()
      {
         setup();
      }

      #endregion


      #region Public methods

      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
      private static void setup()
      {
         Crosstales.Common.Util.Singleton<TRManager>.PrefabPath = "Prefabs/TrueRandom";
         Crosstales.Common.Util.Singleton<TRManager>.GameObjectName = "TrueRandom";
      }

      #endregion
   }
}
// © 2020-2022 crosstales LLC (https://www.crosstales.com)