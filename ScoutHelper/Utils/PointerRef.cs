using System.Runtime.CompilerServices;

namespace ScoutHelper.Utils;

/// <summary>
/// a class for maintaining a pointer to a value.
///
/// refs can't be stored in collections. for use cases that require it, such as
/// generating long lived refs dynamically, this class can hold a ref in a more
/// easily managed object instance.
/// </summary>
/// <typeparam name="T">the type of value stored by the ref</typeparam>
public class PointerRef<T> {
	private T _refValue;

	public PointerRef(T refValue) {
		_refValue = refValue;
	}

	public T GetValue() => _refValue;

	public unsafe void* GetPointer() => Unsafe.AsPointer(ref _refValue);
}
