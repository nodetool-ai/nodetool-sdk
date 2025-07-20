using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class AutomaticSpeechRecognition
{
    [Key(0)]
    public Nodetool.Types.InferenceProviderAutomaticSpeechRecognitionModel model { get; set; } = new Nodetool.Types.InferenceProviderAutomaticSpeechRecognitionModel();
    [Key(1)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();

    [MessagePackObject]
    public class AutomaticSpeechRecognitionOutput
    {
        [Key(0)]
        public string text { get; set; }
        [Key(1)]
        public object chunks { get; set; }
    }

    public AutomaticSpeechRecognitionOutput Process()
    {
        // Implementation would be generated based on node logic
        return new AutomaticSpeechRecognitionOutput();
    }
}
