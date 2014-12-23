using System;
using System.Collections.Generic;

/*
 * ----------- Architecture issues: --------
 * 
 * One monolithic statemachine is as evil as a giant inheritance tree. Have tiny state machines per aspect, like components,
 * related through loose coupling.
 * 
 * Remove event propagation from the machines.
 * 
 * MonoBehaviours as StateMachines
 * 
 * Can we have a monobehaviour react differently to events, input, collision, by using a state machine? Hope so!
 * In addition, a character machine can change an object hierarchy depending on state.
 * 
 * A good helper would be an optional StateMachineBehaviour to inherit from, with states catching all relevant
 * callbacks from the MonoBehaviour interface. Hmm, in that case it makes sense to use reflection to find implemented
 * callbacks, if any exist.
 * 
 * --------- Dependency issues ---------
 * 
 * A state wants to make side effects. It needs access to things like a spawner, a camera system, etc. Inject them?
 * 
 * SERVICE LOCATOR IS SHIT. You can't make a scene with just a camera system, as having a service locator
 * requires all other services for that locator to be loaded.
 * 
 * --------- Mods ---------------
 * 
 * Since we're on the subject of global architecture now, we might also want to start thinking about how
 * mods would work, how they are loaded and how they interact with the core of the game, and with eachother.
 * 
 * My guess is the core of Volo should just be like any other mod in the ecosystem, but loaded by default.
 * 
 * ---------- Parenting issues ---------
 * 
 * - Are parents considered active, while a child is at the top of the stack?
 * 
 * - If you transition back to parent, parent needs to know (respond to it)
 * 
 * Suppose a child could exit and pass optional data along to the parent? And a child could be sent
 * optional data when it is entered?
 * 
 * - If you have a hierarchy of 10 nested states, and all of them are active: bugs
 * 
 * - Is it always possible to transition to a parent from any child? Or do we say some child
 * states cannot exit to their parent? (Stateless has explicit permitions for parent transitions)
 * 
 * Transition modeling
 * 
 * - Maybe timed transitions are just states with a single entry and exit state, and a timer?
 *
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

        public static StateMachine Build(this StateMachineConfig config, object initialStateData) {
            if (config.InitialState == null) {
                throw new Exception("No initial state configured, cannot instantiate state machine");
            }
            return new StateMachine(config, initialStateData);
        }
    }

    public class StateMachine {
        private readonly StateMachineConfig _config;
        private readonly IteratableStack<StateInstance> _stack;

        public StateMachine(StateMachineConfig config, object initialStateData) {
            _config = config;
            _stack = new IteratableStack<StateInstance>();

            var initialStateConfig = _config.States[_config.InitialState.Value];
            var state = CreateState(initialStateConfig.StateType);
            _stack.Push(new StateInstance(initialStateConfig, state));
            state.OnEnter(this, initialStateData);
        }

        public void Transition(StateId stateId, object data) {
            var currentState = _stack.Peek();

            var isNormalTransition = currentState.Config.Transitions.Contains(stateId);
            var isChildTransition = currentState.Config.ChildTransitions.Contains(stateId);
            if (isNormalTransition) {
                currentState.State.OnExit();
                _stack.Pop();

                var newStateConfig = _config.States[stateId];

                var newStateInstance = CreateState(newStateConfig.StateType);
                _stack.Push(new StateInstance(newStateConfig, newStateInstance));
                newStateInstance.OnEnter(this, data);
            } else if (isChildTransition) {
                var newStateConfig = _config.States[stateId];

                var newStateInstance = CreateState(newStateConfig.StateType);
                _stack.Push(new StateInstance(newStateConfig, newStateInstance));
                newStateInstance.OnEnter(this, data);
            }
            else {
                throw new Exception(string.Format("Transition to state '{0}' is not registered, transition failed", stateId));
            }
        }

        public void TransitionToParent() {
            if (_stack.Count <= 1) {
                throw new InvalidOperationException("Cannot transition to parent state, currently at top-level state");
            }

            _stack.Pop().State.OnExit();
            // _stack.Peek().OnChildExited(stateId, data)
        }

        // Todo: Create states based on Type with this, instead of caching instances created externally
        private static IState CreateState(Type stateType) {
            return (IState)Activator.CreateInstance(stateType);
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

    public class StateInstance { // in stack
        private readonly StateConfig _config;
        private readonly IState _state;

        public StateInstance(StateConfig config, IState state) {
            _config = config;
            _state = state;
        }

        public StateConfig Config {
            get { return _config; }
        }

        public IState State {
            get { return _state; }
        }
    }

    public interface IState {
        void OnEnter(StateMachine machine, object data); // object might not be handiest type for passing multiple arguments
        void OnExit();
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