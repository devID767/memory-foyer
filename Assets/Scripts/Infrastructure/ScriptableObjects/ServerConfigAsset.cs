using System;
using MemoryFoyer.Application.Configuration;
using UnityEngine;

namespace MemoryFoyer.Infrastructure.ScriptableObjects
{
    [CreateAssetMenu(menuName = "MemoryFoyer/Server Config", fileName = "ServerConfig")]
    public sealed class ServerConfigAsset : ScriptableObject
    {
        [SerializeField] private string _baseUrl = "http://localhost:3000";
        [SerializeField] private float _requestTimeoutSeconds = 5f;
        [SerializeField] private int _retries = 2;
        [SerializeField] private float _retryBackoffSeconds = 0.5f;

        public ServerConfig ToConfig() => new ServerConfig(
            BaseUrl: _baseUrl,
            RequestTimeout: TimeSpan.FromSeconds(_requestTimeoutSeconds),
            Retries: _retries,
            RetryBackoff: TimeSpan.FromSeconds(_retryBackoffSeconds));
    }
}
