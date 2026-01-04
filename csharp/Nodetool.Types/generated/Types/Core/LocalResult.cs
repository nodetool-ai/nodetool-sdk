using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class LocalResult
{
    [Key(0)]
    public object address { get; set; } = null;
    [Key(1)]
    public object data_cid { get; set; } = null;
    [Key(2)]
    public object data_id { get; set; } = null;
    [Key(3)]
    public object description { get; set; } = null;
    [Key(4)]
    public object gps_coordinates { get; set; } = null;
    [Key(5)]
    public object hours { get; set; } = null;
    [Key(6)]
    public object open_state { get; set; } = null;
    [Key(7)]
    public object operating_hours { get; set; } = null;
    [Key(8)]
    public object phone { get; set; } = null;
    [Key(9)]
    public object photos_link { get; set; } = null;
    [Key(10)]
    public object place_id { get; set; } = null;
    [Key(11)]
    public object place_id_search { get; set; } = null;
    [Key(12)]
    public int position { get; set; }
    [Key(13)]
    public object price { get; set; } = null;
    [Key(14)]
    public object provider_id { get; set; } = null;
    [Key(15)]
    public object rating { get; set; } = null;
    [Key(16)]
    public object reviews { get; set; } = null;
    [Key(17)]
    public object reviews_link { get; set; } = null;
    [Key(18)]
    public object thumbnail { get; set; } = null;
    [Key(19)]
    public object title { get; set; } = null;
    [Key(20)]
    public object type { get; set; } = @"local_result";
    [Key(21)]
    public object types { get; set; } = null;
    [Key(22)]
    public object website { get; set; } = null;
}
