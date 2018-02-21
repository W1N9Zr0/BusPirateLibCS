using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace BusPirateLibCS.Modes
{
	public class Spi : Mode
	{

		BusPiratePipe root;

		public Spi(BusPiratePipe root)
		{
			this.root = root;
		}
		
		public void EnterMode()
		{
			if (root.IsInExclusiveMode())
				throw new InvalidOperationException("Already in another mode");
			root.EnterExclusiveMode();
			root.WriteByte(0x01);
			root.ExpectReadText("SPI1");
		}

		public SPISniffer Sniffer()
		{
			return new SPISniffer(this, root);
		}

		public void ExitMode()
		{
			root.WriteByte(0x00);
			root.ExitExclusiveMode();
		}


		private bool cs = false;

		public bool CS
		{
			get
			{
				return cs;
			}
			set
			{
				root.WriteByte((byte)(0x02 | (value ? 1 : 0)));
				root.ExpectReadByte(0x01);
				cs = value;
			}
		}

		public bool Power
		{
			get
			{
				return power;
			}
			set
			{
				power = value;
				configPins();
			}
		}

		public bool Pullups
		{
			get
			{
				return pullups;
			}
			set
			{
				pullups = value;
				configPins();
			}
		}

		public bool AUX
		{
			get {
				return aux;
			}
			set {
				aux = value;
				configPins();
			}
		}


		public void ConfigPins(bool power, bool pullups, bool aux, bool cs)
		{
			this.power = power;
			this.pullups = pullups;
			this.aux = aux;
			this.cs = cs;
			configPins();
		}

		bool power = false;
		bool pullups = false;
		bool aux = false;

		void configPins()
		{
			byte v = 0x40;
			if (power) v |= 0x08;
			if (pullups) v |= 0x04;
			if (aux) v |= 0x02;
			if (cs) v |= 0x01;
			root.WriteByte(v);
			root.ExpectReadByte(0x01);
		}

		public void ConfigProtocol(bool activeOutput = false, bool idle = false, bool edge = true, bool sample = false)
		{
			byte v = 0x80;
			if (activeOutput) v |= 0x08;
			if (idle) v |= 0x04;
			if (edge) v |= 0x02;
			if (sample) v |= 0x01;
			root.WriteByte(v);
			root.ExpectReadByte(0x01);
		}

		public enum PICMode : byte
		{
			PIC614 = 0,
			PIC416,
			PIC424,

		}

		public PICMode PicMode { 
			set {
				root.WriteByte(0xa0);
				root.WriteByte((byte)value);
				root.ExpectReadByte(0x01);
			}

		}

		public byte[] WriteBulk(byte[] data)
		{
			if (data.Length > 16 || data.Length < 1)
				throw new ArgumentOutOfRangeException("data", "Number of bytes must be between 1 and 16");

			byte[] result = new byte[data.Length];

			root.WriteByte((byte)(0x10 | (data.Length - 1)));
			root.ExpectReadByte(0x01);
			for (int x = 0; x < data.Length; x++ )
			{
				root.WriteByte(data[x]);
				result[x] = root.ReadByte();
			}
			return result;
		}

		public enum Speed : int
		{
			s30khz = 0,
			s125khz = 1,
			s250khz = 2,
			s1mhz = 3,
			s2mhz = 4,
			s2_6mhz = 5,
			s4mhz = 6,
			s8mhz = 7
		}

		private Speed speed = Speed.s30khz;
		public Speed SpeedMode
		{
			get { return speed; }
			set
			{
				root.WriteByte((byte)(0x60 | (int)value));
				root.ExpectReadByte(0x01);
				speed = value;
			}
		}


		#region IDisposable Members

		public void Dispose()
		{
			ExitMode();
		}

		#endregion

	}
}
