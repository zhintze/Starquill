using UnityEngine;
using Crosstales.TrueRandom.Util;

namespace Crosstales.TrueRandom.Module
{
   public abstract class BaseModule
   {
      #region Variables

      protected const int timeout = 5; //in seconds

      #endregion


      #region Events

      /// <summary>Event to get a message when an error occured.</summary>
      public static event ErrorInfo OnErrorInfo;

      #endregion


      #region Private methods

      protected static void onErrorInfo(string errorInfo, string id)
      {
         if (Config.DEBUG)
            Debug.Log($"onErrorInfo: {errorInfo} ({id})");

         OnErrorInfo?.Invoke(errorInfo, id);
      }

      #endregion
   }
}
// © 2016-2022 crosstales LLC (https://www.crosstales.com)