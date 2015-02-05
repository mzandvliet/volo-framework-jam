using System;
using System.Collections;
using System.Collections.Generic;
using RamjetAnvil.StateMachine;
using UnityEngine;

/* Test case:
 * - Start screen
 * - Menu
 * - Different levels
 * - Different gametypes
 * - Player, enemies
 * 
 * - Make a complex enemy, with multiple objects, and a weak point. This forces you to think about events as they happen
 * in hierarchies.
 * 
 * 
 * We can model a timed transition as a state. Camera transition is an interesting one.
 * Instead of saying a transition *is* a camera transition, we could say a transition could use a camera transition.
 */

public class Game : MonoBehaviour {
    [SerializeField] private GameObject _characterPrefab;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _cameraPositionStartScreen;
    [SerializeField] private Transform _cameraPositionGame;
    
    public static class States {
        public static readonly StateId StartScreen = new StateId("StartScreen");
        public static readonly StateId InGame = new StateId("InGame");
        public static readonly StateId InGame_Paused = new StateId("InGame_Paused");
        public static readonly StateId ScoreScreen = new StateId("ScoreScreen");
    }

    private CoroutineScheduler _scheduler;
    private StateMachine<Game> _machine;

    private void Start() {
        var inputDevices = new[] {new PlayerInputDevice()};

        _scheduler = new CoroutineScheduler();
        _machine = new StateMachine<Game>(this, _scheduler);

        _machine.AddState(States.StartScreen, new StartScreen(_machine, inputDevices, _cameraPositionStartScreen, _camera))
            .Permit(States.InGame);

        _machine.AddState(States.InGame, new InGame(_machine, _characterPrefab, _cameraPositionGame, _camera))
            .Permit(States.ScoreScreen)
            .PermitChild(States.InGame_Paused);

        _machine.AddState(States.InGame_Paused, new InGame.InGame_Paused(_machine));

        _machine.AddState(States.ScoreScreen, new ScoreScreen(_machine))
            .Permit(States.StartScreen);

        _machine.Transition(States.StartScreen);
    }

    #region State Methods

    [StateEvent("Update")]
    public event Action OnUpdate; 
    
    private void Update() {
        _scheduler.Update(Time.frameCount, Time.time);

        if (OnUpdate != null) {
            OnUpdate();
        }
    }

    [StateEvent("OnGUI")]
    public event Action OnGui;
    
    private void OnGUI() {
        if (OnGui != null) {
            OnGui();
        }
    }

    #endregion

    #region States

    private class StartScreen : State {
        private IList<PlayerInputDevice> _inputs;
        private Transform _camPos;
        private Camera _cam;

        public StartScreen(IStateMachine machine, IList<PlayerInputDevice> inputs, Transform camPos, Camera cam) : base(machine) {
            _inputs = inputs;
            _camPos = camPos;
            _cam = cam;
        }

        private IEnumerator OnEnter() {
            return Transitions.Transition(_cam, _camPos);
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
        private Transform _camPos;
        private PlayerInputDevice _input;
        private Character _player;
        private IList<Character> _enemies;

        private float _startTime;
        private int _score;

        public InGame(IStateMachine machine, GameObject characterPrefab, Transform camPos, Camera camera) : base(machine) {
            _characterPrefab = characterPrefab;
            _camera = camera;
            _camPos = camPos;
            _enemies = new List<Character>();
        }

        IEnumerator OnEnter(PlayerInputDevice input) {
            _input = input;
            _startTime = Time.time;
            _score = 0;

            yield return Transitions.Transition(_camera, _camPos);

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

                enemyObject.GetComponent<Health>().OnDied += OnEnemyKilled;

                _enemies.Add(enemyCharacter);
            }
        }

        private void OnEnemyKilled(Health health) {
            var enemy = health.GetComponent<Character>();
            Destroy(enemy.gameObject);
            _enemies.Remove(enemy);
            
            _score ++;

            if (_enemies.Count == 0) {
                Machine.Transition(States.ScoreScreen, _score, _input);
            }
        }

        private void Update() {
            if (_input.GetKeyDown(KeyCode.Escape)) {
                Machine.Transition(States.InGame_Paused, _input);
            }

            if (Time.time - _startTime > 30f) {
                Machine.Transition(States.ScoreScreen, 1234, _input);
            }
        }

        private void OnGUI() {
            GUI.Label(new Rect(Screen.width * 0.05f, Screen.height * 0.05f, 200f, 50f), "====== Score: " + _score + " ======");
        }

        private void OnExit() {
            Destroy(_player.gameObject);

            for (int i = 0; i < _enemies.Count; i++) {
                Destroy(_enemies[i].gameObject);
            }

            _enemies.Clear();
        }

        public class InGame_Paused : State {
            private PlayerInputDevice _input;

            public InGame_Paused(IStateMachine machine) : base(machine) { }

            private void OnEnter(PlayerInputDevice input) {
                _input = input;
                Time.timeScale = 0f;
            }

            private void Update() {
                if (_input.GetKeyDown(KeyCode.Escape)) {
                    Machine.TransitionToParent();
                }
            }

            private void OnGUI() {
                GUI.Label(new Rect(Screen.width * 0.5f, Screen.height * 0.5f, 200f, 50f), "Paused");
            }

            private void OnExit() {
                Time.timeScale = 1f;
            }
        }
    }

    private class ScoreScreen : State {
        private int _score;
        private PlayerInputDevice _input;
        
        public ScoreScreen(IStateMachine machine) : base(machine) {}

        private void OnEnter(int score, PlayerInputDevice input) {
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

    #region Transitions

    public static class Transitions {
        public static IEnumerator Transition(Camera camera, Transform target) {
            const float duration = 1f;

            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;

            float time = 0f;
            while (time < duration) {
                float lerp = time/duration;
                camera.transform.position = Vector3.Lerp(originalPosition, target.position, lerp);
                camera.transform.rotation = Quaternion.Lerp(originalRotation, target.rotation, lerp);
                time += Time.deltaTime;
                yield return new WaitFrames();
            }
            camera.transform.position = target.position;
            camera.transform.rotation = target.rotation;
        }
    }

    #endregion
}