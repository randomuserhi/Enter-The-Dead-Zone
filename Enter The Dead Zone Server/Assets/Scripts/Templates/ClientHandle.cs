using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine;
using DeadZoneEngine.Entities;

namespace ClientHandle
{
    public class ClientID
    {
        private class EndPointComparer : IEqualityComparer<EndPoint>
        {
            public bool Equals(EndPoint A, EndPoint B)
            {
                return A.Equals(B);
            }

            public int GetHashCode(EndPoint A)
            {
                return A.GetHashCode();
            }
        }

        public static Dictionary<ushort, Client> ConnectedClients = new Dictionary<ushort, Client>();
        public static Dictionary<EndPoint, ushort> EndPointToID = new Dictionary<EndPoint, ushort>(new EndPointComparer());
        private static ushort StaticID = 0;
        public Client Self { get; private set; }
        public EndPoint EndPoint { get; private set; }
        private ushort Value;
        public ushort ID
        {
            get
            {
                return Value;
            }
            private set
            {
                Value = value;
                if (EndPoint != null)
                    if (EndPointToID.ContainsKey(EndPoint))
                        EndPointToID[EndPoint] = Value;
                    else
                        EndPointToID.Add(EndPoint, Value);
            }
        }

        public ClientID(Client Self, EndPoint EndPoint)
        {
            this.Self = Self;
            this.EndPoint = EndPoint;
            AssignNewID();
        }

        public static Client GetClient(EndPoint EndPoint)
        {
            if (EndPoint == null)
                return null;
            if (EndPointToID.ContainsKey(EndPoint))
                return ConnectedClients[EndPointToID[EndPoint]];
            return null;
        }

        public static Client GetClient(ushort ID)
        {
            if (ConnectedClients.ContainsKey(ID))
                return ConnectedClients[ID];
            return null;
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
            ID = Next;
            ConnectedClients.Add(ID, Self);
        }

        public void ChangeID()
        {
            Remove(ID);
            AssignNewID();
        }

        public void ChangeID(ushort New, bool Replace = false)
        {
            if (ConnectedClients.ContainsKey(New))
            {
                if (ConnectedClients[New] != Self)
                {
                    if (Replace)
                        ConnectedClients[New] = Self;
                    else
                        Debug.LogError("Could not change ClientID as an object at that ClientID already exists...");
                }
            }
            else
            {
                ConnectedClients.Add(New, Self);
                Remove(ID);
            }
            ID = New;
        }

        public static implicit operator ushort(ClientID ID)
        {
            return ID.ID;
        }

        public static void Remove(ushort ID)
        {
            if (ConnectedClients.ContainsKey(ID))
            {
                if (ConnectedClients[ID].EndPoint != null)
                    EndPointToID.Remove(ConnectedClients[ID].EndPoint);
                ConnectedClients.Remove(ID);
            }
            else
                Debug.LogError("ClientID.Remove(ClientID ID) => ID " + ID + " does not exist!");
        }

        public static void Remove(EndPoint EndPoint)
        {
            if (EndPointToID.ContainsKey(EndPoint))
            {
                Remove(EndPointToID[EndPoint]);
            }
            else
                Debug.LogError("ClientID.Remove(EndPoint EndPoint) => EndPoint does not exist!");
        }
    }

    /// <summary>
    /// Contains information on each player and which client they refer to
    /// </summary>
    public class Client
    {
        public static int MaxNumPlayers = 8;
        public const int TicksToTimeout = 60;

        public ClientID ID;

        public EndPoint EndPoint;
        public bool Setup = false;
        public Player[] Players;
        public byte NumPlayers { get; private set; }

        public bool LostConnection = false;
        public int TicksSinceConnectionLoss = 0;

        private Client(EndPoint EndPoint = null)
        {
            this.EndPoint = EndPoint;
            ID = new ClientID(this, EndPoint);
            Players = new Player[MaxNumPlayers];
        }

        public static Client GetClient(EndPoint EndPoint = null)
        {
            Client Client = ClientID.GetClient(EndPoint);
            if (Client == null)
            {
                Client = new Client(EndPoint);
            }
            return Client;
        }

        public void AddPlayer()
        {
            for (byte i = 0; i < Players.Length; i++)
            {
                if (Players[i] == null)
                {
                    Players[i] = new Player(i);
                    NumPlayers++;
                    return;
                }
            }
            Debug.LogError("Max number of players reached");
        }

        public void RemovePlayer(int PlayerID)
        {
            if (Players[PlayerID] == null)
            {
                Debug.LogError("Player does not exist");
                return;
            }

            Players[PlayerID].Destroy();
            Players[PlayerID] = null;
            NumPlayers--;
        }

        public void Destroy()
        {
            ClientID.Remove(EndPoint);
            for (int i = 0; i < Players.Length; i++)
                if (Players[i] != null)
                    Players[i].Destroy();
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
        public byte ID { get; private set; }

        public KeySnapshot KeySnapshot;
        public PlayerController Controller;

        public PlayerCreature Entity;

        public Player(byte ID)
        {
            Entity = new PlayerCreature();
            this.ID = ID;
        }

        public void Destroy()
        {
            DZEngine.Destroy(Entity);
        }

        public byte[] GetBytes()
        {
            List<byte> Data = new List<byte>();
            Data.Add(ID);
            Data.AddRange(BitConverter.GetBytes(Entity.ID));
            return Data.ToArray();
        }
    }
}
