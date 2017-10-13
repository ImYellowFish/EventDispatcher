using UnityEngine;
using System.Collections.Generic;

public class Dispatcher<T> where T : System.IConvertible{
    public bool debug = false;
    public Dispatcher(bool debug) {
        this.debug = debug;
    }

    public delegate void EventHandler(params object[] args);

    private Dictionary<object, EventHandler> dict = new Dictionary<object, EventHandler>();

    public void AddListener(T key, EventHandler handler) {
        if (dict.ContainsKey(key)) {
            dict[key] += handler;
        } else {
            dict.Add(key, handler);
        }
    }

    public void RemoveListener(T key, EventHandler handler) {
        if (dict.ContainsKey(key)) {
            dict[key] -= handler;
        } else {
            throw new System.ArgumentException("Event to remove does not exist: " + key);  // TODO throw
        }
    }

    public void Dispatch(T key, params object[] args) {
        if (debug)
            Debug.Log("dispatch event: " + key);

        if (!dict.ContainsKey(key) || dict[key] == null) {
            return;
        }

        dict[key].Invoke(args);
    }
}