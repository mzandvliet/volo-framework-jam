using System.Collections;
using UnityEngine;

public class Coroutines : MonoBehaviour {
    private CoroutineScheduler _scheduler;

	void Start () {
        _scheduler = new CoroutineScheduler();
	    _scheduler.StartCoroutine(WaitAndPrint());
	}

    IEnumerator WaitAndPrint() {
        Debug.Log("Start");
        yield return _scheduler.StartCoroutine(Wait());
        Debug.Log("End");
    }

    IEnumerator Wait() {
        yield return 1f;
    }
	
	void Update () {
	    _scheduler.Update(Time.frameCount, Time.time);
	}
}
