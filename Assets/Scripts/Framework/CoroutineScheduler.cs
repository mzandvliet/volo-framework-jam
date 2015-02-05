using System;
using System.Collections;
/* Based on this, but prettified slightly: http://wiki.unity3d.com/index.php?title=CoroutineScheduler
 */
using System.Collections.Generic;

namespace RamjetAnvil.StateMachine {
    public class CoroutineScheduler {
        private IList<Routine> _routines;

        public CoroutineScheduler() {
            _routines = new List<Routine>();
        }

        public Routine Start(IEnumerator fibre) {
            if (fibre == null) {
                throw new Exception("Coroutine cannot be null");
            }

            Routine coroutine = new Routine(fibre);
            _routines.Add(coroutine);
            return coroutine;
        }

        public void Stop(Routine r) {
            _routines.Remove(r);
            /*while (r != null) {
                _routines.Remove(r);
                r = r.WaitForCoroutine;
            }*/
        }

        public void Update(int frame, float deltaTime) {
            for (int i = 0; i < _routines.Count; i++) {
                var routine = _routines[i];

                /*var updatedInstruction = routine.CurrentInstruction.Update(frame, deltaTime);
                routine.CurrentInstruction = updatedInstruction;
                if (updatedInstruction.IsFinished) {
                    if (routine.Fibre.MoveNext()) {
                        routine.CurrentInstruction = routine.Fibre.Current as IYieldInstruction;
                        if (routine.CurrentInstruction == null) {
                            throw new ArgumentException("Invalid yield type " + routine.Fibre.Current);
                        }
                    } else {
                        routine.IsFinished = true;
                    }
                }*/
                routine.Update(frame, deltaTime);

                if (routine.IsFinished) {
                    _routines.Remove(routine);
                }
            }

        }
    }

    public interface IYieldInstruction {
        IYieldInstruction Update(int frame, float deltaTime);
        bool IsFinished { get; }
    }

    public struct IdentityInstruction : IYieldInstruction {
        public IYieldInstruction Update(int frame, float deltaTime) {
            return this;
        }

        public bool IsFinished { get { return true; } }
    }

    public struct WaitSeconds : IYieldInstruction {
        public float Seconds;

        public IYieldInstruction Update(int frame, float deltaTime) {
            return new WaitSeconds { Seconds = Seconds - deltaTime };
        }

        public bool IsFinished { get { return Seconds <= 0f; } }
    }

    public struct WaitFrames : IYieldInstruction {
        public int Frames;
        public IYieldInstruction Update(int frame, float deltaTime) {
            return new WaitFrames{Frames = Frames - 1};
        }

        public bool IsFinished {
            get { return Frames <= 0; }
        }
    }

    public class Routine : IYieldInstruction {
        public IEnumerator Fibre;

        public IYieldInstruction CurrentInstruction;

        private bool _isFinished;

        public Routine(IEnumerator fibre) {
            Fibre = fibre;
            CurrentInstruction = new IdentityInstruction();
            _isFinished = false;
        }

        public IYieldInstruction Update(int frame, float deltaTime) {
            CurrentInstruction = CurrentInstruction.Update(frame, deltaTime);
            if (CurrentInstruction.IsFinished) {
                if (Fibre.MoveNext()) {
                    CurrentInstruction = Fibre.Current as IYieldInstruction;
                    if (Fibre.Current is IEnumerator) {
                        CurrentInstruction = new Routine(Fibre.Current as IEnumerator);
                    } else if (CurrentInstruction == null) {
                        _isFinished = true;
                        throw new ArgumentException("Invalid yield type " + Fibre.Current);
                    }
                } else {
                    _isFinished = true;
                }
            }

            return this;
        }

        public bool IsFinished {
            get { return _isFinished; }
        }
    }
}