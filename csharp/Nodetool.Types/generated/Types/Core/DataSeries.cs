using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class DataSeries
{
    [Key(0)]
    public double alpha { get; set; } = 1.0;
    [Key(1)]
    public object binrange { get; set; } = null;
    [Key(2)]
    public object bins { get; set; } = null;
    [Key(3)]
    public object binwidth { get; set; } = null;
    [Key(4)]
    public object ci { get; set; } = null;
    [Key(5)]
    public object color { get; set; } = null;
    [Key(6)]
    public object discrete { get; set; } = null;
    [Key(7)]
    public object estimator { get; set; } = null;
    [Key(8)]
    public object hue { get; set; } = null;
    [Key(9)]
    public string line_style { get; set; } = @"solid";
    [Key(10)]
    public string marker { get; set; } = @".";
    [Key(11)]
    public int n_boot { get; set; } = 1000;
    [Key(12)]
    public string name { get; set; } = @"";
    [Key(13)]
    public object? orient { get; set; } = null;
    [Key(14)]
    public object plot_type { get; set; } = @"line";
    [Key(15)]
    public object seed { get; set; } = null;
    [Key(16)]
    public object size { get; set; } = null;
    [Key(17)]
    public object stat { get; set; } = null;
    [Key(18)]
    public object style { get; set; } = null;
    [Key(19)]
    public object type { get; set; } = @"data_series";
    [Key(20)]
    public object units { get; set; } = null;
    [Key(21)]
    public object weight { get; set; } = null;
    [Key(22)]
    public string x { get; set; } = @"";
    [Key(23)]
    public object y { get; set; } = null;
}
