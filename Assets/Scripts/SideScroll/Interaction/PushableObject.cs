using UnityEngine;

namespace GameCreate3
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PushableObject : SideScrollInteractableBase
    {
        public enum PushAxis
        {
            Horizontal = 0,
            Vertical = 1
        }

        [SerializeField] private string pushId = "default";
        [SerializeField] private PushAxis movementAxis = PushAxis.Horizontal;
        [SerializeField] private float pushSpeed = 2f;
        [SerializeField] private bool reportAsGoal;

        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation |
                (movementAxis == PushAxis.Horizontal ? RigidbodyConstraints2D.FreezePositionY : RigidbodyConstraints2D.FreezePositionX);
        }

        public override bool CanInteract(GameObject interactor)
        {
            return false;
        }

        public override void Interact(GameObject interactor)
        {
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!collision.collider.TryGetComponent<SideScrollCharacterControllerBase>(out _))
            {
                return;
            }

            var direction = Mathf.Sign(collision.relativeVelocity.x == 0f ? collision.transform.position.x - transform.position.x : collision.relativeVelocity.x);
            var velocity = movementAxis == PushAxis.Horizontal
                ? new Vector2(-direction * pushSpeed, 0f)
                : new Vector2(0f, -Mathf.Sign(collision.relativeVelocity.y) * pushSpeed);
            body.velocity = velocity;
        }

        public void ReportSolved()
        {
            if (!TryGetWorkspace(out var workspace))
            {
                return;
            }

            workspace.RaiseWorkspaceEvent($"push.{pushId}.solved");
            if (reportAsGoal)
            {
                workspace.RegisterGoal(pushId);
            }
        }
    }
}
