using RamjetAnvil.Unity.Utils;
using UnityEngine;

public class Character : MonoBehaviour {
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _walkSpeed = 1f;
    [SerializeField] private float _turnSpeed = 45f;

    CharacterInput _input;
    //private StateMachine _machine;

    //private void Start() {
    //    var machineConfig = new StateMachineConfig();
    //    machineConfig.AddState(States.Alive, typeof(Alive))
    //        .Permit(States.Dead);
    //    machineConfig.AddState(States.Dead, typeof(Dead));
    //    machineConfig.Build(this);
    //}
    void Update() {
        transform.Translate(new Vector3(_input.Walk.x, _input.Walk.y, 0f) * Time.deltaTime * _walkSpeed, Space.World);
        float angleToTarget = AngleSigned(transform.up, _input.Look);
        transform.Rotate(0f, 0f, angleToTarget * Time.deltaTime * _turnSpeed, Space.World);

        if (_input.Shoot) {
            Instantiate(_projectilePrefab, transform.position + transform.up, transform.rotation);
        }
    }

    private static float AngleSigned(Vector2 a, Vector2 b) {
        return Mathf.Atan2(b.y, b.x) - Mathf.Atan2(a.y, a.x);
    }

    public void SetInput(CharacterInput input) {
        input.Walk = Vector2.ClampMagnitude(input.Walk, 1f);
        input.Look.Normalize();
        _input = input;
    }

    //public static class States {
    //    public static readonly StateId Alive = new StateId("Alive");
    //    public static readonly StateId Dead = new StateId("Dead");
    //}

    //private class Alive : IState {
    //    public void OnEnter(StateMachine machine, object data) {
            
    //    }

    //    /* Todo: we want to do all the things a monobehaviour can do here
    //     * 
    //     * Update
    //     * OnCollision
    //     * Etc.
    //     */


    //    public void OnExit() {

    //    }
    //}

    //private class Dead : IState {
    //    public void OnEnter(StateMachine machine, object data) {

    //    }

    //    public void OnExit() {

    //    }
    //}
}

public struct CharacterInput {
    public Vector2 Walk;
    public Vector2 Look;
    public bool Shoot;
}