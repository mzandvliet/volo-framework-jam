using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour {
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _damage = 50f;

	void Start () {
	
	}
	
	void Update () {
	    transform.Translate(0f, _speed * Time.deltaTime, 0f, Space.Self);
	}

    void OnTriggerEnter(Collider other) {
        var health = other.GetComponent<Health>(); // Todo: How do we message this if health component is not on the thing we hit?
        if (health) {
            health.Damage(_damage);
        }

        Destroy(gameObject);
    }
}
