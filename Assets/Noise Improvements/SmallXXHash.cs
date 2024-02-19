using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
