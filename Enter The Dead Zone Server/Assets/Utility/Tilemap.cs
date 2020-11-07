using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Tilemaps;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

public class TilemapWrapper : AbstractWorldEntity
{
    private GameObject TilemapObject;

    public TilemapWrapper() { }
    public TilemapWrapper(ulong ID) : base(ID) { }

    protected override void SetEntityType()
    {
        Type = EntityType.Tilemap;
    }

    public override byte[] GetBytes()
    {
        throw new NotImplementedException();
    }
}
