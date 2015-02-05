using System.Collections;
using RamjetAnvil.StateMachine;
using UnityEngine;

public class Coroutines : MonoBehaviour {
    private CoroutineScheduler _scheduler;

	void Start () {
        _scheduler = new CoroutineScheduler();

        for (int i = 0; i < 1000; i++) {
            _scheduler.Start(DoWorkEachFrame());
        }
	}

    IEnumerator DoWorkEachFrame() {
        for (int i = 0; i < 10000; i++) {
            yield return new WaitFrames();
        }
    }
	
	void Update () {
	    _scheduler.Update(Time.frameCount, Time.time);
	}
}
