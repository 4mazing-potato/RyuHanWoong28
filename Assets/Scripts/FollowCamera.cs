using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private string playerTag = "Player";

    private Transform target;
    private Vector3 velocity;
    private float fixedZ;

    private void Awake()
    {
        fixedZ = transform.position.z;
        FindPlayerTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindPlayerTarget();
            if (target == null)
            {
                return;
            }
        }

        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, fixedZ);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.position = new Vector3(transform.position.x, transform.position.y, fixedZ);
    }

    private void FindPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        target = player != null ? player.transform : null;
    }
}
