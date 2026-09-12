using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifeSeconds = 3f;

    void Start()
    {
        Destroy(gameObject, lifeSeconds);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * (speed * Time.deltaTime));
    }

    void OnTriggerEnter(Collider other)
    {
        Target target = other.GetComponent<Target>();
        if (target != null) { target.Hit(); }

        Destroy(gameObject);
    }
}
