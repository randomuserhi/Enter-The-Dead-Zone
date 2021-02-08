using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.InputSystem;
using UnityEngine;

using DeadZoneEngine.Controllers;
using ClientHandle;
using static DeadZoneEngine.Controllers.InputMapping;
using DZNetwork;

public class PlayerController : Controller
{
    public Player Owner;
    public PlayerCreature.Control PlayerControl;

    public PlayerController(Player Owner, PlayerCreature.Control PlayerControl) : base()
    {
        this.Owner = Owner;
        this.PlayerControl = PlayerControl;
    }

    protected override void SetType()
    {
        Type = ControllerType.PlayerController;
    }

    public override void ParseBytes(Packet Data)
    {
        PlayerControl.InputID = Data.ReadULong();
        PlayerControl.MovementDirection = new Vector2(Data.ReadFloat(), Data.ReadFloat());
    }
    public override byte[] GetBytes()
    {
        List<byte> Data = new List<byte>();
        Data.Add(Owner.ID);
        Data.AddRange(BitConverter.GetBytes(PlayerControl.InputID));
        Data.AddRange(BitConverter.GetBytes(PlayerControl.MovementDirection.x));
        Data.AddRange(BitConverter.GetBytes(PlayerControl.MovementDirection.y));
        return Data.ToArray();
    }
}

public static class InputManager
{
    public static void Initialize()
    {
    }
}
