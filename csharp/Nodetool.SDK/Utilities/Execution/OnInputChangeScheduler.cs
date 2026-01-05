using System;

namespace Nodetool.SDK.Utilities.Execution;

public enum OnInputChangeAction
{
    None = 0,
    Start = 1,
    QueueRerun = 2,
    CancelAndRestart = 3,
}

/// <summary>
/// Small helper for hosts that want "execute on input change" scheduling without spamming runs.
/// It does not execute anything itself; it only decides what should happen when a new input signature arrives.
/// </summary>
public sealed class OnInputChangeScheduler
{
    public string LastSignature { get; private set; } = "";
    public bool RerunRequested { get; private set; } = false;

    public void Reset(string? signature = null)
    {
        LastSignature = signature ?? "";
        RerunRequested = false;
    }

    public OnInputChangeAction NotifyInputs(string newSignature, bool isRunning, bool restartOnChange)
    {
        if (newSignature == null) throw new ArgumentNullException(nameof(newSignature));

        if (string.Equals(newSignature, LastSignature, StringComparison.Ordinal))
            return OnInputChangeAction.None;

        LastSignature = newSignature;

        if (!isRunning)
        {
            RerunRequested = false;
            return OnInputChangeAction.Start;
        }

        RerunRequested = true;
        return restartOnChange ? OnInputChangeAction.CancelAndRestart : OnInputChangeAction.QueueRerun;
    }

    public bool ConsumeRerunRequested()
    {
        var r = RerunRequested;
        RerunRequested = false;
        return r;
    }
}


