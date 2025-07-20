using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetSystemInfo
{

    public object Process()
    {
        return default(object);
    }
}
