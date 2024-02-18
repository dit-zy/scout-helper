using System.Runtime.CompilerServices;

namespace ScoutHelper.Utils;

public class PointerRef<T> {
	private T _refValue;
	
	public PointerRef(T refValue) {
		_refValue = refValue;
	}

	public T GetValue() => _refValue;

	public unsafe void* GetPointer() => Unsafe.AsPointer(ref _refValue);
}
