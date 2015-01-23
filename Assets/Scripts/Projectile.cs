using UnityEngine;

public class Projectile : MonoBehaviour {
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _damage = 50f;

	void Start () {
	
	}
	
	void Update () {
	    transform.Translate(0f, _speed * Time.deltaTime, 0f, Space.Self);
	}

    void OnTriggerEnter(Collider other) {
        /*
         * Todo: How do we message this if health component is not on the thing we hit?
         * 
         * We don't know which component might want to handle the damage event, unless we enforce
         * the requirement of having a health component to catch it first.
         * 
         */

        var health = other.GetComponent<Health>();
        if (health) {
            health.Damage(_damage);
        }

        Destroy(gameObject);
    }
}
