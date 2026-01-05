using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class JobResult
{
    [Key(0)]
    public object company_name { get; set; } = null;
    [Key(1)]
    public object extensions { get; set; } = null;
    [Key(2)]
    public object location { get; set; } = null;
    [Key(3)]
    public object share_link { get; set; } = null;
    [Key(4)]
    public object thumbnail { get; set; } = null;
    [Key(5)]
    public object title { get; set; } = null;
    [Key(6)]
    public object type { get; set; } = @"job_result";
    [Key(7)]
    public object via { get; set; } = null;
}
