using FsCheck;
using Random = FsCheck.Random;

namespace ScoutHelperTests.Util.FsCheck;

public static class FsCheckUtils {
	public static Gen<A> Zip<A>(Gen<A> a) => a;
	
	public static Gen<(A a, B b)> Zip<A, B>(Gen<A> a, Gen<B> b) =>
		a.SelectMany(_ => b, (a, b) => (a, b));

	public static Gen<(A a, B b, C c)> Zip<A, B, C>(Gen<A> a, Gen<B> b, Gen<C> c) =>
		Zip(a, b).SelectMany(_ => c, (x, c) => (x.a, x.b, c));

	public static Gen<(A a, B b, C c, D d)> Zip<A, B, C, D>(Gen<A> a, Gen<B> b, Gen<C> c, Gen<D> d) =>
		Zip(a, b, c).SelectMany(_ => d, (x, d) => (x.a, x.b, x.c, d));

	public static Gen<(A a, B b, C c, D d, E e)> Zip<A, B, C, D, E>(Gen<A> a, Gen<B> b, Gen<C> c, Gen<D> d, Gen<E> e) =>
		Zip(a, b, c, d).SelectMany(_ => e, (x, e) => (x.a, x.b, x.c, x.d, e));

	public static Gen<(A a, B b, C c, D d, E e, F f)> Zip<A, B, C, D, E, F>(
		Gen<A> a, Gen<B> b, Gen<C> c, Gen<D> d, Gen<E> e, Gen<F> f
	) => Zip(a, b, c, d, e).SelectMany(_ => f, (x, f) => (x.a, x.b, x.c, x.d, x.e, f));

	public static Gen<(A a, B b, C c, D d, E e, F f, G g)> Zip<A, B, C, D, E, F, G>(
		Gen<A> a, Gen<B> b, Gen<C> c, Gen<D> d, Gen<E> e, Gen<F> f, Gen<G> g
	) => Zip(a, b, c, d, e, f).SelectMany(_ => g, (x, g) => (x.a, x.b, x.c, x.d, x.e, x.f, g));

	public static Gen<(A a, B b, C c, D d, E e, F f, G g, H h)> Zip<A, B, C, D, E, F, G, H>(
		Gen<A> a, Gen<B> b, Gen<C> c, Gen<D> d, Gen<E> e, Gen<F> f, Gen<G> g, Gen<H> h
	) => Zip(a, b, c, d, e, f, g).SelectMany(_ => h, (x, h) => (x.a, x.b, x.c, x.d, x.e, x.f, x.g, h));

	public static Arbitrary<(A a, B b)> Zip<A, B>(Arbitrary<A> a, Arbitrary<B> b) =>
		Arb.From(Zip(a.Generator, b.Generator));

	public static Arbitrary<(A a, B b, C c)> Zip<A, B, C>(Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c) =>
		Arb.From(Zip(a.Generator, b.Generator, c.Generator));

	public static Arbitrary<(A a, B b, C c, D d)> Zip<A, B, C, D>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d
	) => Arb.From(Zip(a.Generator, b.Generator, c.Generator, d.Generator));

	public static Arbitrary<(A a, B b, C c, D d, E e)> Zip<A, B, C, D, E>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d, Arbitrary<E> e
	) => Arb.From(Zip(a.Generator, b.Generator, c.Generator, d.Generator, e.Generator));

	public static Arbitrary<(A a, B b, C c, D d, E e, F f)> Zip<A, B, C, D, E, F>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d, Arbitrary<E> e, Arbitrary<F> f
	) => Arb.From(Zip(a.Generator, b.Generator, c.Generator, d.Generator, e.Generator, f.Generator));

	public static Arbitrary<(A a, B b, C c, D d, E e, F f, G g)> Zip<A, B, C, D, E, F, G>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d, Arbitrary<E> e, Arbitrary<F> f, Arbitrary<G> g
	) => Arb.From(Zip(a.Generator, b.Generator, c.Generator, d.Generator, e.Generator, f.Generator, g.Generator));

	public static Arbitrary<(A a, B b, C c, D d, E e, F f, G g, H h)> Zip<A, B, C, D, E, F, G, H>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d, Arbitrary<E> e, Arbitrary<F> f, Arbitrary<G> g, Arbitrary<H> h
	) => Arb.From(Zip(a.Generator, b.Generator, c.Generator, d.Generator, e.Generator, f.Generator, g.Generator, h.Generator));
	
	public static Property ForAll<A>(
		Arbitrary<A> a,
		Action<A> body
	) => Prop.ForAll(a, body.Invoke);

	public static Property ForAll<A, B>(
		Arbitrary<A> a, Arbitrary<B> b,
		Action<A, B> body
	) => Prop.ForAll(Zip(a, b), x => body.Invoke(x.a, x.b));

	public static Property ForAll<A, B, C>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c,
		Action<A, B, C> body
	) => Prop.ForAll(Zip(a, b, c), x => body.Invoke(x.a, x.b, x.c));

	public static Property ForAll<A, B, C, D>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d,
		Action<A, B, C, D> body
	) => Prop.ForAll(Zip(a, b, c, d), x => body.Invoke(x.a, x.b, x.c, x.d));

	public static Property ForAll<A, B, C, D, E>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d, Arbitrary<E> e,
		Action<A, B, C, D, E> body
	) => Prop.ForAll(Zip(a, b, c, d, e), x => body.Invoke(x.a, x.b, x.c, x.d, x.e));

	public static Property ForAll<A, B, C, D, E, F>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d, Arbitrary<E> e, Arbitrary<F> f,
		Action<A, B, C, D, E, F> body
	) => Prop.ForAll(Zip(a, b, c, d, e, f), x => body.Invoke(x.a, x.b, x.c, x.d, x.e, x.f));

	public static Property ForAll<A, B, C, D, E, F, G>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d, Arbitrary<E> e, Arbitrary<F> f, Arbitrary<G> g,
		Action<A, B, C, D, E, F, G> body
	) => Prop.ForAll(Zip(a, b, c, d, e, f, g), x => body.Invoke(x.a, x.b, x.c, x.d, x.e, x.f, x.g));

	public static Property ForAll<A, B, C, D, E, F, G, H>(
		Arbitrary<A> a, Arbitrary<B> b, Arbitrary<C> c, Arbitrary<D> d, Arbitrary<E> e, Arbitrary<F> f, Arbitrary<G> g, Arbitrary<H> h,
		Action<A, B, C, D, E, F, G, H> body
	) => Prop.ForAll(Zip(a, b, c, d, e, f, g, h), x => body.Invoke(x.a, x.b, x.c, x.d, x.e, x.f, x.g, x.h));

	public static Arbitrary<T> ToArbitrary<T>(this Gen<T> gen) => Arb.From(gen);

	public static void Replay(this Property prop, int a, int b) {
		var config = Configuration.QuickThrowOnFailure;
		config.Replay = Random.StdGen.NewStdGen(a, b);
		prop.Check(config);
	}
}
