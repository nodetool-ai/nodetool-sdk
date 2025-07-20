using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class EmailSearchCriteria
{
    [Key(0)]
    public object type { get; set; } = "email_search_criteria";
    [Key(1)]
    public string? from_address { get; set; } = null;
    [Key(2)]
    public string? to_address { get; set; } = null;
    [Key(3)]
    public string? subject { get; set; } = null;
    [Key(4)]
    public string? body { get; set; } = null;
    [Key(5)]
    public string? cc { get; set; } = null;
    [Key(6)]
    public string? bcc { get; set; } = null;
    [Key(7)]
    public Nodetool.Types.DateSearchCondition? date_condition { get; set; } = null;
    [Key(8)]
    public List<object> flags { get; set; } = new List<object>();
    [Key(9)]
    public List<string> keywords { get; set; } = new List<object>();
    [Key(10)]
    public string? folder { get; set; } = null;
    [Key(11)]
    public string? text { get; set; } = null;
}
