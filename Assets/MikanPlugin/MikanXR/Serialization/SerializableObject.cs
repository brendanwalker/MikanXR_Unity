using System;
using System.Reflection;

namespace MikanXR
{
	public class SerializableObject<T> where T : class
	{
		private long _runtimeClassId= 0;
		public long RuntimeClassId => _runtimeClassId;

		private object _instance;
		public T Instance => _instance as T;

		public SerializableObject() 
		{ 
		}

		public SerializableObject(T instance)
		{
			setInstance(instance);
		}

		public void setInstance(T instance)
		{
			Type instanceType = instance.GetType();
			FieldInfo classIdField= 
				instanceType.GetField(
					"classId", 
					BindingFlags.Public | BindingFlags.Static);

			// All SerializableObject types have a static "classId" field
			// corresponding to the Refureku class ID in the C++ code
			_runtimeClassId = (long)classIdField.GetValue(null);
			_instance = instance;
		}
	}
}
