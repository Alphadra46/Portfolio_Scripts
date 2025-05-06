using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TwitchSDK;
using TwitchSDK.Interop;
using UnityEngine;
using UnityEngine.Networking;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private bool useMockAPI;
    private const string CHAT_REQUEST_URL = "https://api.twitch.tv/helix/chat/messages";
    private const string CHAT_REQUEST_URL_MOCK_API = "http://localhost:8080/mock/chat/messages";

    private ChatMessageRequestBody requestBody = new ChatMessageRequestBody();

    [ContextMenu("SendMessage")]
    public void TestSendChatMessage()
    {
        // requestBody = new ChatMessageRequestBody() {
        //     broadcaster_id = Twitch.API.GetMyStreamInfo().MaybeResult.Id,
        //     sender_id = Twitch.API.GetMyUserInfo().MaybeResult.ChannelId,
        // };

        requestBody.broadcaster_id = Twitch.API.GetMyUserInfo().MaybeResult.ChannelId;
        requestBody.sender_id = Twitch.API.GetMyUserInfo().MaybeResult.ChannelId;
        
        requestBody.message = "Hello! 123465";
        StartCoroutine(PostRequest(useMockAPI?CHAT_REQUEST_URL_MOCK_API:CHAT_REQUEST_URL,JsonUtility.ToJson(requestBody)));
    }
    
    public IEnumerator PostRequest(string url, string contents, params (string, string)[]queryParameters)
    {
        var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(contents));
        url = AddQueryParameters(url, queryParameters);
        
        var request = new UnityWebRequest(url, "POST", new DownloadHandlerBuffer(), uploadHandler);
        
        AddHeaders(request,true);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"{request.responseCode} : {request.error}");
        }
        
        print(request.result);
        print(request.downloadHandler.data);
        print(request.downloadHandler.text);
    }

    private string AddQueryParameters(string baseURL, params (string, string)[]queryParameters)
    {
        if (queryParameters == null || queryParameters.Length == 0)
            return baseURL;
        StringBuilder urlBuilder = new StringBuilder();
        urlBuilder.Append(baseURL);
        urlBuilder.Append("?");
        foreach (var param in queryParameters)
        {
            urlBuilder.Append(param.Item1);
            urlBuilder.Append("=");
            urlBuilder.Append(UnityWebRequest.EscapeURL(param.Item2));
            urlBuilder.Append("&");
        }
        return urlBuilder.ToString();
    }

    protected virtual void AddHeaders(UnityWebRequest webRequest, bool includeContentType)
    {
        webRequest.SetRequestHeader("Authorization",$"Bearer {AuthManager.instance.accessTokenResponseInfos?.access_token}");
        webRequest.SetRequestHeader("Client-Id",$"{TwitchSDKSettings.Instance.ClientId}");
        if (includeContentType)
            webRequest.SetRequestHeader("Content-Type","application/json");
    }
}

public class ChatMessageRequestBody
{
    public string broadcaster_id;
    public string sender_id;
    public string message;
}