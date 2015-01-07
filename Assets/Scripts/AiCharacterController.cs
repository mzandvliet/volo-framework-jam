using UnityEngine;
using System.Collections;

public class AiCharacterController : MonoBehaviour {
    [SerializeField] private Character _character;
    [SerializeField] private Character _target;

    public Character Character {
        get { return _character; }
        set { _character = value; }
    }

    public Character Target {
        get { return _target; }
        set { _target = value; }
    }

    private Vector3 _mouseWorldPos;

    void Update() {
        Vector2 targetDirection = (_target.transform.position - _character.transform.position).normalized;

        _character.SetInput(new CharacterInput {
            Walk = targetDirection,
            Look = targetDirection,
            Shoot = false
        });
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_mouseWorldPos, 0.5f);
    }
}
