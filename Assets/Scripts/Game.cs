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

        Create();

        var gameMachineConfig = new StateMachineConfig();

        gameMachineConfig.AddState(States.StartScreen, typeof(StartScreen))
            .Permit(States.InGame);

        /*
        gameMachineConfig.AddState(States.InGame, typeof(InGame))
            .Permit(States.StartScreen)
            .PermitChild(States.Options);

        gameMachineConfig.AddState(States.Options, typeof(Options));
         */

        gameMachineConfig.SetInitialState(States.StartScreen);
        

        _machine = gameMachineConfig.Build<Game>(null);
    }

    private event Action OnUpdate; 
    [StateMethod]
    private void Update() {
        if (OnUpdate != null)
            OnUpdate();
    }

    private event Func<int, bool> OnVerifyPlayer;
    [StateMethod]
    private bool VerifyPlayer(int playerIndex) {
        if (OnVerifyPlayer != null) {
            return OnVerifyPlayer(playerIndex);
        }
        return false;
    }

    private class StartScreen : IState {
        private GameObject _startScreen;

        public void OnEnter(object data) {
            // Wait for player to press start
        }

        private void Update() {
            
        }

        public void OnExit() {
            Destroy(_startScreen);
        }
    }

    /*
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
     * */

    private static void Create() {
        var compileUnit = new CodeCompileUnit();
        var myNamespace = new CodeNamespace("MyNamespace");
        var myClass = new CodeTypeDeclaration("MyClass");
        var method = new CodeMemberMethod();
        method.Name = "MyMethod";
        method.ReturnType = new CodeTypeReference("System.String");
        method.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "text"));
        method.Statements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("text")));
        myClass.Members.Add(method);
        myNamespace.Types.Add(myClass);
        compileUnit.Namespaces.Add(myNamespace);

        var provider = new CSharpCodeProvider();
        var cp = new CompilerParameters();
        cp.GenerateExecutable = false;
        cp.GenerateInMemory = true;

        var cr = provider.CompileAssemblyFromDom(cp, compileUnit);

        //var myType = cr.CompiledAssembly.GetType("MyNamespace.MyClass");
        //var instance = Activator.CreateInstance(myType);
        var instance = cr.CompiledAssembly.CreateInstance("MyNamespace.MyClass");
        var myType = instance.GetType();
        var methodInfo = myType.GetMethod("MyMethod", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var result = methodInfo.Invoke(instance, new[] { (object)"Hello World" });
        Debug.Log((string)result);
    }
}