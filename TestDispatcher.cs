using UnityEngine;
using System.Collections;

public class TestDispatcher : MonoBehaviour {
    public enum Events { Start = 0, End = 1}
    private Dispatcher<Events> dispatcher = new Dispatcher<Events>(true);

	// Use this for initialization
	void Start () {
        dispatcher.AddListener(Events.Start, (v) => { Debug.Log("on start: " + v[0]); });
        dispatcher.AddListener(Events.Start, (v) => { Debug.Log("on start no param: "); });
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetButtonDown("Fire1")) {
            dispatcher.Dispatch(Events.Start, 0);
        }
	}
}
