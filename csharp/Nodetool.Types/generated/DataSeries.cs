using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class DataSeries
{
    [Key(0)]
    public object type { get; set; } = "data_series";
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string x { get; set; } = "";
    [Key(3)]
    public object y { get; set; } = null;
    [Key(4)]
    public object hue { get; set; } = null;
    [Key(5)]
    public object size { get; set; } = null;
    [Key(6)]
    public object style { get; set; } = null;
    [Key(7)]
    public object weight { get; set; } = null;
    [Key(8)]
    public object color { get; set; } = null;
    [Key(9)]
    public object plot_type { get; set; } = "SeabornPlotType.LINE";
    [Key(10)]
    public object estimator { get; set; } = null;
    [Key(11)]
    public object ci { get; set; } = null;
    [Key(12)]
    public int n_boot { get; set; } = 1000;
    [Key(13)]
    public object units { get; set; } = null;
    [Key(14)]
    public object seed { get; set; } = null;
    [Key(15)]
    public object stat { get; set; } = null;
    [Key(16)]
    public object bins { get; set; } = null;
    [Key(17)]
    public object binwidth { get; set; } = null;
    [Key(18)]
    public object binrange { get; set; } = null;
    [Key(19)]
    public object discrete { get; set; } = null;
    [Key(20)]
    public string line_style { get; set; } = "solid";
    [Key(21)]
    public string marker { get; set; } = ".";
    [Key(22)]
    public double alpha { get; set; } = 1.0;
    [Key(23)]
    public object? orient { get; set; } = null;
}
