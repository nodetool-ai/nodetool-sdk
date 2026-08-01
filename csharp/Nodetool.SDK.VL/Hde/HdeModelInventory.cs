using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Models;

namespace Nodetool.SDK.VL.Hde;

internal enum HdeModelFamily
{
    All,
    Language,
    Image,
    Audio,
    Video,
    ThreeD,
    Other
}
internal static class HdeModelFamilyClassifier
{
    public static HdeModelFamily Classify(string compatibility)
    {
        var value = compatibility.ToLowerInvariant();
        if (ContainsAny(value, "video"))
            return HdeModelFamily.Video;
        if (ContainsAny(value, "3d", "mesh", "point_cloud", "pointcloud"))
            return HdeModelFamily.ThreeD;
        if (ContainsAny(value, "audio", "speech", "asr", "tts", "music"))
            return HdeModelFamily.Audio;
        if (ContainsAny(
                value,
                "image",
                "flux",
                "stable_diffusion",
                "controlnet",
                "inpainting",
                "lora",
                "ip_adapter",
                "real_esrgan",
                "depth",
                "object_detection",
                "segmentation"))
        {
            return HdeModelFamily.Image;
        }
        if (ContainsAny(
                value,
                "text",
                "language",
                "embedding",
                "reranker",
                "sentence",
                "fill_mask",
                "question_answering",
                "t5",
                "llama"))
        {
            return HdeModelFamily.Language;
        }
        return HdeModelFamily.Other;
    }

    public static string Label(HdeModelFamily family)
        => family == HdeModelFamily.ThreeD ? "3D" : family.ToString();

    private static bool ContainsAny(string value, params string[] parts)
        => parts.Any(value.Contains);
}

internal sealed record HdeModelRow(
    string Key,
    string DisplayName,
    string Details,
    string Status,
    string ActionLabel,
    float Progress,
    string ProgressText,
    bool CanAct,
    bool IsDownloading,
    bool Recommended);

internal sealed record HdeModelPageSnapshot(
    string Target,
    string Notice,
    string Family,
    string Search,
    string Summary,
    IReadOnlyList<HdeModelRow> Rows,
    int PageNumber,
    int PageCount,
    int PageSize,
    int TotalCount,
    int RangeStart,
    int RangeEnd)
{
    public static HdeModelPageSnapshot Empty { get; } = new(
        "Target: resolving...",
        "Loading model catalog...",
        "Image",
        "",
        "Image · 0 models",
        Array.Empty<HdeModelRow>(),
        1,
        1,
        100,
        0,
        0,
        0);
}

internal static class HdeModelListProjector
{
    public static HdeModelPageSnapshot Project(
        ModelCatalogSnapshot catalog,
        ModelDownloadSnapshot downloads,
        HdeModelFamily family,
        string? search,
        int pageIndex,
        int pageSize,
        IReadOnlySet<string>? actingKeys = null,
        string target = "Target: resolving...",
        string notice = "")
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(downloads);
        pageSize = Math.Clamp(pageSize, 1, 200);
        search = search?.Trim() ?? "";

        var filtered = catalog.Models
            .Where(model => family == HdeModelFamily.All ||
                            HdeModelFamilyClassifier.Classify(model.Compatibility) == family)
            .Where(model => MatchesSearch(model, search))
            .OrderBy(ModelRank)
            .ThenBy(model => model.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(model => model.Key, StringComparer.Ordinal)
            .ToArray();
        var pageCount = Math.Max(1, (filtered.Length + pageSize - 1) / pageSize);
        pageIndex = Math.Clamp(pageIndex, 0, pageCount - 1);
        var pageModels = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToArray();
        var rows = pageModels
            .Select(model => CreateRow(model, FindDownload(model, downloads), actingKeys))
            .ToArray();
        var ready = filtered.Count(model => model.IsReady);
        var downloadable = filtered.Count(model =>
            model.Availability == SdkModelAvailability.Downloadable);
        var familyLabel = HdeModelFamilyClassifier.Label(family);
        var summary = $"{familyLabel} · {filtered.Length} models · {ready} ready · {downloadable} downloadable";
        var rangeStart = filtered.Length == 0 ? 0 : pageIndex * pageSize + 1;
        var rangeEnd = filtered.Length == 0
            ? 0
            : Math.Min(filtered.Length, rangeStart + rows.Length - 1);

        return new HdeModelPageSnapshot(
            target,
            notice,
            familyLabel,
            search,
            summary,
            rows,
            pageIndex + 1,
            pageCount,
            pageSize,
            filtered.Length,
            rangeStart,
            rangeEnd);
    }

    internal static ModelDownloadState? FindDownload(
        ModelDescriptor model,
        ModelDownloadSnapshot downloads)
        => downloads.Downloads
            .Where(download => DownloadMatches(model, download))
            .OrderByDescending(download => download.UpdatedAt)
            .FirstOrDefault();

    private static HdeModelRow CreateRow(
        ModelDescriptor model,
        ModelDownloadState? download,
        IReadOnlySet<string>? actingKeys)
    {
        var acting = actingKeys?.Contains(model.Key) == true;
        var source = model.Provider ?? model.RepositoryId ?? model.Id;
        var details = string.IsNullOrWhiteSpace(source)
            ? model.Compatibility
            : $"{source} · {model.Compatibility}";

        if (acting)
        {
            return new HdeModelRow(
                model.Key,
                model.DisplayName,
                details,
                "Working...",
                "Working...",
                0f,
                "",
                false,
                false,
                model.Recommended);
        }
        if (download is { IsTerminal: false })
        {
            var progress = (float)(download.Progress ?? 0d);
            return new HdeModelRow(
                model.Key,
                model.DisplayName,
                details,
                download.Status,
                "Cancel",
                progress,
                FormatProgress(download),
                true,
                true,
                model.Recommended);
        }
        if (download is
            { Status: SdkModelDownloadStatuses.Error or SdkModelDownloadStatuses.Cancelled })
        {
            return new HdeModelRow(
                model.Key,
                model.DisplayName,
                details,
                download.Status,
                "Retry",
                (float)(download.Progress ?? 0d),
                string.IsNullOrWhiteSpace(download.Error)
                    ? download.Status
                    : download.Error,
                true,
                false,
                model.Recommended);
        }
        if (download is { Status: SdkModelDownloadStatuses.Completed } && !model.IsReady)
        {
            return new HdeModelRow(
                model.Key,
                model.DisplayName,
                details,
                "Finalizing",
                "Finalizing",
                1f,
                "Refreshing catalog...",
                false,
                false,
                model.Recommended);
        }
        if (model.IsReady)
        {
            return new HdeModelRow(
                model.Key,
                model.DisplayName,
                details,
                "Ready",
                "Ready",
                1f,
                "",
                false,
                false,
                model.Recommended);
        }
        if (model.Availability == SdkModelAvailability.Downloadable &&
            !string.IsNullOrWhiteSpace(model.RepositoryId))
        {
            return new HdeModelRow(
                model.Key,
                model.DisplayName,
                details,
                "Downloadable",
                "Download",
                0f,
                "",
                true,
                false,
                model.Recommended);
        }
        return new HdeModelRow(
            model.Key,
            model.DisplayName,
            details,
            model.Availability,
            "Unavailable",
            0f,
            "",
            false,
            false,
            model.Recommended);
    }

    private static bool MatchesSearch(ModelDescriptor model, string search)
    {
        if (search.Length == 0) return true;
        return Contains(model.DisplayName, search) ||
               Contains(model.Provider, search) ||
               Contains(model.RepositoryId, search) ||
               Contains(model.Compatibility, search) ||
               Contains(model.Id, search) ||
               model.SupportedTasks.Any(task => Contains(task, search));
    }

    private static bool Contains(string? value, string search)
        => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    private static int ModelRank(ModelDescriptor model)
        => model switch
        {
            { Recommended: true, Availability: SdkModelAvailability.Downloadable } => 0,
            { Recommended: true, IsReady: true } => 1,
            { IsReady: true } => 2,
            { Availability: SdkModelAvailability.Downloadable } => 3,
            _ => 4
        };

    private static bool DownloadMatches(
        ModelDescriptor model,
        ModelDownloadState download)
        => !string.IsNullOrWhiteSpace(model.RepositoryId) &&
           string.Equals(model.RepositoryId, download.RepositoryId, StringComparison.Ordinal) &&
           string.Equals(model.Compatibility, download.ModelType, StringComparison.Ordinal) &&
           string.Equals(model.Path ?? "", download.Path ?? "", StringComparison.Ordinal);

    private static string FormatProgress(ModelDownloadState download)
    {
        var progress = download.Progress.HasValue
            ? $"{download.Progress.Value:P0} · {FormatBytes(download.DownloadedBytes)} / {FormatBytes(download.TotalBytes)}"
            : download.Status;
        return string.IsNullOrWhiteSpace(download.Error)
            ? progress
            : $"{progress} · {download.Error}";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024d && unit < units.Length - 1)
        {
            value /= 1024d;
            unit++;
        }
        return $"{value:0.#} {units[unit]}";
    }
}
