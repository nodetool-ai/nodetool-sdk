using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class ChartConfig
{
    [Key(0)]
    public object type { get; set; } = "chart_config";
    [Key(1)]
    public string title { get; set; } = "";
    [Key(2)]
    public string x_label { get; set; } = "";
    [Key(3)]
    public string y_label { get; set; } = "";
    [Key(4)]
    public bool legend { get; set; } = true;
    [Key(5)]
    public Nodetool.Types.ChartData data { get; set; } = new Nodetool.Types.ChartData();
    [Key(6)]
    public object height { get; set; } = null;
    [Key(7)]
    public object aspect { get; set; } = null;
    [Key(8)]
    public object x_lim { get; set; } = null;
    [Key(9)]
    public object y_lim { get; set; } = null;
    [Key(10)]
    public object? x_scale { get; set; } = null;
    [Key(11)]
    public object? y_scale { get; set; } = null;
    [Key(12)]
    public object legend_position { get; set; } = "auto";
    [Key(13)]
    public object palette { get; set; } = null;
    [Key(14)]
    public object hue_order { get; set; } = null;
    [Key(15)]
    public object hue_norm { get; set; } = null;
    [Key(16)]
    public object sizes { get; set; } = null;
    [Key(17)]
    public object size_order { get; set; } = null;
    [Key(18)]
    public object size_norm { get; set; } = null;
    [Key(19)]
    public object marginal_kws { get; set; } = null;
    [Key(20)]
    public object joint_kws { get; set; } = null;
    [Key(21)]
    public object? diag_kind { get; set; } = null;
    [Key(22)]
    public bool corner { get; set; } = false;
    [Key(23)]
    public object center { get; set; } = null;
    [Key(24)]
    public object vmin { get; set; } = null;
    [Key(25)]
    public object vmax { get; set; } = null;
    [Key(26)]
    public object cmap { get; set; } = null;
    [Key(27)]
    public bool annot { get; set; } = false;
    [Key(28)]
    public string fmt { get; set; } = ".2g";
    [Key(29)]
    public bool square { get; set; } = false;
}
