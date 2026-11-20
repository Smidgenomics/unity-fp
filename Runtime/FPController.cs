// smidgens @ github

namespace Smidgenomics.Unity.FirstPerson
{
	using System;
	using System.Collections;
	using UnityEngine;

	/// <summary>
	/// Basic FP controller, modeled after Half-Life
	/// </summary>
	[DefaultExecutionOrder(2)]
	[RequireComponent(typeof(CharacterController))]
	[AddComponentMenu("Smidgenomics/First Person/FP Controller")]
	public sealed class FPController : BaseMono
	{
		public Vector3 Forward => FlattenXZ((_cameraPivot ?? transform).forward);

		public bool IsGrounded => _ground.IsGrounded;

		public Vector3 Position => transform.position;

		// maybe later: event subscribers
		public bool LockMotion
		{
			get => _motionLocked;
			set => _motionLocked = value;
		}

		// maybe move crouch to a separate script later
		public bool Crouched
		{
			get => _crouched;
			set => OnChanged(in _crouched, in value, SetCrouch);
		}

		public bool Sprint
		{
			get => _sprinting;
			set => OnChanged(in _sprinting, in value, SetSprint);
		}

		// ladders, vehicles etc
		public bool Mounted
		{
			get => _mounted;
			set
			{
				_mounted = value;
				if (_mounted)
				{
					_targetMotionXZ = default;
					_motionXZ = default;
				}
			}
		}

		public float GravityScale
		{
			get => _gravityScale;
			set => _gravityScale = Mathf.Max(0f, value);
		}

		public void Push(Vector3 force)
		{
			AddForce(force, 2f);
		}

		public void Move(Vector3 motion)
		{
			//if (_motionLocked) { return; }
			_cc.Move(motion);
		}

		/// <summary>
		/// Move controller relative to current orientation
		/// </summary>
		public void Drive(Vector2 dir)
		{
			if (_motionLocked)
			{
				return;
			}

			var mag = GetCurrentSpeed() * Time.deltaTime;
			_targetMotionXZ = TranslateMotion(new Vector3(dir.x, 0f, dir.y)) * mag;
		}

		/// <summary>
		/// Jump with default force
		/// </summary>
		public void Jump() => Jump(GetJumpForce());

		// Jump with variable force
		public void Jump(float force)
		{
			if (CanJump() && !LockMotion)
			{
				_forceY = force;
			}
		}

		internal void AddLook(Vector2 delta)
		{
			var aspect = _camera.aspect;
			var vFOV = _camera.fieldOfView;
			var hFOV = Camera.VerticalToHorizontalFieldOfView(vFOV, aspect);
			var pitchDelta = (delta.y / Screen.height) * vFOV;
			_yaw += (delta.x / Screen.width) * hFOV * 2;
			_pitch = Mathf.Clamp(_pitch - pitchDelta, -89f, 89f);
		}

		private void UpdateCamera()
		{
			var motionYaw = _ground.GroundMotion.yaw;
			if (!motionYaw.IsApproximatelyZero())
			{
				_yaw -= motionYaw;
			}
			_cameraPivot.position = transform.position + Vector3.up * (_cameraOffset * transform.lossyScale.y);
			_cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
		}

		// CAMERA
		[FieldGroup("Camera")]
		[SerializeField] internal Camera _camera;
		
		[FieldGroup("Camera")]
		[SerializeField] private Transform _cameraPivot; // copy look direction from this transform
		
		[FieldGroup("Camera")]
		[SerializeField] private float _cameraOffset = 1.7f; // copy look direction from this transform

		// MOVEMENT
		[FieldGroup("Movement")]
		[Min(1f)]
		[SerializeField] private float _maxVelocity = 15f; // max movement speed

		[FieldGroup("Movement")]
		[SerializeField] private float _acceleration = 6f;

		[FieldGroup("Movement")]
		[SerializeField] private float _deceleration = 12f;

		[FieldGroup("Movement")]
		[SerializeField] private float _sprintIncrease = 4f;
		
		[FieldGroup("Movement")]
		[SerializeField] private float _sprintAcceleration = 10f;

		[FieldGroup("Movement")]
		[SerializeField] private float _airDeceleration = 2f;
		
		[FieldGroup("Crouch")]
		[SerializeField] private float _crouchAcceleration = 15f;

		// GRAVITY
		[FieldGroup("Gravity")]
		[SerializeField] private float _gravity = 40f; // downwards acceleration when not grounded

		[FieldGroup("Gravity")]
		[Min(0f)]
		[SerializeField] private float _gravityScale = 1f;

		// JUMP
		[FieldGroup("Jump")]
		[SerializeField] private float _jumpForce = 15f; // default upwards force applied on jump
		
		[FieldGroup("Jump")]
		[SerializeField] private float _jumpThreshold = 0.1f; // when leaving ground, allow jumping for this long

		[FieldGroup("Jump")]
		[Range(0.1f, 1f)]
		[SerializeField] private float _jumpForceCrouched = 0.5f;

		[FieldGroup("Climbing")]
		[Expand(true)]
		[SerializeField] internal FPClimbingConfig _climbingSettings = FPClimbingConfig.Default;

		// misc
		[HideInInspector]
		[SerializeField] private bool _lockRotation = true;

		[FieldGroup("Ground")]
		[Expand(true)]
		[SerializeField] internal CCGroundSettings _groundSettings = new();

		[FieldGroup("Input")]
		[Expand(true)]
		[SerializeField] internal InputSettings _input;

		internal CCGround _ground;
		private CharacterController _cc; // clown controller
		private float _forceY; // jump/gravity
		private CollisionFlags _movementFlags = CollisionFlags.None; // from CC.Move
		private Vector3 _motionXZ;
		private Vector3 _targetMotionXZ;
		private bool _motionLocked;
		private bool _mounted;
		private bool _crouched;
		private bool _sprinting;
		private float _sprintState; // 0-1
		private float _crouchState; // 0-1
		private Vector3 _startScale = Vector3.one;
		private float _lastGroundedTime;
		private float _yaw;
		private float _pitch;

		private void Awake()
		{
			_cc = GetComponent<CharacterController>();
			_ground = new CCGround(_cc, _groundSettings);
			_startScale = transform.localScale;

			_yaw = transform.localRotation.eulerAngles.y;
			transform.localRotation = Quaternion.identity;
		}

		private void Start()
		{
			Cursor.lockState = CursorLockMode.Confined;
			Cursor.visible = false;
		}

		private void Update()
		{
			if (Time.timeScale.IsApproximatelyZero())
			{
				return;
			}

			UpdateInput();

			_ground.Update();

			ApplyGroundMotion();
			
			MoveY();
			Physics.SyncTransforms();

			// keep track of ground
			if (_ground.IsGrounded)
			{
				_lastGroundedTime = Time.time;
			}

			// constrain rotation
			if (_lockRotation)
			{
				transform.rotation = default;
			}
		}

		private void FixedUpdate()
		{
			if (Time.timeScale.IsApproximatelyZero())
			{
				return;
			}
			// movement
			
			// ApplyGroundMotion();

			// MoveY();
			//
			// Physics.SyncTransforms();
			
		}

		private void LateUpdate()
		{
			if (Time.timeScale.IsApproximatelyZero())
			{
				return;
			}


			MoveXZ();
			// modifiers
			ApplyCrouch();
			ApplySprint();

			// reset state
			_movementFlags = CollisionFlags.None;

			UpdateCamera();
		}

		private void UpdateInput()
		{
			if (Time.timeScale.IsApproximatelyZero())
			{
				return;
			}

			if (_input.crouchMode == EFPCrouchMode.Toggled)
			{
				if (_input.crouchAction.action.WasPressedThisFrame())
				{
					Crouched = !Crouched;
				}
			}
			else
			{
				Crouched = _input.crouchAction.action.IsPressed();
			}

			// sprint
			Sprint = _input.sprintAction.action.IsPressed();

			Drive(_input.moveAction.action.ReadValue<Vector2>());

			if (_input.jumpAction.action.WasPressedThisFrame())
			{
				Jump();
			}

			AddLook(_input.lookAction.action.ReadValue<Vector2>());
		}

		private void ApplyGroundMotion()
		{
			if (Mounted)
			{
				return;
			}
			var motion = _ground.GroundMotion.movement;

			if (motion.magnitude.IsApproximatelyZero())
			{
				return;
			}

			transform.position += motion;

			Physics.SyncTransforms();
		}

		private static Vector3 FlattenXZ(Vector3 dir)
		{
			dir.y = 0f;
			return dir.normalized;
		}

		private float GetJumpForce()
		{
			// maybe: use curve or something
			var scale = Mathf.Lerp(1f, _jumpForceCrouched, _crouchState);

			var totalForce = _jumpForce;
			var groundForce = _ground.GroundMotion.movement.y;

			// negate downwards force of platforms
			if (groundForce < 0)
			{
				totalForce += -groundForce;
			}

			return totalForce * scale;
		}

		private void AddForce(Vector3 force, float dist)
		{
			StartCoroutine(ForceRoutine(force, dist));
		}

		private IEnumerator ForceRoutine(Vector3 force, float dist)
		{
			var cc = _cc;
			var end = Time.time + 1f;
			var dir = force.normalized;
			var fmag = force.magnitude;
			while (Time.time < end && fmag > 0f && !_ground.IsGrounded)
			{
				var mag = fmag * Time.deltaTime;
				cc.Move(dir * mag);
				fmag -= mag;
				yield return null;
			}
		}

		private float GetCurrentSpeed()
		{
			var speed = (_maxVelocity) * Mathf.Lerp(1f, 0.5f, _crouchState);

			if (!_crouched)
			{
				speed += _sprintIncrease * _sprintState;
			}

			// TODO: air control
			var tcontrol = 1f;

			return speed * tcontrol;
		}

		private void SetSprint(bool v)
		{
			_sprinting = v;
		}

		private void SetCrouch(bool v)
		{
			// validate here later if needed
			_crouched = v;
		}

		// is something intersecting current and max head position
		private bool TopClipsCeiling()
		{
			// note: assumes pivot is at zero
			var layers = Physics.AllLayers;
			layers &= ~(1 << gameObject.layer);
			const float dist = 0.005f;
			var localHeadPos = Vector3.up * _cc.height;
			var actualPos = transform.TransformPoint(localHeadPos);
			var standPos = transform.position + Vector3.up * (_cc.height + dist);
			return Physics.Linecast(actualPos, standPos, layers, QueryTriggerInteraction.Ignore);
		}

		private void MoveY()
		{
			if (Mounted)
			{
				return;
			}

			var dt = Time.deltaTime;

			var grav = dt * -_gravity * GravityScale;
			//
			// if (_ground.IsGrounded && _forceY < 0)
			// {
			// 	grav = 0;
			// 	_forceY = 0;
			// }

			_forceY += grav;

			_movementFlags |= _cc.Move(Vector3.up * (_forceY * dt));
			var hitGround = _movementFlags.HasFlag(CollisionFlags.Below);
			var hitCeiling = _movementFlags.HasFlag(CollisionFlags.Above) && _forceY > 0f;
			if (hitCeiling)
			{
				_forceY = grav;
			}
			else if (hitGround)
			{
				_forceY = 0f;
			}
		}

		private void ApplySprint()
		{
			if (_crouched)
			{
				return;
			}
			_sprintState = Mathf.Clamp01(_sprintState += ((_sprinting ? 1 : -1f) * _sprintAcceleration * Time.deltaTime));
		}

		/*
		Some issues with crouch:
		- Crouching mid air feels awkward; would be nice if it was HL-esque (scales from bottom so you can jump through windows)
		*/
		private void ApplyCrouch()
		{
			// cannot uncrouch if head would clip into ceiling
			if (!_crouched && TopClipsCeiling())
			{
				return;
			}

			// stop at ceiling
			if (_movementFlags.HasFlag(CollisionFlags.Above))
			{
				return;
			}

			var newState = Mathf.Clamp01(_crouchState + ((_crouched ? 1 : -1f) * _crouchAcceleration * Time.deltaTime));
				
			_crouchState = newState;
			var scale = Mathf.Lerp(0.5f, 1f, 1f - _crouchState);
			transform.localScale = new Vector3(_startScale.x, scale * _startScale.y, _startScale.z);

		}

		// apply XZ movement relative to controller orientation
		private void MoveXZ()
		{
			if (LockMotion)
			{
				return;
			}

			bool accelerating = _targetMotionXZ.sqrMagnitude > _motionXZ.sqrMagnitude;
			bool pressingSomething = _targetMotionXZ.sqrMagnitude > 0;

			float accelFactor = !IsGrounded && !pressingSomething
			// If we're in the air and let go, then lerp to zero/target
			// but do it slower
			? _airDeceleration
			//	Else, just pick the appropriate accel factor
			: accelerating ? _acceleration : _deceleration;

			_motionXZ = Vector3.Lerp(_motionXZ, _targetMotionXZ, Time.deltaTime * accelFactor);

			// why bother moving if zero
			if (Mathf.Approximately(_motionXZ.magnitude, 0f))
			{
				return;
			}
			_movementFlags |= _cc.Move(GetGroundMotion(_motionXZ));
			
		}

		// adjust motion based on ground
		private Vector3 GetGroundMotion(in Vector3 motion)
		{
			if (!IsGrounded)
			{
				return motion;
			}

			var dir = FPMath.MoveAlongSurface(motion.normalized, _ground.NormalAverage);

			return dir * motion.magnitude;
		}

		// translate motion from relative to camera direction
		private Vector3 TranslateMotion(in Vector3 motion)
		{
			var lookDir = _cameraPivot.forward;
			lookDir.y = 0f;
			lookDir.Normalize();
			if (Mathf.Approximately(lookDir.magnitude, 0f))
			{
				return motion;
			}
			var rot = Quaternion.LookRotation(lookDir, Vector3.up);
			return rot * motion;
		}

		// trigger jump?
		private bool CanJump()
		{
			return
			_ground.IsGrounded // literally grounded
			|| _lastGroundedTime <= _jumpThreshold; // juuuuuust left ground
		}

		private static void OnChanged<T>(in T current, in T newValue, Action<T> setter) where T : IComparable<T>
		{
			if (current.CompareTo(newValue) != 0)
			{
				setter.Invoke(newValue);
			}
		}
#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (_ground != null)
			{
				_ground.DrawGizmos(_cc, _groundSettings);
			}
		}
#endif
	}

}

//====================================
//============ EDITOR ================
//====================================

#if UNITY_EDITOR

namespace Smidgenomics.Unity.FirstPerson.Editor
{
	using UnityEditor;

	internal sealed class _FPController : _BaseMono
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			serializedObject.UpdateIfRequiredOrScript();
			serializedObject.ApplyModifiedProperties();
			EditorGUILayout.EndVertical();
		}
	
	}
}

#endif