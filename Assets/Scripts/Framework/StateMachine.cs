using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/*
 * Todo:
 * 
 * The owner's events should really just be references to delegates, I think. No need for multicast.
 * Support Coroutines?
 * Special handling for parent->child and child-parent relationships?
 * 
 * Event propagation (damage dealing and handling with complex hierarchies is a good one)
 * Timed, cancelable transitions (camera motions are a good one)
 * Test more intricate parent->child relationships, callable states
 * 
 * Enable OnEnter and OnExit to be implemented as optional, cancelable coroutines.
 */

/*
 * Desired state machine declaration style:
 * 
 * {in-spawnpoint-menu: {transitions: [in-game]
 *                       child-transitions: [in-options-menu]}
 *  in-game:            {transitions: [in-spawnpoint-menu]
 *                       child-transitions: [in-options-menu]}
 *  in-options-menu     {transitions: [in-spawnpoint-menu in-game in-couse-editor]}
 *  in-course-editor    {child-transitions: [in-game, in-options-menu]
 *  in-spectator        {child-transitions: [in-options-menu]}
 * }         
 * 
 */

namespace RamjetAnvil.StateMachine {
    [AttributeUsage(AttributeTargets.Method)]
    public class StateMethodAttribute : Attribute {}

    public interface IStateMachine {
        CoroutineScheduler Scheduler { get; }

        void Transition(StateId stateId, params object[] args);
        void TransitionToParent();
    }

    public class StateMachine<T> : IStateMachine {
        private readonly T _owner;
        private readonly CoroutineScheduler _scheduler;
        private readonly IDictionary<StateId, StateInstance> _states;
        private readonly IteratableStack<StateInstance> _stack;

        private readonly IList<MethodInfo> _ownerMethods;
        private readonly IDictionary<string, EventInfo> _ownerEvents;

        public CoroutineScheduler Scheduler {
            get { return _scheduler; }
        }

        public StateMachine(T owner, CoroutineScheduler scheduler) {
            _owner = owner;
            _scheduler = scheduler;

            _states = new Dictionary<StateId, StateInstance>();
            _stack = new IteratableStack<StateInstance>();

            Type type = typeof (T);
            _ownerMethods = GetMethodsWithAttribute(typeof (T), typeof (StateMethodAttribute));
            _ownerEvents = GetMatchingOwnerEvents(type, _ownerMethods);
        }

        public StateInstance AddState(StateId stateId, State state) {
            if (_states.ContainsKey(stateId)) {
                throw new ArgumentException(string.Format("StateId '{0}' is already registered.", stateId));
            }

            var instance = new StateInstance(stateId, state, GetImplementedStateMethods(state, _ownerMethods));
            _states.Add(stateId, instance);
            return instance;
        }

        public void Transition(StateId stateId, params object[] args) {
            StateInstance oldState = null;
            StateInstance newState = _states[stateId];

            if (_stack.Count > 0) {
                oldState = _stack.Peek();

                var isNormalTransition = oldState.Transitions.Contains(stateId);
                var isChildTransition = !isNormalTransition && oldState.ChildTransitions.Contains(stateId);

                if (!isNormalTransition && !isChildTransition) {
                    throw new Exception(string.Format(
                        "Transition from state '{0}' to state '{1}' is not registered, transition failed",
                        oldState.StateId,
                        stateId));
                }

                CallStateLifeCycleMethod(oldState.OnExit);

                if (isNormalTransition) {
                    _stack.Pop();
                }
            }

            _stack.Push(newState);
            SubscribeToStateMethods(oldState, newState);
            CallStateLifeCycleMethod(newState.OnEnter, args);
        }

        public void TransitionToParent() {
            if (_stack.Count <= 1) {
                throw new InvalidOperationException("Cannot transition to parent state, currently at top-level state");
            }

            var oldState = _stack.Pop();
            CallStateLifeCycleMethod(oldState.OnExit);
            SubscribeToStateMethods(oldState, _stack.Peek());
        }

        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static IList<MethodInfo> GetMethodsWithAttribute(Type ownerType, Type attributeType) {
            return ReflectionUtils.GetMethodsWithAttribute(ownerType, attributeType, Flags);
        }

        private IDictionary<string, EventInfo> GetMatchingOwnerEvents(Type ownerType, IEnumerable<MethodInfo> methods) {
            var stateEvents = new Dictionary<string, EventInfo>();

            var events = ownerType.GetEvents(Flags);
            foreach (var e in events) {
                foreach (var m in methods) {
                    if (e.Name.Equals("On" + m.Name)) {
                        stateEvents.Add(m.Name, e);
                    }
                }
            }

            return stateEvents;
        }

        private static IDictionary<string, Delegate> GetImplementedStateMethods(State state,
            IEnumerable<MethodInfo> ownerMethods) {
            var implementedMethods = new Dictionary<string, Delegate>();

            Type type = state.GetType();

            var stateMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var stateMethod in stateMethods) {
                foreach (var ownerMethod in ownerMethods) {
                    if (stateMethod.Name == ownerMethod.Name) {
                        var del = ReflectionUtils.ToDelegate(stateMethod, state);
                        implementedMethods.Add(ownerMethod.Name, del);
                    }
                }
            }

            return implementedMethods;
        }

        private void SubscribeToStateMethods(StateInstance oldState, StateInstance newState) {
            // Todo: is there an easier way to clear the list of subscribers?
            foreach (var pair in _ownerEvents) {
                // Unregister delegates of the old state
                if (oldState != null && oldState.StateDelegates.ContainsKey(pair.Key)) {
                    pair.Value.RemoveEventHandler(_owner, oldState.StateDelegates[pair.Key]);
                }
                // Register delegates of the new state
                if (newState.StateDelegates.ContainsKey(pair.Key)) {
                    pair.Value.AddEventHandler(_owner, newState.StateDelegates[pair.Key]);
                }
            }
        }

        public void CallStateLifeCycleMethod(Delegate del, params object[] args) {
            if (del == null) {
                return;
            }

            try {
                if (del.Method.ReturnType == typeof(IEnumerator)) {
                    _scheduler.Start((IEnumerator)del.DynamicInvoke(args));
                } else {
                    del.DynamicInvoke(args);
                }
            } catch (TargetParameterCountException e) {
                Debug.LogException(e);
                throw new ArgumentException(GetArgumentExceptionDetails((State)del.Target, del, args));
            }
        }

        private string GetArgumentExceptionDetails(State state, Delegate del, params object[] args) {
            var expectedArgs = del.Method.GetParameters();
            string expectedArgTypes = "";
            for (int i = 0; i < expectedArgs.Length; i++) {
                expectedArgTypes += expectedArgs[i].ParameterType.Name + (i < expectedArgs.Length - 1 ? ", " : "");
            }

            string receivedArgTypes = "";
            for (int i = 0; i < args.Length; i++) {
                receivedArgTypes += args[i].GetType().Name + (i < args.Length - 1 ? ", " : "");
            }
            return String.Format(
                "Wrong arguments for transition to state '{0}', expected: {1}; received: {2}",
                state.GetType(),
                expectedArgTypes,
                receivedArgTypes);
        }
    }

    public struct StateId {
        private readonly string _value;

        public StateId(string value) {
            _value = value;
        }

        public string Value {
            get { return _value; }
        }

        public bool Equals(StateId other) {
            return string.Equals(_value, other._value);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            return obj is StateId && Equals((StateId) obj);
        }

        public override int GetHashCode() {
            return (_value != null ? _value.GetHashCode() : 0);
        }

        public static bool operator ==(StateId left, StateId right) {
            return left.Equals(right);
        }

        public static bool operator !=(StateId left, StateId right) {
            return !left.Equals(right);
        }

        public override string ToString() {
            return _value;
        }
    }

    /// <summary>
    /// A state, plus metadata, which lives in the machine's stack
    /// </summary>
    /// Todo: only expose Permit interface to user, not the list of delegates etc.
    public class StateInstance {
        private readonly State _state;
        private readonly Delegate _onEnter;
        private readonly Delegate _onExit;
        private readonly IDictionary<string, Delegate> _stateDelegates;

        public Delegate OnEnter {
            get { return _onEnter; }
        }

        public Delegate OnExit {
            get { return _onExit; }
        }

        public StateInstance(StateId stateId, State state, IDictionary<string, Delegate> stateDelegates) {
            StateId = stateId;
            _state = state;
            _stateDelegates = stateDelegates;

            Transitions = new List<StateId>();
            ChildTransitions = new List<StateId>();

            _onEnter = GetDelegateByName(State, "OnEnter");
            _onExit = GetDelegateByName(State, "OnExit");
        }

        public StateId StateId { get; private set; }

        public State State {
            get { return _state; }
        }

        public IDictionary<string, Delegate> StateDelegates {
            get { return _stateDelegates; }
        }

        public IList<StateId> Transitions { get; private set; }
        public IList<StateId> ChildTransitions { get; private set; }

        public StateInstance Permit(StateId stateId) {
            Transitions.Add(stateId);
            return this;
        }

        public StateInstance PermitChild(StateId stateId) {
            ChildTransitions.Add(stateId);
            return this;
        }

        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static Delegate GetDelegateByName(State state, string name) {
            Type type = state.GetType();

            var method = type.GetMethod(name, Flags);
            if (method != null) {
                return ReflectionUtils.ToDelegate(method, state);
            }
            return null;
        }
    }

    public class State {
        protected IStateMachine Machine { get; private set; }

        public State(IStateMachine machine) {
            Machine = machine;
        }
    }
}