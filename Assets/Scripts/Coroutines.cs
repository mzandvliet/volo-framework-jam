using System.Collections.Generic;
using RamjetAnvil.StateMachine;
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

    IEnumerator<YieldCommand> WaitAndPrintA() {
        Debug.Log("WaitAndPrintA_Start");
        yield return new YieldCommand { Seconds = 3 };
        //yield return _scheduler.YieldStart(WaitAndPrintB());
        yield return new YieldCommand(WaitAndPrintB());
        Debug.Log("WaitAndPrintA_End");
    }

    IEnumerator<YieldCommand> WaitAndPrintB() {
        Debug.Log("WaitAndPrintB_Start");
        yield return new YieldCommand { Seconds = 3 };
        Debug.Log("WaitAndPrintB_End");
    }

    IEnumerator<YieldCommand> DoWorkEachFrame() {
        for (int i = 0; i < 100000; i++) {
            yield return new YieldCommand {Frames = 0};
        }
    }
	
	void Update () {
	    _scheduler.Update(Time.frameCount, Time.time);
	}
}
