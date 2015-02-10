using UnityEngine;

[RequireComponent(typeof(Health))]
public class Character : MonoBehaviour {
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _walkSpeed = 1f;
    [SerializeField] private float _turnSpeed = 45f;

    private Spawner _spawner;
    private Health _health;
    private CharacterInput _input;

    public Spawner Spawner {
        get { return _spawner; }
        set { _spawner = value; }
    }

    public float WalkSpeed {
        get { return _walkSpeed; }
        set { _walkSpeed = value; }
    }

    public float TurnSpeed {
        get { return _turnSpeed; }
        set { _turnSpeed = value; }
    }

    private void Awake() {
        _health = GetComponent<Health>();
        _health.OnDied += health => {
            Debug.Log(name + " died");
        };
    }

    void Update() {
        transform.Translate(new Vector3(_input.Walk.x, _input.Walk.y, 0f) * Time.deltaTime * _walkSpeed, Space.World);
        float angleToTarget = AngleSigned(transform.up, _input.Look);
        transform.Rotate(0f, 0f, angleToTarget * Time.deltaTime * _turnSpeed, Space.World);

        if (_input.Shoot) {
            Instantiate(_projectilePrefab, transform.position + transform.up, transform.rotation);

//            var projectile = _spawner.Get(Prefabs.Projectile).Spawn();
//            projectile.transform.position = transform.position + transform.up;
//            projectile.transform.rotation = transform.rotation;
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
}

public struct CharacterInput {
    public Vector2 Walk;
    public Vector2 Look;
    public bool Shoot;
}