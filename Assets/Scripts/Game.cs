using System.Collections;
using System.Collections.Generic;
using RamjetAnvil.Unity.Utils;
using UnityEngine;

public class Game : MonoBehaviour {
    [SerializeField] private GameObject _characterPrefab;
    [SerializeField] private GameObject _projectilePrefab;
    
    public static class States {
        public static readonly StateId StartScreen = new StateId("StartScreen");
        public static readonly StateId InGame = new StateId("InGame");
        public static readonly StateId Options = new StateId("Options");
    }

    private StateMachine _machine;

    private void Start() {
        var gameMachineConfig = new StateMachineConfig();

        gameMachineConfig.AddState(States.StartScreen, typeof(StartScreen))
            .Permit(States.InGame);

        gameMachineConfig.AddState(States.InGame, typeof(InGame))
            .Permit(States.StartScreen)
            .PermitChild(States.Options);

        gameMachineConfig.AddState(States.Options, typeof(Options));

        gameMachineConfig.SetInitialState(States.StartScreen);

        _machine = gameMachineConfig.Build(null);
    }

    private class StartScreen : IState {
        private GameObject _startScreen;

        public void OnEnter(StateMachine machine, object data) {
            // Wait for player to press start
        }

        public void OnExit() {
            Destroy(_startScreen);
        }
    }

    private class InGame : IState {
        private Character _player;
        private IList<Character> _enemies;

        public InGame() {
            _enemies = new List<Character>();
        }

        public void OnEnter(StateMachine machine, object data) {
            // Spawn player, enemies, register for relevant events
        }

        public void OnExit() {
        }
    }

    private class Options : IState {
        public void OnEnter(StateMachine machine, object data) {

        }

        public void OnExit() {
        }
    }
}