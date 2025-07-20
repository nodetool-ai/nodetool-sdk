using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class LanguageModel
{
    [Key(0)]
    public object type { get; set; } = "language_model";
    [Key(1)]
    public object provider { get; set; } = "Provider.Empty";
    [Key(2)]
    public string id { get; set; } = "";
    [Key(3)]
    public string name { get; set; } = "";
}
