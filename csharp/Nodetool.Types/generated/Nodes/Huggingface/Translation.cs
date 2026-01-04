using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class Translation
{
    [Key(0)]
    public string inputs { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.HFTranslation model { get; set; } = new Nodetool.Types.Core.HFTranslation();
    [Key(2)]
    public object source_lang { get; set; } = @"en";
    [Key(3)]
    public object target_lang { get; set; } = @"fr";
}
