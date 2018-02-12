using UnityEngine;
using System.Collections.Generic;

namespace ImYellowFish.Utility
{

    /// <summary>
    /// a basic event dispatcher
    /// </summary>
    public class Dispatcher<T> where T : System.IConvertible
    {
        public delegate void EventHandler(IEventMessage msg);
        public bool debug = false;
        private Dictionary<T, EventHandler> dict;
        private List<T> history;

        /// <summary>
        /// Construct a new dispatcher. To eliminate boxing for Enum types, pass in a comparer.
        /// </summary>
        public Dispatcher(bool debug = false, IEqualityComparer<T> comparer = null)
        {
            this.debug = debug;
            if (comparer == null)
            {
                dict = new Dictionary<T, EventHandler>();
            }
            else
            {
                dict = new Dictionary<T, EventHandler>(comparer);
            }

            if (debug)
            {
                history = new List<T>();
            }
        }

        public void AddListener(T key, EventHandler handler)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] += handler;
            }
            else
            {
                dict.Add(key, handler);
            }
        }

        public void RemoveListener(T key, EventHandler handler)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] -= handler;
            }
            else
            {
                Debug.LogError("Event to remove does not exist: " + key);
            }
        }

        public void RemoveAllListeners()
        {
            dict.Clear();
        }

        public void Dispatch(T key, IEventMessage msg)
        {
            if (!dict.ContainsKey(key) || dict[key] == null)
            {
                return;
            }

            dict[key].Invoke(msg);
            if (debug)
            {
                Debug.Log("dispatch event: " + key);
                history.Add(key);
            }
        }

        /// <summary>
        /// The history of all invoked events. Null if debug is disabled.
        /// </summary>
        public List<T> DebugEventHistory
        {
            get { return history; }
        }
    }
}