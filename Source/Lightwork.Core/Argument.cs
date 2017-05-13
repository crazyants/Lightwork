using System;
using D3.Lightwork.Core.Utilities;

namespace D3.Lightwork.Core
{
    public abstract class Argument
    {
        protected readonly object ValueLock = new object();
        private object _value;

        protected Argument(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public virtual object Value
        {
            get
            {
                lock (ValueLock)
                {
                    return _value;
                }
            }

            set
            {
                lock (ValueLock)
                {
                    _value = value;
                }
            }
        }

        public static Argument Create(Type argumentType, string name, object value)
        {
            var argumentBase = typeof(Argument<>);
            var argumentClass = argumentBase.MakeGenericType(argumentType);
            var argument = Activator.CreateInstance(argumentClass, new object[] { name }) as Argument;

            if (argument == null)
            {
                throw new Exception("Couldn't create argument");
            }

            argument.Value = value;
            return argument;
        }

        public static Argument Create(string argumentType, string name, object value)
        {
            return Create(Type.GetType(argumentType), name, value);
        }

        public void Lock(Action action)
        {
            lock (ValueLock)
            {
                action();
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class Argument<T> : Argument
    {
        public Argument(string name)
            : this(name, default(T))
        {
        }

        public Argument(string name, T value)
            : base(name)
        {
            Value = value;
        }

        public new T Value
        {
            get { return TypeHelper.ChangeType<T>(base.Value); }
            set { base.Value = value; }
        }

        public void Lock(Action<Argument<T>> action)
        {
            lock (ValueLock)
            {
                action(this);
            }
        }

        public bool Equals(Argument<T> obj)
        {
            return Value.Equals(obj.Value);
        }

        public bool Equals(T obj)
        {
            return Value.Equals(obj);
        }
    }
}