using UnityEngine;

public class Health : MonoBehaviour {
    [SerializeField] private float _initialHealth = 100f;

    private float _health;

    public event System.Action<Health, float> OnDamaged;
    public event System.Action<Health> OnDied;

    private void OnSpawned() {
        _health = _initialHealth;
    }
    

    public void Damage(float damage) {
        damage = Mathf.Abs(damage);
        _health = Mathf.Clamp(_health - damage, 0f, _initialHealth);

        if (OnDamaged != null) {
            OnDamaged(this, damage);
        }

        if (_health <= 0) {
            if (OnDied != null) {
                OnDied(this);
            }
        }
    }
}
