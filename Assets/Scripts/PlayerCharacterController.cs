using UnityEngine;
using System.Collections;

/* Todo: Inject dependencies (character, camera, input) */

public class PlayerCharacterController : MonoBehaviour {
    [SerializeField] private Character _character;
    [SerializeField] private Camera _camera;

    private PlayerInputDevice _input;

    public Character Character {
        get { return _character; }
        set { _character = value; }
    }

    public PlayerInputDevice Input {
        get { return _input; }
        set { _input = value; }
    }

    public Camera Camera {
        get { return _camera; }
        set { _camera = value; }
    }

    private Vector3 _mouseWorldPos;

	void Update () {
	    var ray = _camera.ScreenPointToRay(_input.GetMousePosition());
	    var hitInfo = new RaycastHit();
        _mouseWorldPos = Vector3.zero;
	    if (Physics.Raycast(ray, out hitInfo, 1000f)) {
            _mouseWorldPos = hitInfo.point;
	    }
        Vector2 lookDirection = (_mouseWorldPos - _character.transform.position).normalized;
        
        _character.SetInput(new CharacterInput {
            Walk = new Vector2(_input.GetAxis("Horizontal"), _input.GetAxis("Vertical")),
            Look = lookDirection,
            Shoot = _input.GetButtonDown("Fire1")
        });
	}

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_mouseWorldPos, 0.5f);
    }
}

/// <summary>
/// Just an input class that lets us pretend we can have separate input devices per player
/// </summary>
public class PlayerInputDevice {
    public float GetAxis(string name) {
        return Input.GetAxis(name);
    }

    public bool GetButtonDown(string name) {
        return Input.GetButtonDown(name);
    }

    public bool GetKeyDown(KeyCode keyCode) {
        return Input.GetKeyDown(keyCode);
    }

    public Vector3 GetMousePosition() {
        return Input.mousePosition;
    }

    public bool AnyKeyDown() {
        return Input.anyKeyDown;
    }
}