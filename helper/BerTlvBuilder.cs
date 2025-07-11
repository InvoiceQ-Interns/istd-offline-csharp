using System;
using System.Collections.Generic;
using System.Text;

namespace ISTD_OFFLINE_CSHARP.helper
{
    public class BerTlvBuilder
    {
        private readonly List<byte> tlvBytes = new();

        public BerTlvBuilder addText(int tag, string value, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            byte[] valueBytes = encoding.GetBytes(value);
            return addBytes(tag, valueBytes);
        }

        public BerTlvBuilder addBytes(int tag, byte[] valueBytes)
        {
            if (tag <= 0 || tag > 255) throw new ArgumentOutOfRangeException(nameof(tag), "Tag must be between 1 and 255");

            tlvBytes.Add((byte)tag);                    // Tag
            tlvBytes.Add((byte)valueBytes.Length);      // Length
            tlvBytes.AddRange(valueBytes);              // Value

            return this;
        }

        public byte[] buildArray()
        {
            return tlvBytes.ToArray();
        }
    }
}
