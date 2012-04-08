//#define DEBUG_SERIAL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace BusPirateLibCS.Util
{
	/// <summary>
	/// Wraps the faster asynchronous SerialPort operations into easier to use synchronous functions
	/// </summary>
	public class ExpectingSerialPort
	{

		/// <summary>
		/// Serial port being wrapped
		/// </summary>
		protected SerialPort myPort;


		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Set the ReadTimeout before passing port into here
		/// </remarks>
		/// <param name="port">SerialPort to use</param>
		public ExpectingSerialPort(SerialPort port)
		{
			myPort = port;
			myPort.DataReceived += new SerialDataReceivedEventHandler(myPort_DataReceived);
		}

		public void Open()
		{
			myPort.Open();
		}

		public void Close()
		{
			lock (expectedLock)
			{
				myPort.Close();
				expected.Clear();
			}
		}

		/// <summary>
		/// Queue for bytes expected to be received, or MAGIC_WAIT_CONSTANT taking place
		/// of items that are to be passed on to the receiveSTUFF commands.
		/// </summary>
		private SynchedQueue<int> expected = new SynchedQueue<int>();
		
		/// <summary>
		/// Lock for the expected queue
		/// </summary>
		Object expectedLock = new Object();

		private void myPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			int exp;
			
			// Lock the queue so it can't be modified while we're receiving data
			lock (expectedLock)
			{
				while (myPort.BytesToRead > 0)
				{
					if (expected.Count == 0)
						return;
					
					exp = expected.Peek();

					if (exp == MAGIC_WAIT_CONSTANT)
						return;
					
					int got = myPort.ReadByte();
					if (exp != got)
					{
						Thread.Sleep(100);
						var d = new byte[myPort.BytesToRead];
						myPort.Read(d, 0, d.Length);
						throw new IOException(String.Format("Exepected: {0:X2} got: {1:X2}", exp, got));
					}
					
					expected.Dequeue();
				}
			}
		}

		protected void wait(int t)
		{
			Thread.Sleep(t);
		}

		public void WriteByte(byte b)
		{
			myPort.Write(new byte[] { b }, 0, 1);
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			myPort.Write(buffer, offset, count);
		}

		private const int MAGIC_WAIT_CONSTANT = 0xbadf00d;

		public byte ReadByte()
		{
			byte[] buffer = new byte[1];
			Read(buffer, 0, 1);
			return buffer[0];
		}

		public bool ReadWaiting
		{
			get
			{
				if (expected.Count > 0)
					return false;

				return myPort.BytesToRead > 0;
			}
		}

		public int Read(byte[] buffer, int offset, int length)
		{
			
			expected.Enqueue(MAGIC_WAIT_CONSTANT);

			while (expected.Count == 0 || (expected.Count > 0 && expected.Peek() != MAGIC_WAIT_CONSTANT))
				sleep();

			int read = 0;

			lock (expectedLock)
			{

				try
				{
					while (read < length - offset)
					{
						if (read > 0) sleep();
						read += myPort.Read(buffer, offset + read, length - read);
					}
				}
				catch (TimeoutException ex)
				{
					
				}
				finally
				{
					expected.Dequeue();
				}
			}
			return read;
		}

		private void sleep()
		{
			wait(1);
		}

		public int ReadTimeout
		{
			get
			{
				return myPort.ReadTimeout;
			}
			set
			{
				myPort.ReadTimeout = value;
			}
		}


		public void ExpectReadByte(byte b)
		{
#if DEBUG_SERIAL
			var result = myPort.ReadByte();
			if (b != result)
				throw new Exception("FLAIL!");
#else
			expected.Enqueue(b);
#endif
		}


		public void ExpectRead(byte[] expect)
		{
			foreach (var b in expect)
			{
				ExpectReadByte(b);
			}
		}


		public void ExpectReadText(string text)
		{
			ExpectRead(Encoding.ASCII.GetBytes(text));
		}


		internal void ClearQueue()
		{
			lock (expectedLock)
			{
				while (myPort.BytesToRead > 0)
				{
					var buffer = new byte[myPort.BytesToRead];
					myPort.Read(buffer, 0, buffer.Length);
					wait(100);
				}
				expected.Clear();
			}
		}
	}
}
