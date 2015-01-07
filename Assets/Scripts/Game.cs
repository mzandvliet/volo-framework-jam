using System;
using System.Collections;
using System.Collections.Generic;
using RamjetAnvil.Unity.Utils;
using UnityEngine;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CSharp;

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
        var gameMachineConfig = new StateMachineConfig();

        gameMachineConfig.AddState(States.StartScreen, typeof(StartScreen))
            .Permit(States.InGame);


        gameMachineConfig.AddState(States.InGame, typeof (InGame))
            .Permit(States.StartScreen);
            //.PermitChild(States.Options);

        /*
        gameMachineConfig.AddState(States.Options, typeof(Options));
         */

        gameMachineConfig.SetInitialState(States.StartScreen);

        var inputDevices = new[] {new PlayerInputDevice()};

        _machine = gameMachineConfig.Build<Game>(this, inputDevices);
    }

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
                    Machine.Transition(States.InGame, input);
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

    /*
    private class Options : IState {
        public void OnEnter(StateMachine machine, object data) {

        }

        public void OnExit() {
        }
    }*/

    //private static void Create() {
    //    var compileUnit = new CodeCompileUnit();
    //    var myNamespace = new CodeNamespace("MyNamespace");
    //    var myClass = new CodeTypeDeclaration("MyClass");
    //    var method = new CodeMemberMethod();
    //    method.Name = "MyMethod";
    //    method.ReturnType = new CodeTypeReference("System.String");
    //    method.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "text"));
    //    method.Statements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("text")));
    //    myClass.Members.Add(method);
    //    myNamespace.Types.Add(myClass);
    //    compileUnit.Namespaces.Add(myNamespace);

    //    var provider = new CSharpCodeProvider();
    //    var cp = new CompilerParameters();
    //    cp.GenerateExecutable = false;
    //    cp.GenerateInMemory = true;

    //    var cr = provider.CompileAssemblyFromDom(cp, compileUnit);

    //    //var myType = cr.CompiledAssembly.GetType("MyNamespace.MyClass");
    //    //var instance = Activator.CreateInstance(myType);
    //    var instance = cr.CompiledAssembly.CreateInstance("MyNamespace.MyClass");
    //    var myType = instance.GetType();
    //    var methodInfo = myType.GetMethod("MyMethod", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //    var result = methodInfo.Invoke(instance, new[] { (object)"Hello World" });
    //    Debug.Log((string)result);
    //}
}