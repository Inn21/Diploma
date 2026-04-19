using UnityEngine;

public class Bird : MonoBehaviour
{
    private GameManager _gameManager;
    [SerializeField] private bool allowMouseInput = false;

    public float jumpForce = 5f;
    private Rigidbody2D rb;
    private Vector3 startPosition;

    public void SetManager(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        if(_gameManager.IsGameOver) return;

        if (allowMouseInput && Input.GetMouseButtonDown(0))
        {
            Jump();
        }

        if(Mathf.Abs(transform.localPosition.y)> 2.8 ) _gameManager.GameOver();
    }

    public void Jump()
    {
        if(_gameManager.IsGameOver) return;

        if (_gameManager.isFirstTap) rb.gravityScale = 1;
        _gameManager.isFirstTap = false;
        rb.linearVelocity = Vector2.up * jumpForce;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(_gameManager.IsGameOver) return;

        _gameManager.GameOver();
    }

    public void ResetState()
    {
        rb.gravityScale = 0;
        transform.localPosition = startPosition;
        rb.linearVelocity = Vector2.zero;
        transform.rotation = Quaternion.identity;
    }
}
