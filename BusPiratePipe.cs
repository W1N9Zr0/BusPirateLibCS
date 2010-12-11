using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusPirateLibCS
{
    public interface BusPiratePipe
    {
        byte ReadByte();
		void Read(byte[] buffer, int offset, int length);
        void ExpectReadByte(byte b);
        void ExpectReadText(string s);
        void WriteByte(byte b);

		bool ReadWaiting { get; }

        void EnterExclusiveMode();
        void ExitExclusiveMode();
        bool IsInExclusiveMode();
    }
}
