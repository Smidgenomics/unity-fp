// smidgens @ github

namespace Smidgenomics.Unity.FirstPerson
{
	using UnityEngine;

	[System.Serializable]
	internal sealed class CCGroundSettings
	{
		[SerializeField] internal LayerMask _layers;

		// how much to protrude collision sphere from bottom
		// [FieldGroup(GroupName.GROUND)]
		[Range(0.05f, 0.1f)]
		[SerializeField] internal float _sphereProtrusion = 0.08f;

		// collision sphere scale (relative to CC radius + skin)
		// [FieldGroup(GroupName.GROUND)]
		[Range(0.1f, 0.98f)]
		[SerializeField] internal float _sphereScale = 0.6f;
	
		// [FieldGroup(GroupName.SLOPE)]
		[Range(0.01f, 0.2f)]
		[SerializeField] internal float _groundScanLength = 0.1f;

		// [FieldGroup(GroupName.SLOPE)]
		[Range(5f, 70f)]
		[SerializeField] internal float _groundScanAngle = 45f;

		// [FieldGroup(GroupName.SLOPE)]
		[Range(3, 16)]
		[SerializeField] internal int _slopeScanSegments = 8;

		// [FieldGroup(GroupName.SLOPE)]
		[Range(2,10)]
		[SerializeField] internal int _slopeScansPerSegment = 3;

		// [FieldGroup(GroupName.MOTION)]
		[SerializeField] internal bool _shouldFadeMotion = true;

		// [FieldGroup(GroupName.MOTION)]
		[SerializeField] internal AnimationCurve _motionFadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

		// [FieldGroup(GroupName.MOTION)]
		[Range(0.1f, 10f)]
		[SerializeField] internal float _motionFadeDuration = 2f;

		// [FieldGroup(GroupName.GROUND)]
		[Tooltip("Use isGrounded from CC as additional authority")]
		[SerializeField] internal bool _includeIsGrounded;
	}
}