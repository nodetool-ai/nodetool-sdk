using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ListGenerator
{
    [Key(0)]
    public string input_text { get; set; } = @"";
    [Key(1)]
    public int max_tokens { get; set; } = 4096;
    [Key(2)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(3)]
    public string prompt { get; set; } = @"";

    [MessagePackObject]
    public class ListGeneratorOutput
    {
        [Key(0)]
        public int index { get; set; }
        [Key(1)]
        public string item { get; set; }
    }

    public ListGeneratorOutput Process()
    {
        return new ListGeneratorOutput();
    }
}
