using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class IMAPConnection
{
    [Key(0)]
    public object type { get; set; } = "imap_connection";
    [Key(1)]
    public string host { get; set; } = "";
    [Key(2)]
    public int port { get; set; } = 993;
    [Key(3)]
    public string username { get; set; } = "";
    [Key(4)]
    public string password { get; set; } = "";
    [Key(5)]
    public bool use_ssl { get; set; } = true;
}
