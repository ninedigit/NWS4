using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NineDigit.NWS4;

public sealed class RawChunks : IReadOnlyList<ReadOnlyMemory<byte>>
{
    public static readonly RawChunks Empty = new();
    
    private readonly IReadOnlyList<ReadOnlyMemory<byte>> _chunks;

    private RawChunks()
    {
        _chunks = new List<ReadOnlyMemory<byte>>();
    }
    
    internal RawChunks(IReadOnlyList<ReadOnlyMemory<byte>> chunks)
    {
        _chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
    }

    public ReadOnlyMemory<byte> this[int index]
        => _chunks[index];

    public byte[] ToArray()
        => _chunks.SelectMany(i => i.ToArray()).ToArray();

    public async Task WriteAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        foreach (var chunk in _chunks)
        {
#if NETSTANDARD2_0
            var buffer = chunk.ToArray();
            await stream.WriteAsync(buffer, 0, chunk.Length, cancellationToken).ConfigureAwait(false);
#else
            await stream.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
#endif
        }
    }

    public int Count
        => _chunks.Count;

    public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator()
        => _chunks.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}