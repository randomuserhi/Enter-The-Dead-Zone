using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Templates
{
    /// <summary>
    /// Contains information on each player and which client they refer to
    /// </summary>
    public class Client
    {
        public List<Player> Players;
    }
    public struct KeySnapshot
    {
        public struct KeyPress
        {
            public enum KeyAction
            {
                UpPress,
                UpRelease,
                DownPress,
                DownRelease,
                LeftPress,
                LeftRelease,
                RightPress,
                RightRelease
            }

            public KeyAction Action;
            public ulong Tick;
        }

        List<KeyPress> Actions;
    }

    public class Player
    {
        public KeySnapshot KeySnapshot;
    }
}
