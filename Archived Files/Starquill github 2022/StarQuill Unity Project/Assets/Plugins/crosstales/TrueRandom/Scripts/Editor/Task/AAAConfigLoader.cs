#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Crosstales.TrueRandom.Util;

namespace Crosstales.TrueRandom.EditorTask
{
   /// <summary>Loads the configuration at startup.</summary>
   [InitializeOnLoad]
   public static class AAAConfigLoader
   {
      #region Constructor

      static AAAConfigLoader()
      {
         if (!Config.isLoaded)
         {
            Config.Load();

            if (Config.DEBUG)
               Debug.Log("Config data loaded");
         }
      }

      #endregion
   }
}
#endif
// © 2017-2022 crosstales LLC (https://www.crosstales.com)