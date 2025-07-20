using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RandomBool
{

    public bool Process()
    {
        return default(bool);
    }
}
