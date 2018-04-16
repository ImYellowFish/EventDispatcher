namespace ImYellowFish.Utility
{
    /// <summary>
    /// The message that is passed to event handler
    /// </summary>
    public interface IEventMessage
    {
        object Arg0 { get; }
        object Arg1 { get; }
        object Arg2 { get; }
        object Arg3 { get; }
        object Arg4 { get; }
        object[] Args { get; }
    }

    /// <summary>
    /// Empty message.
    /// </summary>
    public class EmptyEventMessage : IEventMessage
    {
        public object Arg0 { get { return null; } }
        public object Arg1 { get { return null; } }
        public object Arg2 { get { return null; } }
        public object Arg3 { get { return null; } }
        public object Arg4 { get { return null; } }
        public object[] Args { get { return new object[0]; } }

        private static EmptyEventMessage _instance;
        public static EmptyEventMessage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new EmptyEventMessage();
                return _instance;
            }
        }
    }

    /// <summary>
    /// A message with 5 parameters.
    /// </summary>
    public class FixedEventMessage : IEventMessage
    {
        private object arg0;
        private object arg1;
        private object arg2;
        private object arg3;
        private object arg4;

        public FixedEventMessage(object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null)
        {
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
        }

        public object Arg0
        {
            get { return arg0; }
        }

        public object Arg1
        {
            get { return arg1; }
        }

        public object Arg2
        {
            get { return arg2; }
        }

        public object Arg3
        {
            get { return arg3; }
        }

        public object Arg4
        {
            get { return arg4; }
        }

        public object[] Args
        {
            get { return new object[] { Arg0, Arg1, Arg2, Arg3, Arg4 }; }
        }
    }

    /// <summary>
    /// A message with arbitrary number of parameters.
    /// </summary>
    public class ArrayEventMessage : IEventMessage
    {
        private object[] args;
        public ArrayEventMessage(params object[] args)
        {
            this.args = args;
        }

        public object Arg0
        {
            get { return GetArg(0); }
        }

        public object Arg1
        {
            get { return GetArg(1); }
        }

        public object Arg2
        {
            get { return GetArg(2); }
        }

        public object Arg3
        {
            get { return GetArg(3); }
        }

        public object Arg4
        {
            get { return GetArg(4); }
        }

        public object[] Args
        {
            get { return args; }
        }

        private object GetArg(int index)
        {
            if (args != null && args.Length > index)
            {
                return args[index];
            }
            return null;
        }
    }
}