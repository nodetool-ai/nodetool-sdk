using System.Numerics;
using ImGuiNET;
using Nodetool.SDK.VL.Hde;
using VL.Core.Import;
using ImGui = ImGuiNET.ImGui;
using ImGuiContext = VL.ImGui.Context;

namespace Nodetool.SDK.VL.HDE;

/// <summary>
/// Editor-only ImGui view for the Nodetool model catalog and download manager.
/// Filtering, paging, and actions remain in the reusable SDK.VL state layer.
/// </summary>
[ProcessNode]
public sealed class ModelManagerView : IDisposable
{
    private static readonly HdeModelFamily[] Families =
    [
        HdeModelFamily.All,
        HdeModelFamily.Language,
        HdeModelFamily.Image,
        HdeModelFamily.Audio,
        HdeModelFamily.Video,
        HdeModelFamily.ThreeD,
        HdeModelFamily.Other
    ];

    private static readonly int[] PageSizes = [50, 100, 200];
    private readonly HdeModelManagerNode _state = new();
    private string _search = "";

    /// <summary>
    /// Renders the manager within the active VL.ImGui region.
    /// </summary>
    public void Update(ImGuiContext? context)
    {
        _state.Update();
        if (context == null) return;

        using var frame = context.MakeCurrent();
        Render(_state.ReadState());
    }

    private void Render(HdeModelPageSnapshot view)
    {
        ImGui.TextUnformatted(view.Target);
        RenderFamilyTabs(view.Family);
        RenderToolbar();

        if (!string.IsNullOrWhiteSpace(view.Notice))
            ImGui.TextWrapped(view.Notice);
        ImGui.TextUnformatted(view.Summary);

        var tableHeight = Math.Max(120f, ImGui.GetContentRegionAvail().Y - 34f);
        var flags = ImGuiTableFlags.RowBg |
                    ImGuiTableFlags.BordersInnerH |
                    ImGuiTableFlags.BordersOuter |
                    ImGuiTableFlags.Resizable |
                    ImGuiTableFlags.ScrollY |
                    ImGuiTableFlags.SizingStretchProp;
        if (ImGui.BeginTable("##nodetool-models", 4, flags, new Vector2(0f, tableHeight)))
        {
            try
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Model", ImGuiTableColumnFlags.WidthStretch, 2.5f);
                ImGui.TableSetupColumn("Source / Type", ImGuiTableColumnFlags.WidthStretch, 1.8f);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthStretch, 1.7f);
                ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 92f);
                ImGui.TableHeadersRow();

                foreach (var row in view.Rows)
                    RenderRow(row);
            }
            finally
            {
                ImGui.EndTable();
            }
        }

        RenderPagination(view);
    }

    private void RenderFamilyTabs(string selectedFamily)
    {
        foreach (var family in Families)
        {
            var label = HdeModelFamilyClassifier.Label(family);
            var selected = string.Equals(label, selectedFamily, StringComparison.Ordinal);
            if (selected)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            try
            {
                if (ImGui.SmallButton($"{label}##family-{family}"))
                    _state.SelectFamily(family);
            }
            finally
            {
                if (selected)
                    ImGui.PopStyleColor();
            }
            if (family != Families[^1])
                ImGui.SameLine();
        }
    }

    private void RenderToolbar()
    {
        ImGui.SetNextItemWidth(Math.Max(120f, ImGui.GetContentRegionAvail().X - 92f));
        if (ImGui.InputTextWithHint("##model-search", "Search models...", ref _search, 256))
            _state.SetSearch(_search);
        ImGui.SameLine();
        if (ImGui.Button("Refresh##models"))
            _state.Refresh();
    }

    private void RenderRow(HdeModelRow row)
    {
        ImGui.PushID(row.Key);
        try
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(row.DisplayName);
            if (row.Recommended)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("recommended");
            }

            ImGui.TableSetColumnIndex(1);
            ImGui.TextWrapped(row.Details);

            ImGui.TableSetColumnIndex(2);
            if (row.IsDownloading)
            {
                var overlay = string.IsNullOrWhiteSpace(row.ProgressText)
                    ? row.Status
                    : row.ProgressText;
                ImGui.ProgressBar(row.Progress, new Vector2(-1f, 0f), overlay);
            }
            else
            {
                ImGui.TextWrapped(string.IsNullOrWhiteSpace(row.ProgressText)
                    ? row.Status
                    : $"{row.Status}: {row.ProgressText}");
            }

            ImGui.TableSetColumnIndex(3);
            if (!row.CanAct)
                ImGui.BeginDisabled();
            try
            {
                if (ImGui.Button($"{row.ActionLabel}##action", new Vector2(-1f, 0f)) && row.CanAct)
                    _state.Act(row.Key);
            }
            finally
            {
                if (!row.CanAct)
                    ImGui.EndDisabled();
            }
        }
        finally
        {
            ImGui.PopID();
        }
    }

    private void RenderPagination(HdeModelPageSnapshot view)
    {
        var range = view.TotalCount == 0
            ? "0 models"
            : $"{view.RangeStart}–{view.RangeEnd} of {view.TotalCount}";
        ImGui.TextUnformatted(range);
        ImGui.SameLine();

        if (SmallButton("Previous##page", view.PageNumber > 1))
            _state.PreviousPage();

        ImGui.SameLine();
        ImGui.TextUnformatted($"Page {view.PageNumber} / {view.PageCount}");
        ImGui.SameLine();

        if (SmallButton("Next##page", view.PageNumber < view.PageCount))
            _state.NextPage();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(76f);
        if (ImGui.BeginCombo("##page-size", view.PageSize.ToString()))
        {
            try
            {
                foreach (var size in PageSizes)
                {
                    var selected = size == view.PageSize;
                    if (ImGui.Selectable(size.ToString(), selected))
                        _state.SetPageSize(size);
                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }
            }
            finally
            {
                ImGui.EndCombo();
            }
        }
        ImGui.SameLine();
        ImGui.TextDisabled("per page");
    }

    private static bool SmallButton(string label, bool enabled)
    {
        if (!enabled)
            ImGui.BeginDisabled();
        try
        {
            return ImGui.SmallButton(label) && enabled;
        }
        finally
        {
            if (!enabled)
                ImGui.EndDisabled();
        }
    }

    void IDisposable.Dispose() => _state.Dispose();
}
