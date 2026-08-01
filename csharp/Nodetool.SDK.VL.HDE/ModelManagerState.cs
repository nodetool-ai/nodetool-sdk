using Nodetool.SDK.VL.Hde;
using VL.Core.Import;

namespace Nodetool.SDK.VL.HDE;

/// <summary>
/// Editor-only adapter for the dockable Nodetool model manager.
/// </summary>
[ProcessNode]
public sealed class ModelManagerState : IDisposable
{
    private readonly HdeModelManagerNode _state = new();

    /// <summary>
    /// Updates model catalog and download presentation state.
    /// </summary>
    public void Update(
        out string target,
        out string family,
        out string model,
        out string status,
        out string actionLabel,
        out float progress,
        out string progressText,
        out bool canAct,
        bool language = false,
        bool image = false,
        bool audio = false,
        [Pin(Name = "Video3D")] bool video3D = false,
        bool other = false,
        bool refresh = false,
        bool action = false)
    {
        _state.Language = language;
        _state.Image = image;
        _state.Audio = audio;
        _state.Video3D = video3D;
        _state.Other = other;
        _state.Refresh = refresh;
        _state.Action = action;
        _state.Update();
        _state.ReadState(
            out target,
            out family,
            out model,
            out status,
            out actionLabel,
            out progress,
            out progressText,
            out canAct);
    }

    void IDisposable.Dispose() => _state.Dispose();
}
