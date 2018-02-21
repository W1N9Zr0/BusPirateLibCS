using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace BusPirateLibCS.Modes
{
	public class SPISniffer : Mode, IDisposable
	{
		private BusPiratePipe root;
		private Spi spi;

		public SPISniffer(Spi spi, BusPiratePipe root)
		{
			this.spi = spi;
			this.root = root;
			EnterMode();
		}

		public void Dispose()
		{
			ExitMode();
		}

		public void EnterMode()
		{
			root.WriteByte(0x0e);
		}

		public void ExitMode()
		{
			root.WriteByte(0x00);
		}

		public IEnumerable<Transaction> readTransaction()
		{
			var port = ((BusPirate)root).port;
			var buffer = new byte[0xa000];
			var res = new Transaction();

			var state = -1;
			while (true)
			{
				var len = port.Read(buffer, 0, buffer.Length);
				for (int pos = 0; pos < len; pos++)
				{
					var v = buffer[pos];
					switch (state)
					{
						case -1:
							if (v == 0x5b)
								state = 1;
							break;
						case 0: // before CS
							if (v == 0x5b) // CS triggered
								state = 1;
							else
								throw new ArgumentException();
							break;

						case 1: // inside CS
							if (v == 0x5d) // CS off without data
								state = 10;
							else if (v == 0x5c) // start of data pair
								state = 2;
							else
								throw new ArgumentException();
							break;

						case 2: // data MOSI
							res.mosi.Add(v);
							state = 3;
							break;

						case 3: // data MISO
							res.miso.Add(v);
							state = 1;
							break;
					}
					if (state == 10)
					{
						yield return res;
						res = new Transaction();
						state = 0;
					}
				}
			}
		}
		

		
		public class Transaction
		{
			public List<byte> miso = new List<byte>();
			public List<byte> mosi = new List<byte>();
		}
	}
}