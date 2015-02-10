using System.Collections.Generic;
using RamjetAnvil.Coroutine;
using UnityEngine;

public class Coroutines : MonoBehaviour {
    private CoroutineScheduler _scheduler;

	void Start () {
        _scheduler = new CoroutineScheduler();

	    _scheduler.Start(WaitAndPrintA());

        for (int i = 0; i < 1000; i++) {
            _scheduler.Start(DoWorkEachFrame());
        }
	}

    IEnumerator<WaitCommand> WaitAndPrintA() {
        Debug.Log("WaitAndPrintA_Start");
        yield return WaitCommand.WaitSeconds(3);
        yield return WaitCommand.WaitRoutine(WaitAndPrintB());
        Debug.Log("WaitAndPrintA_End");
    }

    IEnumerator<WaitCommand> WaitAndPrintB() {
        Debug.Log("WaitAndPrintB_Start");
        yield return WaitCommand.WaitSeconds(3);
        Debug.Log("WaitAndPrintB_End");
    }

    IEnumerator<WaitCommand> DoWorkEachFrame() {
        for (int i = 0; i < 100000; i++) {
            yield return WaitCommand.WaitForNextFrame;
        }
    }


	void Update () {
	    _scheduler.Update(Time.frameCount, Time.time);
	}
}
