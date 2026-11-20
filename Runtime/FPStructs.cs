// smidgens @ github

namespace Smidgenomics.Unity.FirstPerson
{
	using System;
	using UnityEngine.InputSystem;

	[Serializable]
	internal struct InputSettings
	{
		public InputActionReference lookAction;
		public InputActionReference moveAction;
		public InputActionReference sprintAction;
		public InputActionReference crouchAction;
		public InputActionReference jumpAction;
		public EFPCrouchMode crouchMode;
	}
}

namespace Smidgenomics.Unity.FirstPerson
{
	using System;
	using UnityEngine;

	[Serializable]
	public struct FPClimbingConfig
	{
		public static readonly FPClimbingConfig Default = new()
		{
			verticalSpeed = 7f,
			lateralSpeed = 5f,
			detachForce = 10f,
			lateralFactor = 0.5f,
		};

		[Min(0.1f)]
		public float verticalSpeed;
		[Min(0.1f)]
		public float lateralSpeed;
		[Range(0.1f,1f)]
		public float lateralFactor;
		[Min(1f)]
		public float detachForce;
	}
}

namespace Smidgenomics.Unity.FirstPerson
{
	using UnityEngine;

	// 
	internal struct MotionInfo
	{
		public Vector3 movement;
		public float yaw;
	}
}