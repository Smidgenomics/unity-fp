// smidgens @ github

namespace Smidgenomics.Unity.FirstPerson
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using UnityEngine;

	internal static class FPExtensions
	{
		public static bool HasLayer(this in LayerMask mask, int layer)
		{
			return mask == (mask | (1 << layer));
		}
		public static bool IsApproximatelyZero(this in float v) => Mathf.Approximately(0f, v);
		
		public static Matrix4x4 CopyMatrix(this Transform t) => Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);

		public static Matrix4x4 CopyMatrixLocal(this Transform t) => Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
		
		// Get position of capsule collider bottom
		public static Vector3 GetColliderBottom(this CharacterController cc, bool useSkin = true)
		{
			var skin = useSkin ? cc.skinWidth : 0f;
	
			// 
			var extents = cc.height < (cc.radius * 2f)
			? cc.radius + skin // height is less than radius * 2 (unlikely scenario)
			: (cc.height) * 0.5f + skin;

			// offset from center
			var offset = Vector3.down * extents;

			// get position in world space
			return cc.transform.TransformPoint(cc.center + offset);
		}
		
		public static Rect SliceTop(this ref Rect r, in float h)
		{
			var r2 = r;
			r2.height = h;
			r.height -= h;
			r.position += new Vector2(0f, h);
			return r2;
		}
		
		public static IReadOnlyList<FieldInfo> FindInspectorFields<T>(this Type owner)
		{
			// NOTE: doesn't work properly for unity components, flags might need to be different

			var baseType = typeof(T);

			List<FieldInfo> fields = new List<FieldInfo>();
			LinkedList<Type> hierarchy = new LinkedList<Type>(); // linked for efficient prepend

			// traverse parent hierarchy, stop at base type
			Type currentType = owner;
			while (currentType != baseType && currentType != null)
			{
				hierarchy.AddFirst(currentType);
				currentType = currentType.BaseType;
			}

			var fieldFlags = BindingFlags.NonPublic
			| BindingFlags.Public
			| BindingFlags.DeclaredOnly
			| BindingFlags.Instance;

			// append fields in
			// same order as Unity would normally list them
			foreach (Type htype in hierarchy)
			{
				foreach (FieldInfo field in htype.GetFields(fieldFlags))
				{
					if (!IsInspectorField(field))
					{
						continue;
					}
					fields.Add(field);
				}
			}
			return fields;
		}

		// can field be drawn by inspector
		private static bool IsInspectorField(FieldInfo f)
		{
			// explicitly public but non-serialized
			if (f.IsPublic && f.IsDefined(typeof(NonSerializedAttribute)))
			{
				return false;
			}

			// explicitly hidden
			if (f.IsDefined(typeof(HideInInspector)))
			{
				return false;
			}

			// private, non serialized
			if (!f.IsPublic && !f.IsDefined(typeof(SerializeField)))
			{
				return false;
			}

			// at this point, either the field is public, or private and using SerializeField
			return true;
		}
	}
}