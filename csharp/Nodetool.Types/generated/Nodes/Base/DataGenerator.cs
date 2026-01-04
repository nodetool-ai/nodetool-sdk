using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DataGenerator
{
    [Key(0)]
    public Nodetool.Types.Core.RecordType columns { get; set; } = new Nodetool.Types.Core.RecordType();
    [Key(1)]
    public string input_text { get; set; } = @"";
    [Key(2)]
    public int max_tokens { get; set; } = 4096;
    [Key(3)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(4)]
    public string prompt { get; set; } = @"";

    [MessagePackObject]
    public class DataGeneratorOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.DataframeRef dataframe { get; set; }
        [Key(1)]
        public int index { get; set; }
        [Key(2)]
        public object record { get; set; }
    }

    public DataGeneratorOutput Process()
    {
        return new DataGeneratorOutput();
    }
}
