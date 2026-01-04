using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GmailSearch
{
    [Key(0)]
    public string body { get; set; } = @"";
    [Key(1)]
    public object date_filter { get; set; }
    [Key(2)]
    public object folder { get; set; }
    [Key(3)]
    public string from_address { get; set; } = @"";
    [Key(4)]
    public string keywords { get; set; } = @"";
    [Key(5)]
    public int max_results { get; set; } = 50;
    [Key(6)]
    public int retry_attempts { get; set; } = 3;
    [Key(7)]
    public double retry_base_delay { get; set; } = 0.5;
    [Key(8)]
    public double retry_factor { get; set; } = 2.0;
    [Key(9)]
    public double retry_jitter { get; set; } = 0.1;
    [Key(10)]
    public double retry_max_delay { get; set; } = 5.0;
    [Key(11)]
    public string subject { get; set; } = @"";
    [Key(12)]
    public string text { get; set; } = @"";
    [Key(13)]
    public string to_address { get; set; } = @"";

    [MessagePackObject]
    public class GmailSearchOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.Email email { get; set; }
        [Key(1)]
        public string message_id { get; set; }
    }

    public GmailSearchOutput Process()
    {
        return new GmailSearchOutput();
    }
}
