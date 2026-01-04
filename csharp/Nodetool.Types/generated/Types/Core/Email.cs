using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Email
{
    [Key(0)]
    public object body { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.Datetime date { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(2)]
    public string id { get; set; } = @"";
    [Key(3)]
    public string sender { get; set; } = @"";
    [Key(4)]
    public string subject { get; set; } = @"";
    [Key(5)]
    public object type { get; set; } = @"email";
}
