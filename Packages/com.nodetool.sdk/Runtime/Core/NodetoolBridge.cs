using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;
using UnityEngine;

namespace Nodetool.SDK.Unity
{
    public sealed class NodetoolBridge : MonoBehaviour
    {
        public static NodetoolBridge Instance { get; private set; }

        [SerializeField] private NodetoolSettings settings;

        private NodeToolExecutionClient _client;
        private bool _isConnected;

        public bool IsConnected => _isConnected;
        public event Action<bool> OnConnectionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (settings == null)
            {
                settings = NodetoolSettings.Load();
            }
        }

        public async Task<bool> ConnectAsync(CancellationToken ct = default)
        {
            if (_client != null)
            {
                await DisconnectAsync();
            }

            var options = new NodeToolClientOptions
            {
                WorkerWebSocketUrl = new Uri(settings.workerWebSocketUrl),
                ApiBaseUrl = new Uri(settings.apiBaseUrl),
                AuthToken = settings.apiKey,
                UserId = settings.userId
            };

            _client = new NodeToolExecutionClient(options);
            _client.ConnectionStatusChanged += OnConnectionStatus;

            _isConnected = await _client.ConnectAsync(ct);
            return _isConnected;
        }

        public async Task DisconnectAsync()
        {
            if (_client != null)
            {
                await _client.DisconnectAsync();
                _client.ConnectionStatusChanged -= OnConnectionStatus;
                _client.Dispose();
                _client = null;
            }

            _isConnected = false;
        }

        public async Task<WorkflowResult> RunWorkflowAsync(
            string workflowName,
            Dictionary<string, object> inputs = null,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Nodetool server");
            }

            var session = await _client.ExecuteWorkflowByNameAsync(workflowName, inputs, ct);

            session.ProgressChanged += p =>
            {
                MainThreadDispatcher.Instance.Enqueue(() => progress?.Report(p));
            };

            var result = new WorkflowResult();

            session.OutputReceived += update =>
            {
                if (update.NodeName == null || update.OutputName == null)
                {
                    return;
                }

                MainThreadDispatcher.Instance.Enqueue(() =>
                {
                    result.AddOutput(update.NodeName, update.OutputName, update.Value);
                });
            };

            session.Completed += (success, error) =>
            {
                MainThreadDispatcher.Instance.Enqueue(() =>
                {
                    result.Success = success;
                    result.Error = error;
                });
            };

            await session.WaitForCompletionAsync(ct);
            result.Outputs ??= new Dictionary<string, NodeToolValue>(StringComparer.Ordinal);
            foreach (var kvp in session.GetLatestOutputs())
            {
                if (!result.Outputs.ContainsKey(kvp.Key))
                {
                    result.Outputs[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        private void OnConnectionStatus(string status)
        {
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                _isConnected = status == "connected";
                OnConnectionChanged?.Invoke(_isConnected);
            });
        }

        private async void OnDestroy()
        {
            if (_client != null)
            {
                await DisconnectAsync();
            }
        }
    }

    public sealed class WorkflowResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public Dictionary<string, NodeToolValue> Outputs { get; set; }

        public void AddOutput(string node, string output, NodeToolValue value)
        {
            if (node == null || output == null || value == null)
            {
                return;
            }

            Outputs ??= new Dictionary<string, NodeToolValue>(StringComparer.Ordinal);
            Outputs[$"{node}:{output}"] = value;
        }
    }
}
