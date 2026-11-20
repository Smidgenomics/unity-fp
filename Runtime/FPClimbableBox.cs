// smidgens @ github

namespace Smidgenomics.Unity.FirstPerson
{
	using UnityEngine;

	/// <summary>
	/// HL-style ladder
	/// </summary>
	[RequireComponent(typeof(BoxCollider))]
	[DisallowMultipleComponent]
	[AddComponentMenu("Smidgenomics/First Person/FP Climbable Box")]
	internal sealed class FPClimbableBox : BaseMono
	{
		[SerializeField] private LayerMask _mask = 0;

		private BoxCollider _collider;
		private FPController _controller;

		private float _cachedGravityScale = 1f;

		private void Awake()
		{
			_collider = GetComponent<BoxCollider>();
			_collider.isTrigger = true;
		}

		private void Update()
		{
			if (_controller)
			{
				ProcessAttached();
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (_controller)
			{
				// shouldn't ever happen but just to be safe
				return;
			}
			// get colliding controller
			if (!TryGetController(other, out _controller))
			{
				return;
			}
			AttachController();
		}
		private void OnTriggerExit(Collider other)
		{
			if (!_controller)
			{
				return;
			}
			DetatchController();
		}

		private void ProcessAttached()
		{
			if (_controller._input.jumpAction.action.WasPressedThisFrame())
			{
				var lookDir = _controller._camera.transform.forward;
				var dir = lookDir;
				dir.y = 0f;
				dir.Normalize();
				var dot = Vector3.Dot(dir, transform.forward);
				_controller.Push(dir * (_controller._climbingSettings.detachForce * Mathf.Sign(dot)));
			}
			
			var movement = GetLadderMotion();
			if (Mathf.Approximately(movement.magnitude, 0f))
			{
				return;
			}

			_controller.Move(movement);
		}

		private Vector3 GetLadderMotion()
		{
			var input = _controller._input.moveAction.action.ReadValue<Vector2>();

			var dir = GetLadderDirection();

			dir.x *= -1 * _controller._climbingSettings.lateralFactor;

			var lookDir = _controller._camera.transform.forward;
			var fwdDot = Vector3.Dot(lookDir, transform.forward);

			var fwdMotion = dir * (Time.deltaTime * _controller._climbingSettings.verticalSpeed * input.y);

			var latMotion = new Vector2(Time.deltaTime * _controller._climbingSettings.lateralSpeed * input.x * fwdDot, 0f);
			
			var totalMotion = latMotion + fwdMotion;

			return transform.TransformDirection(totalMotion);
		}

		private Vector2 GetLadderDirection()
		{
			// var lookDir = _look.forward;
			var lookDir = _controller._camera.transform.forward;
			var lookRight = _controller._camera.transform.right;
			var dot = Vector3.Dot(lookDir, transform.forward);
			var sin = Mathf.Sign(dot);

			var facingLadder = dot < 0f;

			var vdir = lookDir;

			if (facingLadder)
			{
				vdir = -vdir;
			}

			// vertical angle
			var angleV = Vector3.SignedAngle
			(
				ZeroY(vdir),
				vdir,
				-lookRight
			);

			// horizontal angle
			var angleH = Vector3.SignedAngle
			(
				transform.forward * sin,
				ZeroY(lookDir),
				transform.up * -sin
			);

			var tv = angleV / 90f;
			var th = angleH / 90f;

			return new Vector2(th, tv);
		}

		private static Vector3 ZeroY(Vector3 v) => new Vector3(v.x, 0f, v.z).normalized;

		private void AttachController() // start climbing
		{
			_controller.LockMotion = true;
			_controller.Mounted = true;
			_cachedGravityScale = _controller.GravityScale;
			_controller.GravityScale = 0f;
		}

		private void DetatchController()
		{
			if (!_controller)
			{
				return;
			}
			_controller.GravityScale = _cachedGravityScale;
			_controller.Mounted = false;
			_controller.LockMotion = false;
			_controller = null;
		}

		// get visiting controller
		private bool TryGetController(Collider c, out FPController controller)
		{
			controller = null;
			if (!_mask.HasLayer(c.gameObject.layer))
			{
				return false;
			}
			var cc = c as CharacterController;
			if (!cc)
			{
				return false;
			}
			controller = c.GetComponent<FPController>();
			return controller != null;
		}
		
#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (!enabled)
			{
				return;
			}

			if (!_collider)
			{
				_collider = GetComponent<BoxCollider>();
			}
			Gizmos.matrix = _collider.transform.CopyMatrix();
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(_collider.center, _collider.size);
		}
#endif

	}
}
