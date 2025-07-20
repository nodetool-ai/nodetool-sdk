using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Extractor
{
    [Key(0)]
    public string system_prompt { get; set; } = "
        You are an expert data extractor. Your task is to extract specific information from text according to a defined schema.
        ";
    [Key(1)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(2)]
    public string text { get; set; } = "";
    [Key(3)]
    public string extraction_prompt { get; set; } = "Extract the following information from the text:";
    [Key(4)]
    public Nodetool.Types.RecordType columns { get; set; } = new Nodetool.Types.RecordType();
    [Key(5)]
    public int max_tokens { get; set; } = 4096;
    [Key(6)]
    public int context_window { get; set; } = 4096;

    public Nodetool.Types.DataframeRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.DataframeRef);
    }
}
