using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Newtonsoft.Json;
using CodySource.Singleton;

namespace CodySource
{
    namespace Unity_MySQL_API
    {
        public class API : SingletonPersistent<API>
        {
            #region PROPERTIES

            /// <summary>
            /// Configure export properties
            /// </summary>
            [Header("CONFIG")]
            public string url = "";
            public string apiKey = "";
            public string payloadID = "";
            public bool overwrite = true;
            public bool isLoading => _isLoading;
            private bool _isLoading = false;

            private const string apiVersion = "1-0-0";

            [Header("EVENT CONFIG")]
            public UnityEvent<string> onSaveSuccess = new UnityEvent<string>();
            public UnityEvent<string> onSaveFailed = new UnityEvent<string>();
            public UnityEvent<string> onLoadSuccess = new UnityEvent<string>();
            public UnityEvent<string> onLoadFailed = new UnityEvent<string>();

            #endregion

            #region PUBLIC METHODS

            public void SetUrl(string pURL) => url = pURL;
            public void SetOverwrite(bool pOverwrite) => overwrite = pOverwrite;
            public void SetAPIKey(string pAPIKey) => apiKey = pAPIKey;
            public void SetPayloadID(string pPayloadID) => payloadID = pPayloadID;
            public void Create(string pVal) => StartCoroutine(_SQL_Request(Method.POST,pVal));
            public void Update(string pVal) => StartCoroutine(_SQL_Request(Method.PUT,pVal));
            public void Load() => StartCoroutine(_SQL_Request());
            public void Print(string pVal) => Debug.Log(pVal);
            public void ClearListeners()
            {
                onSaveSuccess.RemoveAllListeners();
                onSaveFailed.RemoveAllListeners();
                onLoadSuccess.RemoveAllListeners();
                onLoadFailed.RemoveAllListeners();
            }

            #endregion

            #region INTERNAL METHODS

            /// <summary>
            /// Send a sql request (save or load)
            /// </summary>
            internal IEnumerator _SQL_Request(Method pMethod = Method.GET, string pVal = "")
            {
                _isLoading = true;
                bool isSave = pVal != "";
                if (url == "") Fail("A url has not been set for the save/load request.");
                if (apiKey == "") Fail("An apiKey has not been set for the save/load request.");
                if (payloadID == "") Fail("A payload ID has not been set for the save/load request.");
                if (url == "" || apiKey == "" || payloadID == "") yield break;
                string payload = JsonConvert.SerializeObject(new { id = payloadID, value = pVal });
                WWWForm form = new WWWForm();
                if (isSave) form.AddField("payload", pVal);
                using (UnityWebRequest www = pMethod switch
                {
                    (Method.POST) => UnityWebRequest.Post(url, form),
                    (Method.PUT) => UnityWebRequest.Put(url, payload),
                    _ => UnityWebRequest.Get(url)
                })
                {
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("Api-Version", apiVersion);
                    www.SetRequestHeader("X-Api-Key", apiKey);
                    www.SetRequestHeader("Content-Length", $"{payload.Length}");
                    yield return www.SendWebRequest();
                    _isLoading = false;
                    if (www.result != UnityWebRequest.Result.Success) Fail(www.error);
                    else
                    {
                        try
                        {
                            ServerResponse response = JsonConvert.DeserializeObject<ServerResponse>(www.downloadHandler.text);
                            if (response.isEmpty) Fail(www.downloadHandler.text);
                            if (response.error != null && response.error != "") Fail(response.error);
                            else Success(response.value);
                        }
                        catch (System.Exception e) { Fail(e.Message); }
                    }
                }
                void Fail(string pMessage) => ((isSave) ? onSaveFailed : onLoadFailed)?.Invoke(pMessage);
                void Success(string pMessage) => ((isSave) ? onSaveSuccess : onLoadSuccess)?.Invoke(pMessage);
            }

            internal enum Method
            {
                GET,
                POST,
                PUT
            }

            [System.Serializable]
            internal struct ServerResponse
            {
                public bool isEmpty => (error == null || error == "") && (value == null || value == "");
                public string error;
                public string value;
            }

            #endregion
        }
    }
}