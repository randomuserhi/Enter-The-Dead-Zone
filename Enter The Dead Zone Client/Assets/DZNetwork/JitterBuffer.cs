using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DZNetwork
{
    public class JitterBuffer<T> where T : class
    {
        public class Key
        {
            public T Value;
            public Key Next;

            public Key(T Value)
            {
                this.Value = Value;
            }
        }

        private int _Count = 0;
        public int Count
        {
            get
            {
                return _Count;
            }
        }

        private Key Start = null;
        private Key End = null;
        public T First
        {
            get
            {
                if (Start != null)
                    return Start.Value;
                return default;
            }
        }
        public T Last
        {
            get
            {
                if (End != null)
                    return End.Value;
                return default;
            }
        }

        public Key FirstKey
        {
            get
            {
                return Start;
            }
        }
        public Key LastKey
        {
            get
            {
                return End;
            }
        }

        public void Add(T Value)
        {
            _Count++;
            if (Start == null)
            {
                Start = new Key(Value);
                End = Start;
                return;
            }
            End.Next = new Key(Value);
            End = End.Next;
        }

        public void Clear()
        {
            Start = null;
            End = null;
            _Count = 0;
        }

        public void Dequeue(int Index)
        {
            for (int i = 0; i < Index; i++)
            {
                Start = Start.Next;
                _Count--;
            }
        }

        public void Dequeue(T From)
        {
            while (!EqualityComparer<T>.Default.Equals(Start.Value, From))
            {
                if (Start.Next == null) return;
                Start = Start.Next;
                _Count--;
            }
        }

        public void Iterate(Action<Key> IterateOperation, Func<Key, bool> BreakCondition = null)
        {
            Key Current = Start;
            for (int i = 0; i < _Count; i++)
            {
                IterateOperation(Current);
                if (BreakCondition != null && BreakCondition(Current)) return;
                Current = Current.Next;
            }
        }
    }
}
