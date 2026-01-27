using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nodetool.SDK.Unity.Samples
{
    public class BasicWorkflowExample : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private InputField promptInput;
        [SerializeField] private Button runButton;
        [SerializeField] private Text statusText;
        [SerializeField] private RawImage resultImage;
        [SerializeField] private Slider progressSlider;

        [Header("Workflow")]
        [SerializeField] private string workflowName = "text_to_image";
        [SerializeField] private string defaultPrompt = "A scenic landscape at sunset";

        private NodetoolBridge _bridge;

        private async void Start()
        {
            EnsureBridge();
            EnsureUiFallbacks();

            SetStatus("Connecting...");
            if (runButton != null)
            {
                runButton.interactable = false;
            }

            var connected = await _bridge.ConnectAsync();

            if (connected)
            {
                SetStatus("Connected - Ready");
                if (runButton != null)
                {
                    runButton.interactable = true;
                    runButton.onClick.AddListener(OnRunClicked);
                }
                else
                {
                    await RunWorkflowAsync();
                }
            }
            else
            {
                SetStatus("Connection failed!");
            }
        }

        private async void OnRunClicked()
        {
            await RunWorkflowAsync();
        }

        private async Task RunWorkflowAsync()
        {
            if (runButton != null)
            {
                runButton.interactable = false;
            }

            SetStatus("Running workflow...");
            if (progressSlider != null)
            {
                progressSlider.value = 0;
            }

            var prompt = promptInput != null ? promptInput.text : defaultPrompt;
            var inputs = new Dictionary<string, object>
            {
                ["prompt"] = prompt
            };

            var progress = new System.Progress<float>(p =>
            {
                if (progressSlider != null)
                {
                    progressSlider.value = p;
                }
                SetStatus($"Progress: {p:P0}");
            });

            try
            {
                var result = await _bridge.RunWorkflowAsync(workflowName, inputs, progress);

                if (result.Success)
                {
                    SetStatus("Complete!");

                    var settings = NodetoolSettings.Load();
                    var apiBaseUrl = new System.Uri(settings.apiBaseUrl);
                    foreach (var kvp in result.Outputs)
                    {
                        var value = kvp.Value;
                        var typeDisc = value.TypeDiscriminator;

                        if (typeDisc == "image")
                        {
                            var texture = await UnityAssetManager.LoadTextureAsync(value, apiBaseUrl);
                            if (resultImage != null)
                            {
                                resultImage.texture = texture;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    SetStatus($"Failed: {result.Error}");
                }
            }
            catch (System.Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                Debug.LogException(ex);
            }

            if (runButton != null)
            {
                runButton.interactable = true;
            }
        }

        private void EnsureBridge()
        {
            if (NodetoolBridge.Instance == null)
            {
                var go = new GameObject("NodetoolBridge");
                go.AddComponent<NodetoolBridge>();
            }

            _bridge = NodetoolBridge.Instance;
        }

        private void EnsureUiFallbacks()
        {
            if (statusText != null && resultImage != null)
            {
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("NodetoolCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            if (statusText == null)
            {
                var statusObject = new GameObject("StatusText");
                statusObject.transform.SetParent(canvas.transform, false);
                statusText = statusObject.AddComponent<Text>();
                statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                statusText.alignment = TextAnchor.UpperLeft;
                var rect = statusText.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(16f, -16f);
                rect.sizeDelta = new Vector2(600f, 40f);
            }

            if (resultImage == null)
            {
                var imageObject = new GameObject("ResultImage");
                imageObject.transform.SetParent(canvas.transform, false);
                resultImage = imageObject.AddComponent<RawImage>();
                var rect = resultImage.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(16f, 16f);
                rect.sizeDelta = new Vector2(512f, 512f);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            Debug.Log(message);
        }

        private void OnDestroy()
        {
            if (runButton != null)
            {
                runButton.onClick.RemoveListener(OnRunClicked);
            }
        }
    }
}
