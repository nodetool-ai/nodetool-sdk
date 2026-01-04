using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SendEmail
{
    [Key(0)]
    public string body { get; set; } = @"";
    [Key(1)]
    public string from_address { get; set; } = @"";
    [Key(2)]
    public string password { get; set; } = @"";
    [Key(3)]
    public int retry_attempts { get; set; } = 3;
    [Key(4)]
    public double retry_base_delay { get; set; } = 0.5;
    [Key(5)]
    public double retry_factor { get; set; } = 2.0;
    [Key(6)]
    public double retry_jitter { get; set; } = 0.1;
    [Key(7)]
    public double retry_max_delay { get; set; } = 5.0;
    [Key(8)]
    public int smtp_port { get; set; } = 587;
    [Key(9)]
    public string smtp_server { get; set; } = @"smtp.gmail.com";
    [Key(10)]
    public string subject { get; set; } = @"";
    [Key(11)]
    public string to_address { get; set; } = @"";
    [Key(12)]
    public string username { get; set; } = @"";

    public bool Process()
    {
        return default(bool);
    }
}
