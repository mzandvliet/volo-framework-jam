using System;
using System.Collections;

/* Based on this, but prettified slightly: http://wiki.unity3d.com/index.php?title=CoroutineScheduler
 */
public class CoroutineScheduler {
    private LinkedList<Routine> _routines;

    public CoroutineScheduler() {
        _routines = new LinkedList<Routine>();
    }

    public Routine Start(IEnumerator fibre) {
        if (fibre == null) {
            return null;
        }

        Routine coroutine = new Routine(fibre);
        _routines.Add(coroutine);
        return coroutine;
    }

    public void Update(int frame, float time) {
        var node = _routines.First;
        while (node != null) {
            var nextNode = node.Next;

            var routine = node.Value;
            if (routine.WaitForFrame > 0 && frame >= routine.WaitForFrame) {
                routine.WaitForFrame = -1;
                UpdateCoroutine(node.Value, frame, time);
            } else if (routine.WaitForTime > 0f && time >= routine.WaitForTime) {
                routine.WaitForTime = -1f;
                UpdateCoroutine(node.Value, frame, time);
            } else if (routine.WaitForCoroutine != null && routine.WaitForCoroutine.Finished) {
                routine.WaitForCoroutine = null;
                UpdateCoroutine(routine, frame, time);
            } else if (routine.WaitForFrame == -1 && routine.WaitForTime == -1f && routine.WaitForCoroutine == null) {
                UpdateCoroutine(routine, frame, time);
            }

            if (node.Value.Finished) {
                _routines.Remove(node);
            }
            node = nextNode;
        }
    }
    
    private void UpdateCoroutine(Routine coroutine, int frame, float time) {
        if (coroutine.Fibre.MoveNext()) {
            System.Object yieldCommand = coroutine.Fibre.Current ?? 1;
            System.Type yieldType = yieldCommand.GetType();

            if (yieldType == typeof (int)) {
                coroutine.WaitForFrame = (int) yieldCommand;
                coroutine.WaitForFrame += frame;
            } else if (yieldType == typeof(float)) {
                coroutine.WaitForTime = (float)yieldCommand;
                coroutine.WaitForTime += time;
            } else if (yieldType == typeof (Routine)) {
                coroutine.WaitForCoroutine = (Routine) yieldCommand;
            }
            else {
                throw new ArgumentException("CoroutineScheduler: Unexpected coroutine yield type: " + yieldType);
            }
        } else {
            coroutine.Finished = true;
        }
    }
}

public class Routine {
    public IEnumerator Fibre;

    public bool Finished;

    public int WaitForFrame = -1;
    public float WaitForTime = -1f;
    public Routine WaitForCoroutine;

    public Routine(IEnumerator fibre) {
        Fibre = fibre;
    }
}