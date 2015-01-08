using System;
using System.CodeDom;
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
    [SerializeField] private Camera _playerCamera;
    
    public static class States {
        public static readonly StateId StartScreen = new StateId("StartScreen");
        public static readonly StateId InGame = new StateId("InGame");
        public static readonly StateId ScoreScreen = new StateId("ScoreScreen");
    }

    private StateMachine<Game> _machine;

    private void Start() {
        var inputDevices = new[] {new PlayerInputDevice()};

        _machine = new StateMachine<Game>(this);

        _machine.AddState(States.StartScreen, new StartScreen(_machine, inputDevices))
            .Permit(States.InGame);

        _machine.AddState(States.InGame, new InGame(_machine, _characterPrefab, _playerCamera))
            .Permit(States.StartScreen)
            .Permit(States.ScoreScreen);

        _machine.AddState(States.ScoreScreen, new ScoreScreen(_machine))
            .Permit(States.StartScreen);

        _machine.Start(States.StartScreen);
    }

    #region State Methods

    public event Action OnUpdate; 
    [StateMethod]
    private void Update() {
        if (OnUpdate != null) {
            OnUpdate();
        }
    }

    public event Action OnOnGUI;
    [StateMethod]
    private void OnGUI() {
        if (OnOnGUI != null) {
            OnOnGUI();
        }
    }

    #endregion

    #region States

    private class StartScreen : State {
        private IList<PlayerInputDevice> _inputs;

        public StartScreen(IStateMachine machine, IList<PlayerInputDevice> inputs) : base(machine) {
            _inputs = inputs;
        }

        private void Update() {
            for (int i = 0; i < _inputs.Count; i++) {
                var input = _inputs[i];
                if (input.AnyKeyDown()) {
                    Machine.Transition(States.InGame, input);
                }
            }
        }

        private void OnGUI() {
            GUI.Label(new Rect(Screen.width * 0.5f, Screen.height * 0.5f, 200f, 50f), "Press any key to begin");
        }
    }

    private class InGame : State {
        private GameObject _characterPrefab;
        private Camera _camera;
        private PlayerInputDevice _input;
        private Character _player;
        private IList<Character> _enemies;

        private float _startTime;

        public InGame(IStateMachine machine, GameObject characterPrefab, Camera camera) : base(machine) {
            _characterPrefab = characterPrefab;
            _camera = camera;
            _enemies = new List<Character>();
        }

        public void OnEnter(PlayerInputDevice input) {
            _input = input;
            _startTime = Time.time;
            
            SpawnPlayer(input);
            SpawnEnemies();
        }

        private void SpawnPlayer(PlayerInputDevice input) {
            var playerObject = (GameObject)Instantiate(_characterPrefab);
            var playerCharacter = playerObject.GetComponent<Character>();
            var controller = playerObject.AddComponent<PlayerCharacterController>();
            controller.Character = playerObject.GetComponent<Character>();
            controller.Input = input;
            controller.Camera = _camera;

            playerCharacter.WalkSpeed = 10f; // Todo: separate prefab

            _player = playerCharacter;
        }

        private void SpawnEnemies() {
            Vector3 baseSpawn = new Vector3(-5f, 5f, 0f);

            for (int i = 0; i < 10; i++) {
                Vector3 spawnPoint = baseSpawn + Vector3.right*(float) i;
                var enemyObject = (GameObject)Instantiate(_characterPrefab, spawnPoint, Quaternion.identity);
                var enemyCharacter = enemyObject.GetComponent<Character>();
                var controller = enemyObject.AddComponent<AiCharacterController>();
                controller.Character = enemyCharacter;
                controller.Target = _player;

                _enemies.Add(enemyCharacter);
            }
        }

        private void Update() {
            if (Time.time - _startTime > 5f) {
                Machine.Transition(States.ScoreScreen, 1234, _input);
            }
        }

        private void OnGUI() {
            GUI.Label(new Rect(Screen.width * 0.05f, Screen.height * 0.05f, 200f, 50f), "====== Score: " + 1234 + " ======");
        }

        public void OnExit() {
            Destroy(_player.gameObject);

            for (int i = 0; i < _enemies.Count; i++) {
                Destroy(_enemies[i].gameObject);
            }
            _enemies.Clear();
        }
    }

    private class ScoreScreen : State {
        private int _score;
        private PlayerInputDevice _input;
        
        public ScoreScreen(IStateMachine machine) : base(machine) {}

        public void OnEnter(int score, PlayerInputDevice input) {
            _score = score;
            _input = input;
        }

        private void Update() {
            if (_input.AnyKeyDown()) {
                Machine.Transition(States.StartScreen, null);
            }
        }

        private void OnGUI() {
            GUI.Label(new Rect(Screen.width * 0.5f, Screen.height * 0.5f, 200f, 50f), "====== Score: " + _score + " ======");
        }
    }

    #endregion
}