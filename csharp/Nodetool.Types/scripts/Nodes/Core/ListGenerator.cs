using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class ListGenerator
{
    [Key(0)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public string input_text { get; set; } = "";
    [Key(3)]
    public int max_tokens { get; set; } = 4096;

    [MessagePackObject]
    public class ListGeneratorOutput
    {
        [Key(0)]
        public string item { get; set; }
        [Key(1)]
        public int index { get; set; }
    }

    public ListGeneratorOutput Process()
    {
        // Implementation would be generated based on node logic
        return new ListGeneratorOutput();
    }
}
