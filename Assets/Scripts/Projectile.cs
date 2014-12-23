using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour {
    [SerializeField] private float _speed = 1f;

	void Start () {
	
	}
	
	void Update () {
	    transform.Translate(0f, _speed * Time.deltaTime, 0f, Space.Self);
	}

    void OnTriggerEnter(Collider other) {
        Destroy(gameObject);
    }
}
