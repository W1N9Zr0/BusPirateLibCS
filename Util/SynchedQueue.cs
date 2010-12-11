using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusPirateLibCS.Util
{
	class SynchedQueue<T>
	{

		System.Collections.Queue inner = System.Collections.Queue.Synchronized(new System.Collections.Queue());
		public void Enqueue(T v) {
			inner.Enqueue(v);
		}

		public int Count
		{
			get
			{
				return inner.Count;
			}
		}

		public T Dequeue()
		{
			return (T)inner.Dequeue();
		}

		public void Clear()
		{
			inner.Clear();
		}

		public T Peek()
		{
			return (T)inner.Peek();
		}

	}
}
