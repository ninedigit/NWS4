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
    
    private readonly IReadOnlyList<ReadOnlyMemory<byte>> chunks;

    private RawChunks()
    {
        this.chunks = new List<ReadOnlyMemory<byte>>();
    }
    
    internal RawChunks(IReadOnlyList<ReadOnlyMemory<byte>> chunks)
    {
        this.chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
    }

    public ReadOnlyMemory<byte> this[int index]
        => this.chunks[index];

    public byte[] ToArray()
        => this.chunks.SelectMany(i => i.ToArray()).ToArray();

    public async Task WriteAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        foreach (var chunk in this.chunks)
            await stream.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
    }

    public int Count
        => chunks.Count;

    public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator()
        => this.chunks.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();
}