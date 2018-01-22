﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using System.Collections;

namespace NMS.AMQP.Util.Types.Map
{
    /// <summary>
    /// Utility class that implements Apache.NMS.Util.IPrimativeMap.
    /// Key Methods and Properties are abstracted to facilitate different container concrete implementations.
    /// </summary>
    public abstract class PrimitiveMapBase : IPrimitiveMap
    {
        
        #region Abstract IPrimativeMap Methods

        public abstract int Count { get; }

        public abstract ICollection Keys { get; }

        public abstract ICollection Values { get; }

        public abstract void Clear();

        public abstract void Remove(object key);

        public abstract bool Contains(object key);

        #endregion

        #region IPrimativeMap Methods

        public object this[string key]
        {
            get
            {
                return GetObjectProperty(key);
            }

            set
            {
                CheckValidType(value);
                SetObjectProperty(key, value);
            }
        }


        public bool GetBool(string key)
        {
            object value = GetObjectProperty(key);
            CheckValueType(value, typeof(bool));
            return (bool)value;
        }

        public byte GetByte(string key)
        {
            object value = GetObjectProperty(key);
            CheckValueType(value, typeof(byte));
            return (byte)value;
        }

        public byte[] GetBytes(string key)
        {
            object value = GetObjectProperty(key);
            if (value != null && value is byte[])
            {
                throw new NMSException("Property: " + key + " is not byte[] but is: " + value);
            }
            return (byte[])value;
        }

        public char GetChar(string key)
        {
            object value = GetObjectProperty(key);
            CheckValueType(value, typeof(char));
            return (char)value;
        }

        public IDictionary GetDictionary(string key)
        {
            object value = GetObjectProperty(key);
            if (value != null && value is IDictionary)
            {
                throw new NMSException("Property: " + key + " is not IDictionary but is: " + value);
            }
            return (IDictionary)value;
        }

        public double GetDouble(string key)
        {
            object value = GetObjectProperty(key);
            CheckValueType(value, typeof(double));
            return (double)value;
        }

        public float GetFloat(string key)
        {
            object value = GetObjectProperty(key);
            CheckValueType(value, typeof(float));
            return (float)value;
        }

        public int GetInt(string key)
        {
            object value = GetObjectProperty(key);
            CheckValueType(value, typeof(int));
            return (int)value;
        }

        public IList GetList(string key)
        {
            object value = GetObjectProperty(key);
            if (value != null && value is IList)
            {
                throw new NMSException("Property: " + key + " is not IList but is: " + value);
            }
            return (IList)value;
        }

        public long GetLong(string key)
        {
            object value = GetObjectProperty(key);
            CheckValueType(value, typeof(long));
            return (long)value;
        }

        public short GetShort(string key)
        {
            object value = GetObjectProperty(key);
            CheckValueType(value, typeof(short));
            return (short)value;
        }

        public string GetString(string key)
        {
            object value = GetObjectProperty(key);
            CheckValueType(value, typeof(string));
            return (string)value;
        }

        public void SetBool(string key, bool value)
        {
            SetObjectProperty(key, value);
        }

        public void SetByte(string key, byte value)
        {
            SetObjectProperty(key, value);
        }

        public void SetBytes(string key, byte[] value)
        {
            SetBytes(key, value, 0, value.Length);
        }

        public void SetBytes(string key, byte[] value, int offset, int length)
        {
            byte[] copy = new byte[length];
            Array.Copy(value, offset, copy, 0, length);
            SetObjectProperty(key, copy);
        }

        public void SetChar(string key, char value)
        {
            SetObjectProperty(key, value);
        }

        public void SetDictionary(string key, IDictionary dictionary)
        {
            SetObjectProperty(key, dictionary);
        }

        public void SetDouble(string key, double value)
        {
            SetObjectProperty(key, value);
        }

        public void SetFloat(string key, float value)
        {
            SetObjectProperty(key, value);
        }

        public void SetInt(string key, int value)
        {
            SetObjectProperty(key, value);
        }

        public void SetList(string key, IList list)
        {
            SetObjectProperty(key, list);
        }

        public void SetLong(string key, long value)
        {
            SetObjectProperty(key, value);
        }

        public void SetShort(string key, short value)
        {
            SetObjectProperty(key, value);
        }

        public void SetString(string key, string value)
        {
            SetObjectProperty(key, value);
        }

        #endregion

        #region Protected Abstract Methods

        internal abstract object SyncRoot { get; }
        protected abstract object GetObjectProperty(string key);
        protected abstract void SetObjectProperty(string key, object value);

        #endregion

        #region Protected Methods 

        protected virtual void CheckValueType(Object value, Type type)
        {
            if (!type.IsInstanceOfType(value))
            {
                throw new NMSException("Expected type: " + type.Name + " but was: " + value);
            }
        }

        protected virtual void CheckValidType(Object value)
        {
            if (value != null && !(value is IList) && !(value is IDictionary))
            {
                Type type = value.GetType();

                if (type.IsInstanceOfType(typeof(Object)) ||
                   (!type.IsPrimitive && !type.IsValueType && !type.IsAssignableFrom(typeof(string))) ||
                   (!ConversionSupport.IsNMSType(value))
                   )
                {
                    throw new NMSException("Invalid type: " + type.Name + " for value: " + value);
                }
            }
        }

        #endregion

        #region Overriden Methods

        public override string ToString()
        {
            string result = "{";
            bool first = true;
            lock (SyncRoot)
            {
                foreach (string key in this.Keys)
                {
                    if (!first) { result += ", "; }
                    first = false;
                    object value = GetObjectProperty(key);
                    result = key + "=" + value;
                }
            }
            result += "}";
            return result;
        }

        #endregion 
    }
}
