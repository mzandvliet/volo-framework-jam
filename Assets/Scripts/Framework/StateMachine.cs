using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

/*
 * Todo:
 * 
 * The owner's events should really just be references to delegates, I think. No need for multicast.
 * 
 * Support Coroutines?
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

    public class StateMethodAttribute : Attribute { }

    public interface IStateMachine {
        void Transition(StateId stateId, IDictionary<string, object> data);
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

        public void Start(StateId stateId, IDictionary<string, object> data) {
            StateInstance instance = _states[stateId];
            _stack.Push(instance);

            SetOwnerDelegates(null, instance);
            instance.State.OnEnter(data);
        }

        public StateInstance AddState(StateId stateId, State state) {
            var instance = new StateInstance(stateId, state, GetImplementedStateMethods(state, _ownerMethods));
            _states.Add(stateId, instance);
            return instance;
        }

        public void Transition(StateId stateId, IDictionary<string, object> data) {
            var oldState = _stack.Peek();

            // Todo: better handling of parent-to-child transition. It's not onexit, but things do change for the parent

            var isNormalTransition = oldState.Transitions.Contains(stateId);
            var isChildTransition = oldState.ChildTransitions.Contains(stateId);
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
            newState.State.OnEnter(data);
        }

        public void TransitionToParent() {
            if (_stack.Count <= 1) {
                throw new InvalidOperationException("Cannot transition to parent state, currently at top-level state");
            }

            _stack.Pop().State.OnExit();
            // _stack.Peek().OnChildExited(stateId, data)
        }

        private static IList<MethodInfo> GetMethodsWithAttribute(Type ownerType, Type type) {
            var methods = ownerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var stateMethods = new List<MethodInfo>();
            foreach (var m in methods) {
                var attributes = m.GetCustomAttributes(type, false);
                if (attributes.Length > 0) {
                    UnityEngine.Debug.Log("Found owner method: " + m.Name);
                    stateMethods.Add(m);
                }
            }
            return stateMethods;
        }

        private IDictionary<string, EventInfo> GetMatchingOwnerEvents(Type ownerType, IEnumerable<MethodInfo> methods) {
            var stateEvents = new Dictionary<string, EventInfo>();

            var events = ownerType.GetEvents(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var e in events) {
                foreach (var m in methods) {
                    UnityEngine.Debug.Log(m.Name);
                    if (e.Name.Equals("On" + m.Name)) {
                        stateEvents.Add(m.Name, e);
                        UnityEngine.Debug.Log("Found owner event: " + e.Name);
                    }
                }
            }

            return stateEvents;
        }

        private static IDictionary<string, Delegate> GetImplementedStateMethods(State state, IEnumerable<MethodInfo> ownerMethods) {
            var implementedMethods = new Dictionary<string, Delegate>();

            Type type = state.GetType();

            var stateMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var mS in stateMethods) {
                foreach (var mO in ownerMethods) {
                    if (mS.Name == mO.Name) {
                        var del = ToDelegate(mS, state);
                        implementedMethods.Add(mO.Name, del);
                        UnityEngine.Debug.Log("Found state delegate: " + mS.Name);
                    }
                }
            }

            return implementedMethods;
        }

        /// <summary>
        /// Builds a Delegate instance from the supplied MethodInfo object and a target to invoke against.
        /// </summary>
        private static Delegate ToDelegate(MethodInfo methodInfo, object target) {
            if (methodInfo == null) throw new ArgumentNullException("methodInfo");

            Type delegateType;

            var typeArgs = methodInfo.GetParameters()
                .Select(p => p.ParameterType)
                .ToList();

            // builds a delegate type
            if (methodInfo.ReturnType == typeof(void)) {
                delegateType = Expression.GetActionType(typeArgs.ToArray());
            } else {
                typeArgs.Add(methodInfo.ReturnType);
                delegateType = Expression.GetFuncType(typeArgs.ToArray());
            }

            // creates a binded delegate if target is supplied
            var result = (target == null)
                ? Delegate.CreateDelegate(delegateType, methodInfo)
                : Delegate.CreateDelegate(delegateType, target, methodInfo);

            return result;
        }

        private void SetOwnerDelegates(StateInstance oldState, StateInstance newState) {
            foreach (var pair in _ownerEvents) {
                if (oldState != null && oldState.StateDelegates.ContainsKey(pair.Key)) {
                    pair.Value.RemoveEventHandler(_owner, oldState.StateDelegates[pair.Key]);
                }
                if (newState.StateDelegates.ContainsKey(pair.Key)) {
                    pair.Value.AddEventHandler(_owner, newState.StateDelegates[pair.Key]);
                    UnityEngine.Debug.Log("Hooking up state event: " + newState.StateDelegates[pair.Key]);
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

    
    /* Todo: Maybe make State<T, U, V> overloads, which define arguments passed into OnEnter, or try 
     * a reflection approach similar to the StateMethod linking. */

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