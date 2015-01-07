using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

/*
 * Todo:
 * 
 * The owner's events should really just be references to delegates, I think. No need for multicast.
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

    public class StateMachineConfig {

        private readonly IDictionary<StateId, StateConfig> _states;
        private StateId? _initialState;

        public StateMachineConfig() {
            _states = new Dictionary<StateId, StateConfig>();
            _initialState = null;
        }

        public StateConfig AddState(StateId stateId, Type type) {
            var stateConfig = new StateConfig(stateId, type);
            _states.Add(stateId, stateConfig);
            return stateConfig;
        }

        public StateMachineConfig SetInitialState(StateId initialState) {
            if (!_states.ContainsKey(initialState)) {
                throw new Exception("Configuring an initial state: " + initialState + " that does not exist");
            }
            _initialState = initialState;
            return this;
        }

        public IDictionary<StateId, StateConfig> States {
            get { return _states; }
        }

        public StateId? InitialState {
            get { return _initialState; }
        }
    }

    public static class StateMachineConfigExtensions {

        public static StateMachine<T> Build<T>(this StateMachineConfig config, T owner, object initialStateData) {
            if (config.InitialState == null) {
                throw new Exception("No initial state configured, cannot instantiate state machine");
            }
            return new StateMachine<T>(owner, config, initialStateData);
        }
    }

    public interface IStateMachine {
        void Transition(StateId stateId, object data);
        void TransitionToParent();
    }

    public class StateMachine<T> : IStateMachine {
        private readonly T _owner;
        private readonly StateMachineConfig _config;
        private readonly IteratableStack<StateInstance> _stack;

        private readonly IList<MethodInfo> _ownerMethods;
        private readonly IDictionary<string, EventInfo> _ownerEvents;

        public StateMachine(T owner, StateMachineConfig config, object initialStateData) {
            _owner = owner;
            _config = config;
            _stack = new IteratableStack<StateInstance>();

            Type type = typeof (T);
            _ownerMethods = FindOwnerMethods(type);
            _ownerEvents = FindOwnerEvents(type, _ownerMethods);
            
            var initialStateConfig = _config.States[_config.InitialState.Value];
            var stateInstance = CreateStateInstance(initialStateConfig, _ownerMethods, this);
            _stack.Push(stateInstance);
            SetOwnerDelegates(null, stateInstance);
            stateInstance.State.OnEnter(initialStateData);
        }

        public void Transition(StateId stateId, object data) {
            var oldStateInstance = _stack.Peek();

            // Todo: better handling of parent-to-child transition. It's not onexit, but things do change for the parent

            var isNormalTransition = oldStateInstance.Config.Transitions.Contains(stateId);
            var isChildTransition = oldStateInstance.Config.ChildTransitions.Contains(stateId);
            if (isNormalTransition) {
                oldStateInstance.State.OnExit();
                _stack.Pop();
            } else if (isChildTransition) {

            } else {
                throw new Exception(string.Format("Transition to state '{0}' is not registered, transition failed", stateId));
            }

            var newStateConfig = _config.States[stateId];
            var newStateInstance = CreateStateInstance(newStateConfig, _ownerMethods, this);
            _stack.Push(newStateInstance);

            SetOwnerDelegates(oldStateInstance, newStateInstance);

            newStateInstance.State.OnEnter(data);
        }

        public void TransitionToParent() {
            if (_stack.Count <= 1) {
                throw new InvalidOperationException("Cannot transition to parent state, currently at top-level state");
            }

            _stack.Pop().State.OnExit();
            // _stack.Peek().OnChildExited(stateId, data)
        }

        private static IList<MethodInfo> FindOwnerMethods(Type ownerType) {
            var methods = ownerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var stateMethods = new List<MethodInfo>();
            foreach (var m in methods) {
                var attributes = m.GetCustomAttributes(typeof (StateMethodAttribute), false);
                if (attributes.Length > 0) {
                    UnityEngine.Debug.Log("Found owner method: " + m.Name);
                    stateMethods.Add(m);
                }
            }
            return stateMethods;
        }

        private IDictionary<string, EventInfo> FindOwnerEvents(Type ownerType, IEnumerable<MethodInfo> methods) {
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

        private static IDictionary<string, Delegate> GetImplementedStateDelegates(State state, IEnumerable<MethodInfo> ownerMethods) {
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
        private static Delegate ToDelegate(MethodInfo mi, object target) {
            if (mi == null) throw new ArgumentNullException("Failed to construct delegate, Method Info is null");

            Type delegateType;

            var typeArgs = mi.GetParameters()
                .Select(p => p.ParameterType)
                .ToList();

            // builds a delegate type
            if (mi.ReturnType == typeof(void)) {
                delegateType = Expression.GetActionType(typeArgs.ToArray());

            } else {
                typeArgs.Add(mi.ReturnType);
                delegateType = Expression.GetFuncType(typeArgs.ToArray());
            }

            // creates a binded delegate if target is supplied
            var result = (target == null)
                ? Delegate.CreateDelegate(delegateType, mi)
                : Delegate.CreateDelegate(delegateType, target, mi);

            return result;
        }

        private static StateInstance CreateStateInstance(StateConfig config, IEnumerable<MethodInfo> ownerMethods, IStateMachine machine) {
            var state = (State)Activator.CreateInstance(config.StateType, machine);
            var delegates = GetImplementedStateDelegates(state, ownerMethods);
            return new StateInstance(config, state, delegates);
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

    public class StateConfig {
        public StateId StateId { get; private set; }
        public Type StateType { get; private set; }
        public IList<StateId> Transitions { get; private set; }
        public IList<StateId> ChildTransitions { get; private set; }

        public StateConfig(StateId stateId, Type type) {
            StateId = stateId;
            StateType = type;
            Transitions = new List<StateId>();
            ChildTransitions = new List<StateId>();
        }

        public StateConfig Permit(StateId stateId) {
            Transitions.Add(stateId);
            return this;
        }

        public StateConfig PermitChild(StateId stateId) {
            ChildTransitions.Add(stateId);
            return this;
        }
    }

    /// <summary>
    /// A state, plus metadata, which lives in the machine's stack
    /// </summary>
    public class StateInstance { 
        private readonly StateConfig _config;
        private readonly State _state;
        private readonly IDictionary<string, Delegate> _stateDelegates;

        public StateInstance(StateConfig config, State state, IDictionary<string, Delegate> stateDelegates) {
            _config = config;
            _state = state;
            _stateDelegates = stateDelegates;
        }

        public StateConfig Config {
            get { return _config; }
        }

        public State State {
            get { return _state; }
        }

        public IDictionary<string, Delegate> StateDelegates {
            get { return _stateDelegates; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// Todo:
    /// - Could remove this interface and find implementations through reflection
    /// - Also, this means boilerplate in implementations (caching the machine ref)
    /// - object might not be handiest type for passing multiple arguments, but are by far the most flexible
    /*public interface IState {
        void OnEnter(object data); 
        void OnExit();
    }*/

    public class State {
        protected IStateMachine Machine { get; private set; }
        public State(IStateMachine machine) {
            Machine = machine;
        }

        public virtual void OnEnter(object data) {
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