// smidgens @ github

namespace Smidgenomics.Unity.FirstPerson
{
	using UnityEngine;
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Character Controller specific implementation of ground check
	/// </summary>
	internal partial class CCGround
	{
		private const int OVERLAP_MAX = 1;

		/// <summary>
		/// Is controller effectively grounded
		/// </summary>
		public bool IsGrounded { get; private set; } = true;

		/// <summary>
		/// Averaged ground normal
		/// </summary>
		public Vector3 NormalAverage { get; private set; } = Vector3.up;

		public MotionInfo GroundMotion => _lastGroundMotion;
		
		public Collider GroundCollider => GetGroundCollider();

		public CCGround(CharacterController cc, CCGroundSettings settings)
		{
			_cc = cc;
			_settings = settings;
			_groundScanDirections = GetSlopeScanDirections(_settings._groundScanAngle, _settings._slopeScanSegments, _settings._slopeScansPerSegment);
			_cachedAngle = _settings._groundScanAngle;
		}

		private readonly CCGroundSettings _settings;

		private readonly Collider[] _overlapColliders = new Collider[OVERLAP_MAX];
		private int _overlapCount = 0;

		// for motion tracking
		private Collider _lastGroundCollider;
		
		// 
		private MotionInfo _lastGroundMotion;

		// _lastGround matrix from last frame
		private Matrix4x4 _cachedGroundTransform = Matrix4x4.identity; // cached transform from last frame

		private readonly CharacterController _cc;
		private Vector3[] _groundScanDirections; // cached unit directions
		private float _cachedAngle = 0f;
		private float _lastGroundedTime = 0f;

		public void Update()
		{

#if UNITY_EDITOR
			// so we don't have to restart
			var refreshDirections =
			(_settings._slopeScansPerSegment * _settings._slopeScanSegments) != _groundScanDirections.Length
			|| !Mathf.Approximately(_settings._groundScanAngle, _cachedAngle);

			if (refreshDirections)
			{
				_groundScanDirections = GetSlopeScanDirections(_settings._groundScanAngle, _settings._slopeScanSegments, _settings._slopeScansPerSegment);
				_cachedAngle = _settings._groundScanAngle;
			}
#endif
			IsGrounded = CheckGroundSphere();

			if (_settings._includeIsGrounded)
			{
				IsGrounded |= _cc.isGrounded;
			}
			
			NormalAverage = IsGrounded ? GetNormalFromGroundRays() : Vector3.up;

			if (IsGrounded)
			{
				_lastGroundedTime = Time.time;
			}
			
			TrackGroundMotion();
		}

		private Collider GetGroundCollider()
		{
			if (_overlapCount == 0)
			{
				return null;
			}

			// may want to filter these at some point (collide with but not treat as "ground")
			return _overlapColliders[0];
		}

		private void TrackGroundMotion()
		{
			var currentGround = GroundCollider;
			_lastGroundMotion = default;

			if (_lastGroundCollider)
			{
				
			}

			// note:
			// when airborne it would be cool if we could gradually diminish the weight of motion
			// from last ground collider
			if (_lastGroundCollider != currentGround && currentGround)
			{
				_lastGroundCollider = currentGround;
				_cachedGroundTransform = _lastGroundCollider
				? _lastGroundCollider.transform.CopyMatrix()
				: Matrix4x4.identity;
				return;
			}

			// I'm flying, Jackass!
			if (!_lastGroundCollider)
			{
				_lastGroundMotion = default;
				return;
			}

			// we potentially fade weight when airborne
			float weight = GetMotionWeight();

			var movement = GetMotionDelta(_cc.transform.position);

			_lastGroundMotion = new()
			{
				// movement = dir,
				movement = movement * weight,
				yaw = (GetGroundYawDelta()) * weight,
			};
			_cachedGroundTransform = _lastGroundCollider.transform.CopyMatrix();
		}

		private float GetMotionWeight()
		{
			if (!_lastGroundCollider)
			{
				return 0f;
			}
			
			if (!_settings._shouldFadeMotion)
			{
				// always apply full motion of ground
				return 1f;
			}
			var airborneTime = Time.time - _lastGroundedTime;

			if (airborneTime.IsApproximatelyZero())
			{
				return 1f;
			}

			var t = Mathf.Clamp01(airborneTime / _settings._motionFadeDuration);

			return Mathf.Clamp01(_settings._motionFadeCurve.Evaluate(t));
		}
		
		private float GetGroundYawDelta()
		{
			var oldY = _cachedGroundTransform.rotation.eulerAngles.y;
			var newY = _lastGroundCollider.transform.rotation.eulerAngles.y;
			// kinda hacky
			if (Mathf.Approximately(oldY, newY))
			{
				return 0f;
			}
			return (_cachedGroundTransform.rotation * Quaternion.Inverse(_lastGroundCollider.transform.rotation)).eulerAngles.y;
		}

		// get motion to apply to point that should be treated as parented
		private Vector3 GetMotionDelta(Vector3 position)
		{
			// basically, we want to treat something in world space
			// as if it were parented to the ground transform
			var localPos = _cachedGroundTransform.inverse.MultiplyPoint3x4(position);
			var newPos = _lastGroundCollider.transform.TransformPoint(localPos);
			if (Vector3.Distance(position, newPos).IsApproximatelyZero())
			{
				return default;
			}
			return newPos - position;
		}

		// performs ground collision check
		private bool CheckGroundSphere()
		{
			var (center, radius) = GetCollisionSphere();
			_overlapCount = Physics.OverlapSphereNonAlloc(center, radius, _overlapColliders, _settings._layers, QueryTriggerInteraction.Ignore);
			return _overlapCount > 0;
		}

		private Vector3 GetNormalFromGroundRays()
		{
			var center = _cc.GetColliderBottom();
			center += Vector3.up * (_cc.radius + _cc.skinWidth);

			var unitRays = _groundScanDirections;
			var length = GetSlopeRayDistance(_cc, _settings);

			var sum = Vector3.zero;
			var count = 0;
			var slopeLimit = _cc.slopeLimit;

			foreach (var dir in unitRays)
			{
				var ray = new Ray(center, dir);
				if(Physics.Raycast(ray, out RaycastHit hit, length, _settings._layers))
				{
					var angle = Vector3.Angle(Vector3.up, hit.normal);
					if (angle > slopeLimit) { continue; }
					sum += hit.normal;
					count++;
#if UNITY_EDITOR
					Debug.DrawLine(ray.origin, hit.point, Color.green);
#endif
				}
			}
			return count > 0 ? (sum / count).normalized : Vector3.up;
		}

		// get properties of sphere used for checking ground collison
		private (Vector3, float) GetCollisionSphere()
		{
			var radius = (_cc.radius + _cc.skinWidth) * _settings._sphereScale;
			var offset = radius - _settings._sphereProtrusion;
			var pos =
			_cc.GetColliderBottom()
			+ Vector3.up * offset;
			return (pos, radius);
		}

		// how far to raycast when scanning for slope
		private static float GetSlopeRayDistance(CharacterController cc, CCGroundSettings settings)
		{
			return
			cc.radius
			+ cc.skinWidth
			+ settings._groundScanLength;
		}

		// get unit directions for slope checking at bottom
		public static Vector3[] GetSlopeScanDirections
		(
			float angle,
			int n1,
			int n2
		)
		{
			var rays = new List<Vector3>();

			var pitchOffset = 90f - angle;
			var yawStep = 360f / n1;
			var pitchStep = (angle) / n2;
			for (var i = 0; i < n1; i++)
			{
				var theta = yawStep * i;

				for (var j = 0; j < n2; j++)
				{
					var pitch = 90f + pitchOffset + pitchStep * j;
					var dir = FPMath.GetUnitSpherePos(theta, pitch);
					rays.Add(dir);
				}
			}
			return rays.ToArray();
		}


	}
}

//====================================
//============ GIZMOS ================
//====================================

#if UNITY_EDITOR

namespace Smidgenomics.Unity.FirstPerson
{
	using UnityEngine;

	internal partial class CCGround
	{
		public void DrawGizmos(CharacterController cc, CCGroundSettings settings)
		{
			if (IsGrounded)
			{
				var gpos = cc.GetColliderBottom();
				Gizmos.DrawRay(gpos, NormalAverage);
			}
			var grounded = Application.isPlaying && IsGrounded;
			var (center, radius) = GetCollisionSphere();
			Gizmos.color = grounded ? Color.green : Color.red;
			Gizmos.DrawWireSphere(center, radius);
			DrawGroundScanGizmos(cc, settings);
		}

		private static void DrawGroundScanGizmos(CharacterController cc, CCGroundSettings settings)
		{
			if (Application.isPlaying)
			{
				return;
			}

			var center = cc.GetColliderBottom();
			center += Vector3.up * (cc.radius + cc.skinWidth);

			var length = GetSlopeRayDistance(cc, settings);

			var pitchOffset = 90f - settings._groundScanAngle;
			var yawStep = 360f / settings._slopeScanSegments;
			var pitchStep = (settings._groundScanAngle) / settings._slopeScansPerSegment;

			Gizmos.color = Color.magenta;
			for (var i = 0; i < settings._slopeScanSegments; i++)
			{
				var theta = yawStep * i;
				for (var j = 0; j < settings._slopeScansPerSegment; j++)
				{
					var pitch = 90f + pitchOffset + pitchStep * j;
					var dir = FPMath.GetUnitSpherePos(theta, pitch);
					var pos = center + dir * length;
					Gizmos.DrawLine(center, pos);

				}
			}
		}
	}

}

#endif

