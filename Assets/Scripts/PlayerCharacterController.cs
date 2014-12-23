using UnityEngine;
using System.Collections;

public class PlayerCharacterController : MonoBehaviour {
    [SerializeField] private Character _character;
    [SerializeField] private Camera _camera;

    private Vector3 _mouseWorldPos;

	void Update () {
	    var ray = _camera.ScreenPointToRay(Input.mousePosition);
	    var hitInfo = new RaycastHit();
        _mouseWorldPos = Vector3.zero;
	    if (Physics.Raycast(ray, out hitInfo, 1000f)) {
            _mouseWorldPos = hitInfo.point;
	    }
        Vector2 lookDirection = (_mouseWorldPos - _character.transform.position).normalized;
        
        _character.SetInput(new CharacterInput {
            Walk = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")),
            Look = lookDirection,
            Shoot = Input.GetButtonDown("Fire1")
        });
	}

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_mouseWorldPos, 0.5f);
    }
}
