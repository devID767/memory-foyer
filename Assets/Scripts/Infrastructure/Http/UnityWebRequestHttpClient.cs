using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Application.Configuration;
using MemoryFoyer.Application.Http;
using UnityEngine;
using UnityEngine.Networking;

namespace MemoryFoyer.Infrastructure.Http
{
    public sealed class UnityWebRequestHttpClient : IHttpClient
    {
        private const string ContentTypeJson = "application/json";

        private readonly ServerConfig _config;

        public UnityWebRequestHttpClient(ServerConfig config)
        {
            _config = config;
        }

        public UniTask<TResponse> GetAsync<TResponse>(string path, CancellationToken ct = default)
        {
            return SendWithRetryAsync<TResponse>(HttpMethod.Get, path, body: null, ct);
        }

        public UniTask<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
        {
            string json = JsonUtility.ToJson(body);
            return SendWithRetryAsync<TResponse>(HttpMethod.Post, path, json, ct);
        }

        private async UniTask<TResponse> SendWithRetryAsync<TResponse>(HttpMethod method, string path, string? body, CancellationToken ct)
        {
            int attempt = 0;
            int maxAttempts = 1 + Math.Max(0, _config.Retries);

            while (true)
            {
                attempt++;
                try
                {
                    return await SendOnceAsync<TResponse>(method, path, body, ct);
                }
                catch (HttpTransportException)
                {
                    if (attempt >= maxAttempts)
                    {
                        throw;
                    }
                    await UniTask.Delay(_config.RetryBackoff, cancellationToken: ct);
                }
            }
        }

        private async UniTask<TResponse> SendOnceAsync<TResponse>(HttpMethod method, string path, string? body, CancellationToken ct)
        {
            string url = ComposeUrl(_config.BaseUrl, path);

            using UnityWebRequest request = method == HttpMethod.Get
                ? UnityWebRequest.Get(url)
                : BuildPostRequest(url, body ?? string.Empty);

            request.timeout = (int)_config.RequestTimeout.TotalSeconds;

            try
            {
                await request.SendWebRequest().WithCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnityWebRequestException ex)
            {
                throw TranslateFailure(ex.UnityWebRequest, ex);
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw TranslateFailure(request, inner: null);
            }

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            return ParseJson<TResponse>(responseText, (int)request.responseCode);
        }

        private static UnityWebRequest BuildPostRequest(string url, string jsonBody)
        {
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", ContentTypeJson);
            request.SetRequestHeader("Accept", ContentTypeJson);
            return request;
        }

        private static Exception TranslateFailure(UnityWebRequest request, Exception? inner)
        {
            int statusCode = (int)request.responseCode;
            string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            string error = string.IsNullOrEmpty(request.error) ? "request failed" : request.error;

            switch (request.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                    if (statusCode >= 400 && statusCode < 500)
                    {
                        return new HttpContractException(statusCode, body, $"HTTP {statusCode}: {error}", inner);
                    }
                    return new HttpTransportException($"HTTP {statusCode}: {error}", inner);

                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    return new HttpTransportException(error, inner);

                default:
                    return new HttpTransportException(error, inner);
            }
        }

        private static TResponse ParseJson<TResponse>(string responseText, int statusCode)
        {
            try
            {
                TResponse parsed = JsonUtility.FromJson<TResponse>(responseText);
                if (parsed is null)
                {
                    throw new HttpContractException(statusCode, responseText, "malformed JSON: empty response");
                }
                return parsed;
            }
            catch (HttpContractException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpContractException(statusCode, responseText, "malformed JSON", ex);
            }
        }

        private static string ComposeUrl(string baseUrl, string path)
        {
            return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
        }

        private enum HttpMethod
        {
            Get,
            Post,
        }
    }
}
