using UnityEngine;

public class TopDownPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Animator animator;

    private Vector2 facingDirection = Vector2.down;

    private static readonly int MoveXHash = Animator.StringToHash("moveX");
    private static readonly int MoveYHash = Animator.StringToHash("moveY");
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector2 movement = input.sqrMagnitude > 1f ? input.normalized : input;

        transform.position += (Vector3)(movement * (moveSpeed * Time.deltaTime));

        bool isMoving = movement.sqrMagnitude > 0f;
        if (isMoving)
        {
            facingDirection = GetCardinalDirection(movement);
        }

        animator.SetBool(IsMovingHash, isMoving);
        animator.SetFloat(MoveXHash, facingDirection.x);
        animator.SetFloat(MoveYHash, facingDirection.y);
    }

    private static Vector2 GetCardinalDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return new Vector2(Mathf.Sign(direction.x), 0f);
        }

        return new Vector2(0f, Mathf.Sign(direction.y));
    }
}
