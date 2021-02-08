using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.InputSystem;

using DeadZoneEngine;
using DZNetwork;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Controllers;

namespace ClientHandle
{
    public class ClientID
    {
        public static Dictionary<ushort, Client> ConnectedClients = new Dictionary<ushort, Client>();
        public static Dictionary<IPEndPoint, ushort> EndPointToID = new Dictionary<IPEndPoint, ushort>(new IPEndPointComparer());
        private static ushort StaticID = 0;
        public Client Self { get; private set; }
        public IPEndPoint EndPoint { get; private set; }
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

        public ClientID(Client Self, IPEndPoint EndPoint)
        {
            this.Self = Self;
            this.EndPoint = EndPoint;
            AssignNewID();
        }

        public static Client GetClient(IPEndPoint EndPoint)
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

        public static void Remove(IPEndPoint EndPoint)
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

        public IPEndPoint EndPoint;
        public Player[] Players;
        public byte NumPlayers { get; private set; }

        public bool LostConnection = false;
        public int TicksSinceConnectionLoss = 0;

        private Client(IPEndPoint EndPoint = null)
        {
            this.EndPoint = EndPoint;
            ID = new ClientID(this, EndPoint);
            Players = new Player[MaxNumPlayers];
        }

        public static Client GetClient(IPEndPoint EndPoint = null)
        {
            Client Client = ClientID.GetClient(EndPoint);
            if (Client == null)
            {
                Client = new Client(EndPoint);
            }
            return Client;
        }

        public Player AddPlayer()
        {
            for (byte i = 0; i < Players.Length; i++)
            {
                if (Players[i] == null)
                {
                    Players[i] = new Player(i);
                    NumPlayers++;
                    return Players[i];
                }
            }
            Debug.LogError("Max number of players reached");
            return null;
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

        public void RemoveAllPlayers()
        {
            for (int i = 0; i < Players.Length; i++)
            {
                if (Players[i] != null)
                {
                    Players[i].Destroy();
                    Players[i] = null;
                }
            }
            NumPlayers = 0;
        }

        public void Destroy()
        {
            ClientID.Remove(EndPoint);
            for (int i = 0; i < Players.Length; i++)
                if (Players[i] != null)
                    Players[i].Destroy();
        }
    }

    public class Player
    {
        public byte ID { get; private set; }

        private PlayerController _Controller;
        public PlayerController Controller
        {
            get
            {
                return _Controller;
            }
            set
            {
                _Controller = value;
                _Controller.Owner = this;
                if (Entity != null)
                {
                    _Controller.PlayerControl = Entity.Controller;
                    Entity.Controller.Owner = _Controller;
                }
            }
        }

        public PlayerCreature Entity;

        public Player(byte ID)
        {
            Entity = new PlayerCreature();
            Entity.ProtectedDeletion = true;
            _Controller = new PlayerController(this, Entity.Controller);
            if (Main.GameStarted)
            {
                Vector2 Direction = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
                Entity.Position = Direction * UnityEngine.Random.Range(15f, 25f);
            }
            this.ID = ID;
        }

        public void Destroy()
        {
            DZEngine.Destroy(Entity);
            if (_Controller != null)
                _Controller.Disable();
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
