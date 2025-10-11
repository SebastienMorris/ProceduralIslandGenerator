using Unity.Mathematics;

using static Unity.Mathematics.math;

public static partial class Noise
{
	public struct Lattice1D<G, L> : INoise where G : struct, IGradient where L : struct, ILattice
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			LatticeSpan4 x = default(L).GetLatticeSpan4(positions.c0, frequency);

			var g = default(G);
			return g.EvaluateAfterInterpolation(lerp(g.Evaluate(hash.Eat(x.p0), x.g0), g.Evaluate(hash.Eat(x.p1), x.g1), x.t));
		}
	}
	
	public struct Lattice2D<G, L> : INoise where G : struct, IGradient where L : struct, ILattice
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency);
			LatticeSpan4 z = l.GetLatticeSpan4(positions.c2, frequency);

			SmallXXHash4 h0 = hash.Eat(x.p0);
			SmallXXHash4 h1 = hash.Eat(x.p1);

			var g = default(G);

			float4 L0 = lerp(g.Evaluate(h0.Eat(z.p0), x.g0, z.g0), g.Evaluate(h0.Eat(z.p1), x.g0, z.g1), z.t);
			float4 L1 = lerp(g.Evaluate(h1.Eat(z.p0), x.g1, z.g0), g.Evaluate(h1.Eat(z.p1), x.g1, z.g1), z.t);
			
			return  g.EvaluateAfterInterpolation(lerp(L0, L1, x.t));
		}
	}
	
	public struct Lattice3D<G, L> : INoise where G : struct, IGradient where L : struct, ILattice
	{
		public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
		{
			var l = default(L);
			
			LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency);
			LatticeSpan4 y = l.GetLatticeSpan4(positions.c1, frequency);
			LatticeSpan4 z = l.GetLatticeSpan4(positions.c2, frequency);

			SmallXXHash4 h0 = hash.Eat(x.p0);
			SmallXXHash4 h1 = hash.Eat(x.p1);

			SmallXXHash4 h00 = h0.Eat(y.p0);
			SmallXXHash4 h01 = h0.Eat(y.p1);
			SmallXXHash4 h10 = h1.Eat(y.p0);
			SmallXXHash4 h11 = h1.Eat(y.p1);

			var g = default(G);

			float4 L00 = lerp(g.Evaluate(h00.Eat(z.p0), x.g0, y.g0, z.g0), g.Evaluate(h00.Eat(z.p1), x.g0, y.g0, z.g1), z.t);
			float4 L01 = lerp(g.Evaluate(h01.Eat(z.p0), x.g0, y.g1, z.g0), g.Evaluate(h01.Eat(z.p1), x.g0, y.g1, z.g1), z.t);
			
			float4 L0 = lerp(L00, L01, y.t);
			
			float4 L10 = lerp(g.Evaluate(h10.Eat(z.p0), x.g1, y.g0, z.g0), g.Evaluate(h10.Eat(z.p1), x.g1, y.g0, z.g1), z.t);
			float4 L11 = lerp(g.Evaluate(h11.Eat(z.p0), x.g1, y.g1, z.g0), g.Evaluate(h11.Eat(z.p1), x.g1, y.g1, z.g1), z.t);
			
			float4 L1 = lerp(L10, L11, y.t);
			
			return  g.EvaluateAfterInterpolation(lerp(L0, L1, x.t));
		}
	}

	public struct LatticeSpan4
	{
		public int4 p0, p1;
		public float4 g0, g1;
		public float4 t;
	}

	public interface ILattice
	{
		LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency);
	}
	
	public struct LatticeNormal : ILattice
	{
		public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
		{
			coordinates *= frequency;
			float4 points = floor(coordinates);
			LatticeSpan4 span;
			span.p0 = (int4)points;
			span.p1 = span.p0 + 1;
			span.g0 = coordinates - span.p0;
			span.g1 = span.g0 - 1f;
			span.t = coordinates - points;
			span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
			return span;
		}	
	}
	
	public struct LatticeTilling : ILattice
	{
		public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
		{
			coordinates *= frequency;
			float4 points = floor(coordinates);
			LatticeSpan4 span;
			span.p0 = (int4)points;
			span.g0 = coordinates - span.p0;
			span.g1 = span.g0 - 1f;

			span.p0 -= (int4)ceil(points / frequency) * frequency;
			span.p0 = select(span.p0, span.p0 + frequency, span.p0 < 0);
			span.p1 = span.p0 + 1;
			span.p1 = select(span.p1, 0, span.p1 == frequency);
			
			span.t = coordinates - points;
			span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
			return span;
		}	
	}
}