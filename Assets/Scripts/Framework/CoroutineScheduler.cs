using System;
using System.Collections.Generic;

/* Todo: 
 * - Don't expose some internals of Routine class to users
 * - Decide on flat array / recursive routine storage techniques
 * - Could optimize away a type check by not having coroutines be YieldInstructions, but having them as explicit
 * secondary member called 'subroutine' of the Coroutine interterface (if subroutine, do that, else handle yield instruction)
 */

namespace RamjetAnvil.StateMachine {
    /// <summary>
    /// A coroutine system initialy based on this, but prettier: http://wiki.unity3d.com/index.php?title=CoroutineScheduler
    /// </summary>
    public class CoroutineScheduler {
        private IList<Routine> _routines;

        public CoroutineScheduler() {
            _routines = new List<Routine>();
        }

        public Routine Start(IEnumerator<YieldCommand> fibre) {
            if (fibre == null) {
                throw new Exception("Coroutine cannot be null");
            }

            Routine coroutine = new Routine(fibre);
            _routines.Add(coroutine);
            return coroutine;
        }

        public YieldCommand YieldStart(IEnumerator<YieldCommand> fibre) {
            return new YieldCommand {Routine = Start(fibre)};
        }

        public void Stop(Routine r) {
            _routines.Remove(r);
            while (r != null) {
                _routines.Remove(r);
                r = r.Fibre.Current.Routine;
            }
        }

        private int _lastFrame;
        private float _lastTime;

        public void Update(int frame, float time) {
            int deltaFrames = frame - _lastFrame;
            _lastFrame = frame;
            float deltaTime = time - _lastTime;
            _lastTime = time;

            for (int i = 0; i < _routines.Count; i++) {
                var routine = _routines[i];
                routine.Command.Update(deltaFrames, deltaTime);

                if (routine.Command.IsIdentity() || routine.Command.IsFinished) {
                    if (routine.Fibre.MoveNext()) {
                        routine.Command = routine.Fibre.Current;
                    }
                    else {
                        routine.IsFinished = true;
                    }
                }

                if (routine.IsFinished) {
                    _routines.Remove(routine);
                }
            }

        }
    }

    public struct YieldCommand {
        public int? Frames;
        public float? Seconds;
        public Routine Routine;

        public void Update(int deltaFrames, float deltaTime) {
            Frames -= deltaFrames;
            Seconds -= deltaTime;
        }

        public bool IsFinished {
            get {
                return
                    (Frames != null && Frames <= 0) ||
                    (Seconds != null && Seconds <= 0) ||
                    (Routine != null && Routine.IsFinished);
            }
        }

        public bool IsIdentity() {
            return Frames == null && Seconds == null && Routine == null;
        }

        public static readonly YieldCommand NextFrame = new YieldCommand {Frames = 0};
    }

    public class Routine {
        public IEnumerator<YieldCommand> Fibre;

        public YieldCommand Command;

        public Routine(IEnumerator<YieldCommand> fibre) {
            Fibre = fibre;
            
//            if (Fibre.MoveNext()) {
//                Instruction = Fibre.Current;
//            }

            IsFinished = false;
        }

        public bool IsFinished { get; set; }
    }
}