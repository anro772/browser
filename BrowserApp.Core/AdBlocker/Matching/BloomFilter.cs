using System.Security.Cryptography;
using System.Text;

namespace BrowserApp.Core.AdBlocker.Matching;

/// <summary>
/// Bloom filter for fast "definitely not blocked" checks.
/// Probabilistic data structure with 0.1% false positive rate.
/// </summary>
public class BloomFilter
{
    private readonly BitArray _bits;
    private readonly int _hashCount;
    private int _itemCount = 0;

    /// <summary>
    /// Creates a Bloom filter with specified capacity and false positive rate.
    /// </summary>
    /// <param name="expectedItems">Expected number of items to add</param>
    /// <param name="falsePositiveRate">Desired false positive rate (default 0.001 = 0.1%)</param>
    public BloomFilter(int expectedItems, double falsePositiveRate = 0.001)
    {
        // Calculate optimal bit array size
        var bitCount = (int)Math.Ceiling(
            -expectedItems * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2))
        );

        // Calculate optimal number of hash functions
        _hashCount = (int)Math.Ceiling((bitCount / (double)expectedItems) * Math.Log(2));

        _bits = new BitArray(bitCount);
    }

    /// <summary>
    /// Adds an item to the Bloom filter.
    /// </summary>
    public void Add(string item)
    {
        var hashes = GetHashes(item);

        foreach (var hash in hashes)
        {
            var index = (int)(hash % (uint)_bits.Length);
            _bits[index] = true;
        }

        _itemCount++;
    }

    /// <summary>
    /// Checks if an item might be in the set.
    /// Returns false = definitely not in set (100% accurate)
    /// Returns true = might be in set (0.1% false positive rate)
    /// </summary>
    public bool MightContain(string item)
    {
        var hashes = GetHashes(item);

        foreach (var hash in hashes)
        {
            var index = (int)(hash % (uint)_bits.Length);
            if (!_bits[index])
            {
                return false; // Definitely not in set
            }
        }

        return true; // Might be in set
    }

    /// <summary>
    /// Generates multiple hash values for an item.
    /// Uses SHA256 with different seeds.
    /// </summary>
    private uint[] GetHashes(string item)
    {
        var hashes = new uint[_hashCount];
        var bytes = Encoding.UTF8.GetBytes(item.ToLowerInvariant());

        using (var sha256 = SHA256.Create())
        {
            for (int i = 0; i < _hashCount; i++)
            {
                // Use different seed for each hash
                var seedBytes = BitConverter.GetBytes(i);
                var combined = new byte[bytes.Length + seedBytes.Length];
                Buffer.BlockCopy(bytes, 0, combined, 0, bytes.Length);
                Buffer.BlockCopy(seedBytes, 0, combined, bytes.Length, seedBytes.Length);

                var hash = sha256.ComputeHash(combined);
                hashes[i] = BitConverter.ToUInt32(hash, 0);
            }
        }

        return hashes;
    }

    /// <summary>
    /// Gets the approximate false positive rate based on current fill.
    /// </summary>
    public double GetFalsePositiveRate()
    {
        int setBits = 0;
        for (int i = 0; i < _bits.Length; i++)
        {
            if (_bits[i]) setBits++;
        }
        var fillRatio = setBits / (double)_bits.Length;
        return Math.Pow(fillRatio, _hashCount);
    }

    public int ItemCount => _itemCount;
    public int BitCount => _bits.Length;
}

/// <summary>
/// BitArray implementation for Bloom filter.
/// </summary>
internal class BitArray
{
    private readonly bool[] _bits;

    public BitArray(int length)
    {
        _bits = new bool[length];
    }

    public bool this[int index]
    {
        get => _bits[index];
        set => _bits[index] = value;
    }

    public int Length => _bits.Length;
}
