using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DataStreamer
{
    [Key(0)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public string input_text { get; set; } = "";
    [Key(3)]
    public int max_tokens { get; set; } = 4096;
    [Key(4)]
    public Nodetool.Types.RecordType columns { get; set; } = new Nodetool.Types.RecordType();

    public object Process()
    {
        return default(object);
    }
}
