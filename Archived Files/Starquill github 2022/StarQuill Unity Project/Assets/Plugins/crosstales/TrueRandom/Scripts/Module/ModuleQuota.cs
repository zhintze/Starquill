using UnityEngine;
using UnityEngine.Networking;
using Crosstales.TrueRandom.Util;

namespace Crosstales.TrueRandom.Module
{
   /// <summary>
   /// This module gets the remaining quota on www.random.org.
   /// </summary>
   public abstract class ModuleQuota : BaseModule
   {
      #region Variables

      private static int quota = 1000000;

      #endregion


      #region Static properties

      /// <summary>Returns the remaining quota in bits from the last check.</summary>
      /// <returns>Remaining quota in bits from the last check.</returns>
      public static int Quota => quota;

      #endregion


      #region Events

      /// <summary>Event to get a message with the current quota.</summary>
      public static event QuotaUpdate OnUpdateQuota;

      #endregion


      #region Public methods

      /// <summary>Gets the remaining quota in bits from the server.</summary>
      public static System.Collections.IEnumerator GetQuota()
      {
         if (Crosstales.Common.Util.NetworkHelper.isInternetAvailable)
         {
            using (UnityWebRequest www = UnityWebRequest.Get(Constants.GENERATOR_URL + "quota/?format=plain"))
            {
               www.timeout = timeout;
               www.downloadHandler = new DownloadHandlerBuffer();
               yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
               if (www.result != UnityWebRequest.Result.ProtocolError && www.result != UnityWebRequest.Result.ConnectionError)
#else
               if (!www.isHttpError && !www.isNetworkError)
#endif
               {
                  if (!int.TryParse(www.downloadHandler.text, out quota))
                  {
                     Debug.LogError("Could not parse value to integer: " + www.downloadHandler.text);
                  }
                  else
                  {
                     onUpdateQuota(quota);
                  }
               }
               else
               {
                  onErrorInfo(www.error, "quota");
                  Debug.LogWarning("Could not read from url: " + www.error);
               }
            }
         }
         else
         {
            const string msg = "No Internet access available - can not get quota!";
            Debug.LogWarning(msg);
            onErrorInfo(msg, "quota");

            quota = 1000000;
         }
      }

      #endregion


      #region Event-trigger methods

      private static void onUpdateQuota(int _quota)
      {
         if (Config.DEBUG)
            Debug.Log($"onUpdateQuota: {_quota}");

         OnUpdateQuota?.Invoke(_quota);
      }

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR

      /// <summary>Gets the remaining quota in bits from the server (Editor only).</summary>
      public static void GetQuotaInEditor()
      {
#if !UNITY_WSA && !UNITY_XBOXONE
         try
         {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = Crosstales.Common.Util.NetworkHelper.RemoteCertificateValidationCallback;

            using (System.Net.WebClient client = new Crosstales.Common.Util.CTWebClient(timeout * 1000))
            {
               using (System.IO.Stream stream = client.OpenRead(Constants.GENERATOR_URL + "quota/?format=plain"))
               {
                  using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                  {
                     string content = reader.ReadToEnd();

                     if (Config.DEBUG)
                        Debug.Log(content);

                     if (!int.TryParse(content, out quota))
                     {
                        Debug.LogWarning("Could not parse quota!: " + content);
                     }
                  }
               }
            }
         }
         catch (System.Exception ex)
         {
            if (!Helper.isEditor)
               Debug.LogError(ex);
         }
#endif
      }

#endif

      #endregion
   }
}
// © 2016-2022 crosstales LLC (https://www.crosstales.com)