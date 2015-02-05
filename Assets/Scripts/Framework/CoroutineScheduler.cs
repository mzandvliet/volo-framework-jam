using System;
using System.Collections;
using System.Collections.Generic;

namespace RamjetAnvil.StateMachine {
    /// <summary>
    /// A coroutine system initialy based on this, but prettier: http://wiki.unity3d.com/index.php?title=CoroutineScheduler
    /// </summary>
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
            while (r != null) {
                _routines.Remove(r);
                r = (Routine) r.CurrentInstruction;
            }
        }

        public void Update(int frame, float deltaTime) {
            for (int i = 0; i < _routines.Count; i++) {
                var routine = _routines[i];

                var updatedInstruction = routine.CurrentInstruction.Update(frame, deltaTime);
                routine.CurrentInstruction = updatedInstruction;

                if (updatedInstruction.IsFinished) {
                    if (routine.Fibre.MoveNext()) {
                        UpdateRoutine(routine);
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

        private void UpdateRoutine(Routine routine) {
            // Handle regular yield instructions. If that fails, see if we're actually supposed to start a subroutine

            var current = routine.Fibre.Current as IYieldInstruction;
            if (current != null) {
                routine.CurrentInstruction = current;
            }
            else {
                var fibre = routine.Fibre.Current as IEnumerator;
                if (fibre != null) {
                    routine.CurrentInstruction = Start(fibre);
                }
                else {
                    throw new ArgumentException("Invalid yield type " + routine.Fibre.Current);
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

        public Routine(IEnumerator fibre) {
            Fibre = fibre;
            CurrentInstruction = new IdentityInstruction();
            IsFinished = false;
        }

        public IYieldInstruction Update(int frame, float deltaTime) {
            return this;
        }

        public bool IsFinished { get; set; }
    }
}