﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

/*
 * Todo:
 * 
 * The owner's events should really just be references to delegates, I think. No need for multicast.
 * 
 * Support Coroutines?
 * 
 * Special handling for parent->child and child-parent relationships?
 */
namespace RamjetAnvil.Unity.Utils {

    /*
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

    [AttributeUsage(AttributeTargets.Method)]
    public class StateMethodAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OnEnterArgument : Attribute {
        public string Name { get; private set; }
        public Type Type { get; private set; }

        public OnEnterArgument(string name, Type type) {
            Name = name;
            Type = type;
        }
    }

    public interface IStateMachine {
        void Transition(StateId stateId, IDictionary<string, object> args);
        void TransitionToParent();
    }

    public class StateMachine<T> : IStateMachine {
        private readonly T _owner;
        private readonly IDictionary<StateId, StateInstance> _states;
        private readonly IteratableStack<StateInstance> _stack;

        private readonly IList<MethodInfo> _ownerMethods;
        private readonly IDictionary<string, EventInfo> _ownerEvents;

        public StateMachine(T owner) {
            _owner = owner;

            _states = new Dictionary<StateId, StateInstance>();
            _stack = new IteratableStack<StateInstance>();

            Type type = typeof (T);
            _ownerMethods = GetMethodsWithAttribute(typeof(T), typeof(StateMethodAttribute));
            _ownerEvents = GetMatchingOwnerEvents(type, _ownerMethods);
        }

        public void Start(StateId stateId, IDictionary<string, object> args) {
            StateInstance instance = _states[stateId];
            _stack.Push(instance);

            SetOwnerDelegates(null, instance);
            VerifyOnEnterArguments(instance, args);
            instance.State.OnEnter(args);
        }

        public StateInstance AddState(StateId stateId, State state) {
            if (_states.ContainsKey(stateId)) {
                throw new ArgumentException(string.Format("StateId {0} is already registered.", stateId));
            }

            var instance = new StateInstance(stateId, state, GetImplementedStateMethods(state, _ownerMethods));
            _states.Add(stateId, instance);
            return instance;
        }

        public void Transition(StateId stateId, IDictionary<string, object> args) {
            var oldState = _stack.Peek();

            var isNormalTransition = oldState.Transitions.Contains(stateId);
            var isChildTransition = !isNormalTransition && oldState.ChildTransitions.Contains(stateId);
            if (isNormalTransition) {
                oldState.State.OnExit();
                _stack.Pop();
            } else if (isChildTransition) {

            } else {
                throw new Exception(string.Format("Transition to state '{0}' is not registered, transition failed", stateId));
            }

            var newState = _states[stateId];
            _stack.Push(newState);

            SetOwnerDelegates(oldState, newState);
            VerifyOnEnterArguments(newState, args);
            newState.State.OnEnter(args);
        }

        public void TransitionToParent() {
            if (_stack.Count <= 1) {
                throw new InvalidOperationException("Cannot transition to parent state, currently at top-level state");
            }

            _stack.Pop().State.OnExit();
        }

        private void VerifyOnEnterArguments(StateInstance instance, IDictionary<string, object> args) {
            var requiredArgs = GetOnEnterArguments(instance.State);
            foreach (var pair in requiredArgs) {
                if (!args.ContainsKey(pair.Key)) {
                    throw new ArgumentException(string.Format("Required OnEnter argument '{0}' is missing", pair.Key));
                }
                if (pair.Value != args[pair.Key].GetType()) {
                    throw new ArgumentException(string.Format("Required OnEnter argument '{0}' is of wrong type. Expected: {1}", pair.Key, pair.Value));
                }
            }
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

        private static IDictionary<string, Delegate> GetImplementedStateMethods(State state, IEnumerable<MethodInfo> ownerMethods) {
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

        private static IDictionary<string, Type> GetOnEnterArguments(State state) {
            var arguments = new Dictionary<string, Type>();

            Type type = state.GetType();

            var method = type.GetMethod("OnEnter");
            if (method != null) {
                foreach (var a in method.GetCustomAttributes(typeof(OnEnterArgument), false)) {
                    var attribute = (OnEnterArgument) a;
                    arguments.Add(attribute.Name, attribute.Type);
                }
            }

            return arguments;
        }

        private void SetOwnerDelegates(StateInstance oldState, StateInstance newState) {
            foreach (var pair in _ownerEvents) {
                if (oldState != null && oldState.StateDelegates.ContainsKey(pair.Key)) {
                    pair.Value.RemoveEventHandler(_owner, oldState.StateDelegates[pair.Key]);
                }
                if (newState.StateDelegates.ContainsKey(pair.Key)) {
                    pair.Value.AddEventHandler(_owner, newState.StateDelegates[pair.Key]);
                }
            }
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
    public class StateInstance { 
        private readonly State _state;
        private readonly IDictionary<string, Delegate> _stateDelegates;

        public StateInstance(StateId stateId, State state, IDictionary<string, Delegate> stateDelegates) {
            StateId = stateId;
            _state = state;
            _stateDelegates = stateDelegates;

            Transitions = new List<StateId>();
            ChildTransitions = new List<StateId>();
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
    }

    
    /* Todo: Try to achieve some type safety for OnEnter
     * 
     * Maybe make State<T, U, V> overloads, which define arguments passed into OnEnter, or try 
     * a reflection approach similar to the StateMethod linking.
     * 
     * A readable runtime argument exception is better than a random failure somewhere down the line. 
     */

    public class State {
        protected IStateMachine Machine { get; private set; }
        public State(IStateMachine machine) {
            Machine = machine;
        }

        public virtual void OnEnter(IDictionary<string, object> data) {
        }

        public virtual void OnExit() {
        }
    }

    public class IteratableStack<T> {
        private IList<T> _stack;

        public int Count {
            get { return _stack.Count; }
        }

        public IteratableStack() {
            _stack = new List<T>();
        }

        public IteratableStack(int capacity) {
            _stack = new List<T>(capacity);
        }

        public IteratableStack(IEnumerable<T> collection) {
            _stack = new List<T>(collection);
        }

        public void Clear() {
            _stack.Clear();
        }

        public bool Contains(T item) {
            return _stack.Contains(item);
        }

        public T Peek() {
            return _stack[LastIndex()];
        }

        public T Pop() {
            var removed = _stack[LastIndex()];
            _stack.RemoveAt(LastIndex());
            return removed;
        }

        public void Push(T item) {
            _stack.Add(item);
        }

        public T this[int i] {
            get { return _stack[i]; }
        }

        private int LastIndex() {
            return _stack.Count - 1;
        }
    }
}