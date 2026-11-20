// smidgens @ github

namespace Smidgenomics.Unity.FirstPerson
{
	using System;

	[AttributeUsage(AttributeTargets.Field)]
	internal sealed class FieldGroupAttribute : Attribute
	{
		public FieldGroupAttribute(string name)
		{
			groupName = name;
		}

		internal readonly string groupName;
	}
}