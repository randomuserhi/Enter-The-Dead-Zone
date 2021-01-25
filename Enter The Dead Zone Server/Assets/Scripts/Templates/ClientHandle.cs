using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace ClientHandle
{
    /// <summary>
    /// Contains information on each player and which client they refer to
    /// </summary>
    public class Client
    {
        public static Dictionary<ushort, Client> ConnectedClients = new Dictionary<ushort, Client>();
        public static Dictionary<EndPoint, ushort> EndPointToID = new Dictionary<EndPoint, ushort>();
        private static ushort StaticID = 0;

        private ushort _ClientID;
        public ushort ClientID
        {
            get
            {
                return _ClientID;
            }
            private set
            {
                _ClientID = value;
                if (EndPoint != null)
                    if (EndPointToID.ContainsKey(EndPoint))
                        EndPointToID[EndPoint] = _ClientID;
                    else
                        EndPointToID.Add(EndPoint, _ClientID);
            }
        }
        public EndPoint EndPoint;
        public bool PlayerSetup = false;
        public List<Player> Players;

        public Client(EndPoint EndPoint = null)
        {
            this.EndPoint = EndPoint;
            AssignNewID();
        }

        private void AssignNewID()
        {
            ushort Next = StaticID++;
            if (ConnectedClients.Count >= ushort.MaxValue - 100)
            {
                Debug.LogError("No more IDs to give!");
                return;
            }
            while (ConnectedClients.ContainsKey(Next))
            {
                Next = StaticID++;
            }
            ClientID = Next;
            ConnectedClients.Add(ClientID, this);
        }

        public void ChangeID()
        {
            Remove(ClientID);
            AssignNewID();
        }

        public void ChangeID(ushort New, bool Replace = false)
        {
            if (ConnectedClients.ContainsKey(New))
            {
                if (Replace && ConnectedClients[New] != this)
                {
                    ConnectedClients[New] = this;
                }
                else
                {
                    Debug.LogError("Could not change ClientID as an object at that ClientID already exists...");
                }
            }
            else
            {
                ConnectedClients.Add(New, this);
                Remove(ClientID);
            }
            ClientID = New;
        }

        public static void Remove(ushort ID)
        {
            if (ConnectedClients.ContainsKey(ID))
            {
                EndPointToID.Remove(ConnectedClients[ID].EndPoint);
                ConnectedClients.Remove(ID);
            }
            else
                Debug.LogError("ClientID.Remove(ClientID ID) => ID " + ID + " does not exist!");
        }
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

    public class PlayerController
    {
        public Vector2 Direction;
    }

    public class Player
    {
        public KeySnapshot KeySnapshot;
        public PlayerController Controller;

        public PlayerCreature Entity;

        public Player()
        {
            Entity = new PlayerCreature();
        }
    }
}
