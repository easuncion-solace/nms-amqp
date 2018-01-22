﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amqp.Types;
using Amqp;
using Apache.NMS;

namespace NMS.AMQP.Util.Types
{
    internal static class ConversionSupport
    {
        public static Amqp.Types.Map NMSMapToAmqp(IPrimitiveMap nmsmap)
        {
            if(nmsmap == null) return null;
            Amqp.Types.Map result = new Amqp.Types.Map();
            if(nmsmap is Map.AMQP.AMQPValueMap) { return (nmsmap as Map.AMQP.AMQPValueMap).AmqpMap; }
            else
            {
                foreach (string key in nmsmap.Keys)
                {
                    object value = nmsmap[key];

                    if (value is IDictionary)
                    {
                        value = ConversionSupport.MapToAmqp(value as IDictionary);
                    }
                    else if (value is IList)
                    {
                        value = ConversionSupport.ListToAmqp(value as IList);
                    }
                    
                    result[key] = value;
                    //Tracer.InfoFormat("Conversion key : {0}, value : {1}, valueType: {2} nmsValue: {3}, nmsValueType: {4}.", 
                    //    key, value, value.GetType().Name, );
                }
            }
            return result;
        } 

        public static IPrimitiveMap AmqpMapToNMS(Amqp.Types.Map map)
        {
            if (map == null) return null;
            IPrimitiveMap result = new Map.AMQP.AMQPValueMap(map);
            return result;
        }

        public static Amqp.Types.Map MapToAmqp(IDictionary dictionary)
        {
            if (dictionary == null) return null;
            /*if (dictionary is Amqp.Types.Map)
            {
                Amqp.Types.Map DictMap = dictionary as Amqp.Types.Map;
                
                return DictMap.Clone() as Amqp.Types.Map;
            }*/
            Amqp.Types.Map map = new Amqp.Types.Map();
            IEnumerator iterator = dictionary.Keys.GetEnumerator();
            iterator.MoveNext();
            object key = iterator.Current;
            if (key == null) return null;
            object value = null;
            do
            {
                value = dictionary[key];
                if (value != null)
                {
                    Type valtype = value.GetType();
                    if (value is IDictionary)
                    {
                        map[key] = ConversionSupport.MapToAmqp(value as IDictionary);
                    }
                    else if (value is IList)
                    {
                        map[key] = ConversionSupport.ListToAmqp(value as IList);
                    }
                    else if (IsNMSType(value))
                    //else if (valtype.IsPrimitive || value is byte[] || value is String)
                    {
                        map[key] = value;
                    }
                    else
                    {
                        Tracer.InfoFormat("Failed to convert IDictionary to Map value: Invalid Type: {0}", valtype.Name);
                    }

                }

            }
            while (iterator.MoveNext() && (key = iterator.Current) != null);
            return map;
        }


        public static IDictionary MapToNMS(Amqp.Types.Map map)
        {
            if (map == null) return null;
            
            IDictionary dictionary = new Dictionary<object, object>(map) as IDictionary;

            return dictionary;
        }

        public static List ListToAmqp(IList ilist)
        {
            if (ilist == null) return null;
            List list = new List();
            foreach(object o in ilist)
            {
                object value = o;
                if(o != null)
                {
                    Type valtype = value.GetType();
                    if (value is IDictionary)
                    {
                        value = ConversionSupport.MapToAmqp(value as IDictionary);
                    }
                    else if (value is IList)
                    {
                        value = ConversionSupport.ListToAmqp(value as IList);
                    }
                    else if (ConversionSupport.IsNMSType(value))
                    //else if (valtype.IsPrimitive || value is byte[] || value is String)
                    {
                        // do nothing
                        // value = value;
                    }
                    else
                    {
                        Tracer.InfoFormat("Failed to convert IList to amqp List value: Invalid Type: {0}", valtype.Name);
                    }
                }
                list.Add(value);
            }
            return list;
        }

        public static IList ListToNMS(List list)
        {
            if (list == null) return null;
            IList ilist = new ArrayList(list);
            return ilist;
        }

        public static string ToString(Amqp.Types.Map map)
        {
            if (map == null) return "{}";
            string result = "{";
            bool first = true;
            foreach (object key in map.Keys)
            {
                if (first) result += "\n";
                first = false;
                result += "key: " + key.ToString() + ", value: " + map[key].ToString() + ";\n";
            }
            result += "}";
            return result;
        }

        public static string ToString(IPrimitiveMap map)
        {
            if (map == null) return "{}";
            string result = "{";
            bool first = true;
            foreach (string key in map.Keys)
            {
                if (first) result += "\n";
                first = false;
                result += "key: " + key.ToString() + ", value: " + map[key].ToString() + ";\n";
            }
            result += "}";
            return result;
        }

        #region NMS Type Conversion Table

        static ConversionSupport(){
            Dictionary < ConversionKey, ConversionEntry > typeMap = new Dictionary<ConversionKey, ConversionEntry>(NMSTypeConversionSet.Count);
            foreach(ConversionEntry entry in NMSTypeConversionSet)
            {
                typeMap.Add(entry, entry);
            }

            NMSTypeConversionTable = typeMap;
        }

        public enum NMS_TYPE_INDEX
        {
            STRING = 0,
            INT32 = 1,
            INT16 = 2,
            INT64 = 3,
            FLOAT32 = 4,
            FLOAT64 = 5,
            DOUBLE = 5,
            INT8 = 6,
            CHAR = 7,
            BOOLEAN = 8,
            BYTE_ARRAY = 9,
            NULL = 10,
            OBJECT = 11,
            UNKOWN
        }

        private static readonly Type[] NMSTypes = { typeof(String), typeof(int), typeof(short), typeof(long), typeof(float), typeof(double), typeof(byte), typeof(char), typeof(bool), typeof(byte[]), null, typeof(object) };

        public delegate T ConversionInstance<T, K>(K o);

        private class ConversionKey : IComparable
        {
            private int hash = 0;
            internal static ConversionKey GetKey(Type target, Type Source)
            {

                return new ConversionKey(target, Source);
            }
            protected ConversionKey(Type target, Type source)
            {
                TargetType = target;
                SourceType = source;
                SetHashCode();
            }
            public Type TargetType { get; protected set; }
            public Type SourceType { get; protected set; }

            public int CompareTo(object obj)
            {
                if(obj != null && obj is ConversionKey) { return CompareTo(obj as ConversionKey); }
                return -1;
            }

            protected void SetHashCode()
            {
                long th = TargetType.GetHashCode();
                long sh = SourceType.GetHashCode();
                // Cantor pairing
                hash = (int)((th + sh) * (th + sh + 1) / 2 + sh);
            }

            protected virtual int CompareTo(ConversionKey other)
            {
                return other.GetHashCode() - this.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if(obj != null && obj is ConversionKey)
                {
                    return this.CompareTo(obj as ConversionKey) == 0;
                }
                else
                {
                    return base.Equals(obj);
                }
            }

            public override int GetHashCode()
            {
                return hash;
            }

            public override string ToString()
            {
                return this.GetType().Name + "| SourceType: " + SourceType.Name + ", TargetType " + TargetType.Name;
            }
        }

        private abstract class ConversionEntry : ConversionKey
        {

            protected ConversionEntry(Type t, Type s) : base(t,s) { }
            
            public abstract object Convert(object o);
        }

        private class ConversionEntry<T, K> : ConversionEntry
        {
            internal ConversionInstance<T, K> ConvertInstance;

            internal ConversionEntry() : base(typeof(T), typeof(K))
            {   
            }


            public override object Convert(object o)
            {
                if (ConvertInstance != null)
                {
                    return ConvertInstance((K)o);
                }
                return null;
            }

            public override string ToString()
            {
                return base.ToString() + ", Convert Instance Delegate : " + ConvertInstance.ToString();
            }
        }

        //public static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, ConversionInstance<,object>>> NMSTypeConversionTable = new Dictionary<Type, IReadOnlyDictionary<Type, ConversionInstance<?,?>>>
        //{
        /* Type convert to                           Types convert from            */
        /*{ Types[Convert.ToInt32(TYPE_INDEX.STRING)], new Type[]{ typeof(string), typeof(float), typeof(double), typeof(long), typeof(int),typeof(short),typeof(byte),typeof(bool),typeof(char)} },
        { Types[Convert.ToInt32(TYPE_INDEX.DOUBLE)], new Type[]{ typeof(string), typeof(float), typeof(double)} },
        { Types[Convert.ToInt32(TYPE_INDEX.FLOAT32)], new Type[]{ typeof(string), typeof(float)} },
        { Types[Convert.ToInt32(TYPE_INDEX.INT64)], new Type[]{ typeof(string), typeof(long), typeof(int), typeof(short), typeof(byte)} },
        { Types[Convert.ToInt32(TYPE_INDEX.INT32)], new Type[]{ typeof(string), typeof(int), typeof(short), typeof(byte)} },
        { Types[Convert.ToInt32(TYPE_INDEX.INT16)], new Type[]{ typeof(string), typeof(short), typeof(byte)} },
        { Types[Convert.ToInt32(TYPE_INDEX.INT8)], new Type[]{ typeof(string), typeof(byte)} },
        { Types[Convert.ToInt32(TYPE_INDEX.CHAR)], new Type[]{ typeof(char)} },
        { Types[Convert.ToInt32(TYPE_INDEX.BOOLEAN)], new Type[]{ typeof(string), typeof(bool)} },*/

        //};
#if NET40
        private static readonly IDictionary<ConversionKey, ConversionEntry> NMSTypeConversionLookupTable;
        private static readonly IDictionary<ConversionKey, ConversionEntry> NMSTypeConversionTable;
#else

        private static readonly IReadOnlyDictionary<ConversionKey, ConversionEntry> NMSTypeConversionLookupTable;
        private static readonly IReadOnlyDictionary<ConversionKey, ConversionEntry> NMSTypeConversionTable;
#endif
        private static readonly ISet<ConversionEntry> NMSTypeConversionSet = new HashSet<ConversionEntry>
        {
            // string conversion
            { new ConversionEntry<string, string>{ConvertInstance = ((o) =>{ return Convert.ToString(o); }) } },
            { new ConversionEntry<string, float>{ConvertInstance = ((o) =>{ return Convert.ToString(o); }) } },
            { new ConversionEntry<string, double>{ConvertInstance = ((o) =>{ return Convert.ToString(o); }) } },
            { new ConversionEntry<string, long>{ConvertInstance = ((o) =>{ return Convert.ToString(o); }) } },
            { new ConversionEntry<string, int>{ConvertInstance = ((o) =>{ return Convert.ToString(o); }) } },
            { new ConversionEntry<string, short>{ConvertInstance = ((o) =>{ return Convert.ToString(o); }) } },
            { new ConversionEntry<string, byte>{ConvertInstance = ((o) =>{ return Convert.ToString(o); }) } },
            { new ConversionEntry<string, bool>{ConvertInstance = ((o) =>{ return Convert.ToString(o); }) } },
            { new ConversionEntry<string, char>{ConvertInstance = ((o) =>{ return Convert.ToString(o); }) } },
            //{new ConversionEntry<string, byte[]>{ConvertInstance = ((o) =>{ throw new InvalidOperationException("Cannot convert string to byte array."); }) } },
            // double conversion
            { new ConversionEntry<double, string>{ConvertInstance = ((o) =>{ return Convert.ToDouble(o); }) } },
            { new ConversionEntry<double, float>{ConvertInstance = ((o) =>{ return Convert.ToDouble(o); }) } },
            { new ConversionEntry<double, double>{ConvertInstance = ((o) =>{ return Convert.ToDouble(o); }) } },
            // float conversion
            { new ConversionEntry<float, string>{ConvertInstance = ((o) =>{ return Convert.ToSingle(o); }) } },
            { new ConversionEntry<float, float>{ConvertInstance = ((o) =>{ return Convert.ToSingle(o); }) } },
            // long conversion
            { new ConversionEntry<long, string>{ConvertInstance = ((o) =>{ return Convert.ToInt64(o); }) } },
            { new ConversionEntry<long, long>{ConvertInstance = ((o) =>{ return Convert.ToInt64(o); }) } },
            { new ConversionEntry<long, int>{ConvertInstance = ((o) =>{ return Convert.ToInt64(o); }) } },
            { new ConversionEntry<long, short>{ConvertInstance = ((o) =>{ return Convert.ToInt64(o); }) } },
            { new ConversionEntry<long, byte>{ConvertInstance = ((o) =>{ return Convert.ToInt64(o); }) } },
            // int conversion
            { new ConversionEntry<int, string>{ConvertInstance = ((o) =>{ return Convert.ToInt32(o); }) } },
            { new ConversionEntry<int, int>{ConvertInstance = ((o) =>{ return Convert.ToInt32(o); }) } },
            { new ConversionEntry<int, short>{ConvertInstance = ((o) =>{ return Convert.ToInt32(o); }) } },
            { new ConversionEntry<int, byte>{ConvertInstance = ((o) =>{ return Convert.ToInt32(o); }) } },
            // short conversion
            { new ConversionEntry<short, string>{ConvertInstance = ((o) =>{ return Convert.ToInt16(o); }) } },
            { new ConversionEntry<short, short>{ConvertInstance = ((o) =>{ return Convert.ToInt16(o); }) } },
            { new ConversionEntry<short, byte>{ConvertInstance = ((o) =>{ return Convert.ToInt16(o); }) } },
            // byte conversion
            { new ConversionEntry<byte, string>{ConvertInstance = ((o) =>{ return Convert.ToByte(o); }) } },
            { new ConversionEntry<byte, byte>{ConvertInstance = ((o) =>{ return Convert.ToByte(o); }) } },
            // boolean conversion
            { new ConversionEntry<bool, string>{ConvertInstance = ((o) =>{ return Convert.ToBoolean(o); }) } },
            { new ConversionEntry<bool, bool>{ConvertInstance = ((o) =>{ return Convert.ToBoolean(o); }) } },
            // char conversion
            { new ConversionEntry<char, char>{ConvertInstance = ((o) =>{ return Convert.ToChar(o); }) } },
        };


        public static Type ForIndex(NMS_TYPE_INDEX index)
        {
            int i = Convert.ToInt32(index);
            
            if(i<0 || i >= (int)NMS_TYPE_INDEX.UNKOWN)
            {
                throw new IndexOutOfRangeException("Unrecognized NMS Type Index " + index);
            }
            else
            {
                return NMSTypes[i];
            }
            
        }

        public static bool IsNMSType(object value)
        {
            bool result = false;
            int index = 0;
            Type t = NMSTypes[index];
            while (t != null && !result)
            {
                result |= t.Equals(value.GetType());
                t = NMSTypes[index++];
            }
            return result;
        }

        public static bool CanConvertNMSType<T>(object value)
        {
            ConversionKey key = ConversionKey.GetKey(typeof(T), value.GetType());
            return NMSTypeConversionSet.Contains(key);
        }

        public static T ConvertNMSType<T, S>(S value)
        {
            ConversionKey key = ConversionKey.GetKey(typeof(T), value.GetType());
            ConversionEntry<T, S> converter = (ConversionEntry<T, S>)NMSTypeConversionTable[key];
            if(converter == null)
            {
                throw new NMSTypeConversionException("Cannot convert between type : " + (typeof(T)).Name + ", and type: " + value.GetType().Name);
            }
            return converter.ConvertInstance(value);

        }

        public static T ConvertNMSType<T>(object value)
        {
            ConversionKey key = ConversionKey.GetKey(typeof(T), value.GetType());
            NMSTypeConversionTable.TryGetValue(key, out ConversionEntry converter);
            if (converter == null)
            {
                throw new NMSTypeConversionException("Cannot convert between type : " + (typeof(T)).Name + ", and type: " + value.GetType().Name);
            }
            return (T)converter.Convert(value);
        }

        public class NMSTypeConversionException : MessageFormatException
        {
            public NMSTypeConversionException() : base() { }
            public NMSTypeConversionException(string message) : base(message) { }
        }

#endregion

    }
}
