// smidgens @ github

namespace Smidgenomics.Unity.FirstPerson
{
	using UnityEngine;

	public static class FPMath
	{
		// get pos on unit sphere
		public static Vector3 GetUnitSpherePos(float phi, float theta)
		{
			return
			Quaternion.Euler(theta, phi, 0f)
			* Vector3.up;
		}
		
		// Align forward vector to surface normal
		public static Vector3 MoveAlongSurface
		(
			in Vector3 forward, // head needle
			in Vector3 surfaceNormal // neelix
		)
		{
			// ass planes
			var tempRight = Vector3.Cross(forward, surfaceNormal).normalized;
			var surfaceforward = Vector3.Cross(tempRight, surfaceNormal).normalized;
			return -surfaceforward.normalized; // this kinda just works...not sure why
		}
	}
}