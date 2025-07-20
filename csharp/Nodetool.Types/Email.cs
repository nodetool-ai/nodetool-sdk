using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class Email
{
    [Key(0)]
    public object type { get; set; } = "email";
    [Key(1)]
    public string id { get; set; } = "";
    [Key(2)]
    public string sender { get; set; } = "";
    [Key(3)]
    public string subject { get; set; } = "";
    [Key(4)]
    public Nodetool.Types.Datetime date { get; set; } = new Nodetool.Types.Datetime();
    [Key(5)]
    public object body { get; set; } = "";
}
