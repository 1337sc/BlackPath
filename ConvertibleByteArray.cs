using System;
using System.Collections.Generic;
using System.Text;

namespace tgBot
{
    public class ConvertibleByteArray : IConvertible
    {
        public byte[] Value { get; set; }
        public ConvertibleByteArray(byte[] value)
        {
            Value = value;
        }

        public object ConvertFromByteArray(byte[] value, Type type)
        {
            if (type == typeof(int))
            {
                return BitConverter.ToInt32(value, 0);
            }
            if (type == typeof(double))
            {
                return BitConverter.ToDouble(value, 0);
            }
            return new object();
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(Value[0]);
        }

        public byte ToByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public char ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(Value[0]);
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return (decimal)BitConverter.ToDouble(Value, 0);
        }

        public double ToDouble(IFormatProvider provider)
        {
            return BitConverter.ToDouble(Value, 0);
        }

        public short ToInt16(IFormatProvider provider)
        {
            return BitConverter.ToInt16(Value, 0);
        }

        public int ToInt32(IFormatProvider provider)
        {
            return BitConverter.ToInt32(Value, 0);
        }

        public long ToInt64(IFormatProvider provider)
        {
            return BitConverter.ToInt64(Value, 0);
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public float ToSingle(IFormatProvider provider)
        {
            return BitConverter.ToSingle(Value, 0);
        }

        public string ToString(IFormatProvider provider)
        {
            return BitConverter.ToString(Value, 0);
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(this, conversionType);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return BitConverter.ToUInt16(Value, 0);
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return BitConverter.ToUInt32(Value, 0);
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return BitConverter.ToUInt64(Value, 0);
        }
    }
}
