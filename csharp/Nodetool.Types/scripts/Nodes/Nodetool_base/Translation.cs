using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Translation
{
    [Key(0)]
    public Nodetool.Types.InferenceProviderTranslationModel model { get; set; } = new Nodetool.Types.InferenceProviderTranslationModel();
    [Key(1)]
    public string text { get; set; } = "";
    [Key(2)]
    public string src_lang { get; set; } = "";
    [Key(3)]
    public string tgt_lang { get; set; } = "";
    [Key(4)]
    public bool clean_up_tokenization_spaces { get; set; } = true;
    [Key(5)]
    public object truncation { get; set; } = "Truncation.do_not_truncate";

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
