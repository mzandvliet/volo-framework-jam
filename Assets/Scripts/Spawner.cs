using System;
using System.Collections.Generic;
using UnityEngine;

// Todo: resolve dependencies for instantiated prefabs. (Character wants to spawn projectiles, needs Spawner ref)

public class Spawner : MonoBehaviour, IObjectPool {
    [SerializeField]
    private List<PooledObject> _objects;

    private ObjectPool Pools;

    private void Awake() {
        Pools = new ObjectPool(_objects);
    }

    private void OnDestroy() {
        // Clean up pools and any of their still-alive instances
    }

    public Pool Get(PrefabId type) {
        return Pools.Get(type);
    }
}

[Serializable]
public class PrefabId {
    [SerializeField] private string _id;

    public string Id {
        get { return _id; }
    }

    public PrefabId(string id) {
        _id = id;
    }

    public override string ToString() {
        return _id;
    }

    protected bool Equals(PrefabId other) {
        return string.Equals(_id, other._id);
    }

    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) {
            return false;
        }
        if (ReferenceEquals(this, obj)) {
            return true;
        }
        if (obj.GetType() != this.GetType()) {
            return false;
        }
        return Equals((PrefabId) obj);
    }

    public override int GetHashCode() {
        return (_id != null ? _id.GetHashCode() : 0);
    }

    public static bool operator ==(PrefabId left, PrefabId right) {
        return Equals(left, right);
    }

    public static bool operator !=(PrefabId left, PrefabId right) {
        return !Equals(left, right);
    }
}

public interface IObjectPool {
    Pool Get(PrefabId id);
}

public class ObjectPool : IObjectPool {
    private IDictionary<PrefabId, Pool> _pools;

    public ObjectPool(IList<PooledObject> prefabs) {
        _pools = new Dictionary<PrefabId, Pool>(prefabs.Count);

        for (int i = 0; i < prefabs.Count; i++) {
            var pooledObject = prefabs[i];

            if (_pools.ContainsKey(pooledObject.Type)) {
                throw new ArgumentException(string.Format("Multiple PooledObjects with type '{0}' found.", pooledObject.Type));
            }

            _pools.Add(pooledObject.Type, new Pool(pooledObject));
        }
    }

    public Pool Get(PrefabId type) {
        return _pools[type];
    }
}

[Serializable]
public class PooledObject {
    [SerializeField] public PrefabId Type;
    [SerializeField] public GameObject Prefab;
    [SerializeField] public int Count;
}

public class Pool {
    private PooledObject _object;
    private Stack<GameObject> _despawned;
    private List<GameObject> _spawned;

    public Pool(PooledObject o) {
        _object = o;
        _despawned = new Stack<GameObject>(o.Count);
        _spawned = new List<GameObject>(o.Count);

        for (int i = 0; i < o.Count; i++) {
            var instance = UnityEngine.Object.Instantiate(o.Prefab);
            DespawnInstance(instance);
            _despawned.Push(instance);
        }
    }

    public GameObject Spawn() {
        if (_despawned.Count <= 0) {
            throw new OverflowException(string.Format("Pool of type '{0}: Reached maximum number of spawned instances", _object.Type));
        }

        GameObject instance = _despawned.Pop();
        SpawnInstance(instance);
        _spawned.Add(instance);
        return instance;
    }

    public void Despawn(GameObject instance) {
        if (!_spawned.Contains(instance)) {
            throw new ArgumentException(string.Format("GameObject '{0}' cannot be despawned in pool {1}", instance, _object.Type));
        }

        _spawned.Remove(instance);
        DespawnInstance(instance);
        _despawned.Push(instance);

    }
    private void SpawnInstance(GameObject instance) {
        if (instance == null) {
            throw new NullReferenceException(string.Format("Pooled instance of type '{0}' is null. Did you Destroy() it by accident?", _object.Type));
        }

        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.SetActive(true);
        instance.SendMessage("OnSpawned", SendMessageOptions.DontRequireReceiver);
    }

    private void DespawnInstance(GameObject instance) {
        instance.SendMessage("OnDespawn", SendMessageOptions.DontRequireReceiver);
        instance.SetActive(false);
    }
}

public interface IPooledComponent {
    void OnSpawned();
    void OnDespawn();
}
