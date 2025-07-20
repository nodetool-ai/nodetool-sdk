using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class Translation
{
    [Key(0)]
    public Nodetool.Types.HFTranslation model { get; set; } = new Nodetool.Types.HFTranslation();
    [Key(1)]
    public string inputs { get; set; } = "";
    [Key(2)]
    public object source_lang { get; set; } = "LanguageCode.ENGLISH";
    [Key(3)]
    public object target_lang { get; set; } = "LanguageCode.FRENCH";

    public string Process()
    {
        return default(string);
    }
}
