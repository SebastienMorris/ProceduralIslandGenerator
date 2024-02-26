using Unity.Mathematics;

public readonly struct SmallXXHash
{
    private const uint primeA = 0b10011110001101110111100110110001;
    private const uint primeB = 0b10000101111010111100101001110111;
    private const uint primeC = 0b11000010101100101010111000111101;
    private const uint primeD = 0b00100111110101001110101100101111;
    private const uint primeE = 0b00010110010101100110011110110001;

    private readonly uint accumulator;

    public SmallXXHash(uint accumulator)
    {
        this.accumulator = accumulator;
    }

    public static implicit operator uint (SmallXXHash hash)
    {
        uint avalanche = hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= primeB;
        avalanche ^= avalanche >> 13;
        avalanche *= primeC;
        avalanche ^= avalanche >> 16;
        return avalanche;
    }

    public static implicit operator SmallXXHash(uint accumulator)
    { 
        return new SmallXXHash(accumulator);
    }

    public static implicit operator SmallXXHash4(SmallXXHash hash)
    {
        return new SmallXXHash4(hash.accumulator);
    }

    public static SmallXXHash Seed(int seed)
    {
        return (uint)seed + primeE;
    }

    public SmallXXHash Eat(int data)
    {
        return RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;
    }

    public SmallXXHash Eat(byte data)
    {
        return RotateLeft(accumulator + data * primeE, 11) * primeA;
    }

    private static uint RotateLeft(uint data, int steps)
    {
        return (data << steps) | (data >> 32 - steps);
    }
}

public readonly struct SmallXXHash4
{
    private const uint primeB = 0b10000101111010111100101001110111;
    private const uint primeC = 0b11000010101100101010111000111101;
    private const uint primeD = 0b00100111110101001110101100101111;
    private const uint primeE = 0b00010110010101100110011110110001;

    private readonly uint4 accumulator;

    public SmallXXHash4(uint4 accumulator)
    {
        this.accumulator = accumulator;
    }

    public static implicit operator uint4 (SmallXXHash4 hash)
    {
        uint4 avalanche = hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= primeB;
        avalanche ^= avalanche >> 13;
        avalanche *= primeC;
        avalanche ^= avalanche >> 16;
        return avalanche;
    }

    public static implicit operator SmallXXHash4(uint4 accumulator)
    { 
        return new SmallXXHash4(accumulator);
    }

    public static SmallXXHash4 Seed(int4 seed)
    {
        return (uint4)seed + primeE;
    }

    public SmallXXHash4 Eat(int4 data)
    {
        return RotateLeft(accumulator + (uint4)data * primeC, 17) * primeD;
    }

    private static uint4 RotateLeft(uint4 data, int steps)
    {
        return (data << steps) | (data >> 32 - steps);
    }
}
