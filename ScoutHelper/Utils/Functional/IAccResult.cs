using System.Collections.Generic;

namespace ScoutHelper.Utils.Functional;

public interface IAccResults<out T, out E> {
	T Value { get; }
	IEnumerable<E> Errors { get; }
}
