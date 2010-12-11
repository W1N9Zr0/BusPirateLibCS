using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace BusPirateLibCS.Modes
{
	public class UART : Mode
	{

		BusPiratePipe root;

		public UART(BusPiratePipe root)
		{
			this.root = root;
		}
		
		public void EnterMode()
		{
			if (root.IsInExclusiveMode())
				throw new InvalidOperationException("Already in another mode");
			root.EnterExclusiveMode();
			root.WriteByte(0x03);
			root.ExpectReadText("ARTx");
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
				
				cs = value;
				configPins();
			}
		}


		public void WriteBulk(byte[] data) {
			if (data.Length > 16 || data.Length < 1)
				throw new ArgumentOutOfRangeException("data", "Number of bytes must be between 1 and 16");

			root.WriteByte((byte)(0x10 | (data.Length - 1)));
			root.ExpectReadByte(0x01);
			foreach (var b in data)
			{
				root.WriteByte(b);
				root.ExpectReadByte(0x01);
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

		bool readEnabled = false;

		public bool ReadWaiting
		{
			get
			{
				return root.ReadWaiting;
			}
		}

		public bool ReadEnabled { 
			get {
				return readEnabled;
			}
			set
			{
				root.WriteByte((byte)(0x02 | (value ? 0 : 1)));
				root.ExpectReadByte(0x01);
				readEnabled = value;
			}
		}

		public enum Parity
		{
			None = 0,
			Even = 1,
			Odd = 2,
			NineBit = 3
		}

		public enum StopBits
		{
			One = 0,
			Two = 1
		}

		public enum RXIdle {
			High = 0,
			Low = 1
		}

		public void ConfigProtocol(bool activeOutput = false, Parity parity= Parity.None, StopBits stopbits = StopBits.One, RXIdle rxIdle = RXIdle.High)
		{
			byte v = 0x80;
			if (activeOutput) v |= 0x10;
			v |= (byte)((int)parity << 2);
			v |= (byte)((int)stopbits << 1);
			v |= (byte)rxIdle;
			root.WriteByte(v);
			root.ExpectReadByte(0x01);
			
		}

		public enum UARTSpeed {
			bps300 = 0,
			bps1200 = 1,
			bps2400 = 2,
			bps4800 = 3,
			bps9600 = 4,
			bps19200 = 5,
			bps31250 = 6,
			MIDI = bps31250,
			bps38400 = 7,
			bps57600 = 8,
			bps115200 = 9
		}

		public void SetSpeed(UARTSpeed speed)
		{
			root.WriteByte((byte)(0x60 | (byte)speed));
			root.ExpectReadByte(0x01);
		}

		public void SetSpeed(short BRG)
		{
			root.WriteByte(0x07);
			root.WriteByte((byte)(BRG >> 8));
			root.WriteByte((byte)(BRG & 0xff));
			
			root.ExpectReadByte(0x01);
			root.ExpectReadByte(0x01);
			root.ExpectReadByte(0x01);
			
		}

		public byte ReadByte()
		{
			if (!ReadEnabled)
				throw new InvalidOperationException("Enable reads first");

			return root.ReadByte();
		}

		public void Read(byte[] buffer, int offset, int length)
		{
			if (!ReadEnabled)
				throw new InvalidOperationException("Enable reads first");
			root.Read(buffer, offset, length);

		}

		
		

		#region IDisposable Members

		public void Dispose()
		{
			ExitMode();
		}

		#endregion

	}
}
