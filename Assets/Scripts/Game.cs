using System;
using System.Collections.Generic;
using RamjetAnvil.Unity.Utils;
using UnityEngine;

/* Test case:
 * - Start screen
 * - Menu
 * - Different levels
 * - Different gametypes
 * - Player, enemies
 */

public class Game : MonoBehaviour {
    [SerializeField] private GameObject _characterPrefab;
    [SerializeField] private GameObject _projectilePrefab;
    
    public static class States {
        public static readonly StateId StartScreen = new StateId("StartScreen");
        public static readonly StateId InGame = new StateId("InGame");
        public static readonly StateId Options = new StateId("Options");
    }

    private StateMachine<Game> _machine;

    private void Start() {
        _machine = new StateMachine<Game>(this);

        _machine.AddState(States.StartScreen, new StartScreen(_machine))
            .Permit(States.InGame);

        _machine.AddState(States.InGame, new InGame(_machine))
            .Permit(States.StartScreen);

         var inputDevices = new[] {new PlayerInputDevice()};

        _machine.Start(States.StartScreen, inputDevices);
    }

    #region State Methods

    protected event Action OnUpdate; 
    [StateMethod]
    private void Update() {
        if (OnUpdate != null) {
            OnUpdate();
        }
    }

    protected event Func<int, bool> OnVerifyPlayer;
    [StateMethod]
    private bool VerifyPlayer(int playerIndex) {
        if (OnVerifyPlayer != null) {
            return OnVerifyPlayer(playerIndex);
        }
        return false;
    }

    #endregion

    #region States

    private class StartScreen : State {
        private IList<PlayerInputDevice> _inputs;

        public StartScreen(IStateMachine machine) : base(machine) {}

        public override void OnEnter(object data) {
            Debug.Log("StartScreen_OnEnter");
            _inputs = (IList<PlayerInputDevice>) data;
        }

        private void Update() {
            Debug.Log("StartScreen_Update");
            for (int i = 0; i < _inputs.Count; i++) {
                var input = _inputs[i];
                if (input.AnyKeyDown()) {
                    Machine.Transition(States.InGame, new InGame.EnterData() {});
                }
            }
        }

        public override void OnExit() {
            Debug.Log("StartScreen_OnExit");
        }
    }

    private class InGame : State {
        private Character _player;
        private IList<Character> _enemies;

        public InGame(IStateMachine machine) : base(machine) {
            _enemies = new List<Character>();
        }

        public override void OnEnter(object data) {
            Debug.Log("InGame_OnEnter");
            var onEnterData = (EnterData) data;

            /* Todo:
             * - create enemies, and character with given input device
             * - register for appropriate events
             */

            var player = Instantiate(onEnterData.CharacterPrefab);
            
        }

        private void Update() {
            Debug.Log("InGame_Update");
        }

        public override void OnExit() {
            Debug.Log("InGame_OnExit");
        }

        public class EnterData {
            public PlayerInputDevice Input;
            public GameObject CharacterPrefab;
        }
    }

    #endregion
}