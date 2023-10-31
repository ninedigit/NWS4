// Copyright(c) Microsoft Open Technologies, Inc.All rights reserved.
// Microsoft Open Technologies would like to thank its contributors, a list
// of whom are at http://aspnetwebstack.codeplex.com/wikipage?title=Contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License. You may
// obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied. See the License for the specific language governing permissions
// and limitations under the License.

// Taken from: https://github.com/aspnet/AspNetWebStack/blob/main/src/System.Net.Http.Formatting/PushStreamContent.cs

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

/// <summary>
/// Provides an <see cref="HttpContent"/> implementation that exposes an output <see cref="Stream"/>
/// which can be written to directly. The ability to push data to the output stream differs from the
/// <see cref="StreamContent"/> where data is pulled and not pushed.
/// </summary>
internal class PushStreamContent : HttpContent
{
    private readonly Func<Stream, HttpContent, TransportContext?, Task> _onStreamAvailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="PushStreamContent"/> class. The
    /// <paramref name="onStreamAvailable"/> action is called when an output stream
    /// has become available allowing the action to write to it directly. When the
    /// stream is closed, it will signal to the content that it has completed and the
    /// HTTP request or response will be completed.
    /// </summary>
    /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
    public PushStreamContent(Action<Stream, HttpContent, TransportContext?> onStreamAvailable)
        : this(Taskify(onStreamAvailable), (MediaTypeHeaderValue?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PushStreamContent"/> class.
    /// </summary>
    /// <param name="onStreamAvailable">The action to call when an output stream is available. When the
    /// output stream is closed or disposed, it will signal to the content that it has completed and the
    /// HTTP request or response will be completed.</param>
    public PushStreamContent(Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable)
        : this(onStreamAvailable, (MediaTypeHeaderValue?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given media type.
    /// </summary>
    /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
    /// <param name="mediaType">The value of the Content-Type content header on an HTTP response.</param>
    public PushStreamContent(Action<Stream, HttpContent, TransportContext?> onStreamAvailable, string mediaType)
        : this(Taskify(onStreamAvailable), new MediaTypeHeaderValue(mediaType))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given media type.
    /// </summary>
    /// <param name="onStreamAvailable">The action to call when an output stream is available. When the
    /// output stream is closed or disposed, it will signal to the content that it has completed and the
    /// HTTP request or response will be completed.</param>
    /// <param name="mediaType">The value of the Content-Type content header on an HTTP response.</param>
    public PushStreamContent(Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable, string mediaType)
        : this(onStreamAvailable, new MediaTypeHeaderValue(mediaType))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given <see cref="MediaTypeHeaderValue"/>.
    /// </summary>
    /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
    /// <param name="mediaType">The value of the Content-Type content header on an HTTP response.</param>
    public PushStreamContent(Action<Stream, HttpContent, TransportContext?> onStreamAvailable, MediaTypeHeaderValue mediaType)
        : this(Taskify(onStreamAvailable), mediaType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given <see cref="MediaTypeHeaderValue"/>.
    /// </summary>
    /// <param name="onStreamAvailable">The action to call when an output stream is available. When the
    /// output stream is closed or disposed, it will signal to the content that it has completed and the
    /// HTTP request or response will be completed.</param>
    /// <param name="mediaType">The value of the Content-Type content header on an HTTP response.</param>
    public PushStreamContent(Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable, MediaTypeHeaderValue? mediaType)
    {
        _onStreamAvailable = onStreamAvailable ?? throw new ArgumentNullException(nameof(onStreamAvailable));
        Headers.ContentType = mediaType ?? new MediaTypeHeaderValue(MediaTypeNames.Application.Octet);
    }

    private static Func<Stream, HttpContent, TransportContext?, Task> Taskify(
        Action<Stream, HttpContent, TransportContext?> onStreamAvailable)
    {
        if (onStreamAvailable == null)
            throw new ArgumentNullException(nameof(onStreamAvailable));

        return (Stream stream, HttpContent content, TransportContext? transportContext) =>
        {
            onStreamAvailable(stream, content, transportContext);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// When this method is called, it calls the action provided in the constructor with the output
    /// stream to write to. Once the action has completed its work it closes the stream which will
    /// close this content instance and complete the HTTP request or response.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
    /// <param name="context">The associated <see cref="TransportContext"/>.</param>
    /// <returns>A <see cref="Task"/> instance that is asynchronously serializing the object's content.</returns>
    [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is passed as task result.")]
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        TaskCompletionSource<bool> serializeToStreamTask = new TaskCompletionSource<bool>();

        Stream wrappedStream = new CompleteTaskOnCloseStream(stream, serializeToStreamTask);
        await _onStreamAvailable(wrappedStream, this, context);

        // wait for wrappedStream.Close/Dispose to get called.
        await serializeToStreamTask.Task;
    }

    /// <summary>
    /// Computes the length of the stream if possible.
    /// </summary>
    /// <param name="length">The computed length of the stream.</param>
    /// <returns><c>true</c> if the length has been computed; otherwise <c>false</c>.</returns>
    protected override bool TryComputeLength(out long length)
    {
        // We can't know the length of the content being pushed to the output stream.
        length = -1;
        return false;
    }

    internal class CompleteTaskOnCloseStream : DelegatingStream
    {
        private TaskCompletionSource<bool> _serializeToStreamTask;

        public CompleteTaskOnCloseStream(Stream innerStream, TaskCompletionSource<bool> serializeToStreamTask)
            : base(innerStream)
        {
            Contract.Assert(serializeToStreamTask != null);
            _serializeToStreamTask = serializeToStreamTask;
        }
        public override void Close()
        {
            // We don't Close the underlying stream because we don't own it. Dispose in this case just signifies
            // that the user's action is finished.
            _serializeToStreamTask.TrySetResult(true);
        }
    }
    
    // Forwards all calls to an inner stream except where overriden in a derived class.
    internal abstract class DelegatingStream : Stream
    {
        private Stream innerStream;

        #region Properties

        public override bool CanRead => innerStream.CanRead;

        public override bool CanSeek => innerStream.CanSeek;

        public override bool CanWrite => innerStream.CanWrite;

        public override long Length => innerStream.Length;

        public override long Position
        {
            get => innerStream.Position;
            set => innerStream.Position = value;
        }

        public override int ReadTimeout
        {
            get => innerStream.ReadTimeout;
            set => innerStream.ReadTimeout = value;
        }

        public override bool CanTimeout
        {
            get { return innerStream.CanTimeout; }
        }

        public override int WriteTimeout
        {
            get => innerStream.WriteTimeout;
            set => innerStream.WriteTimeout = value;
        }

        #endregion Properties

        protected DelegatingStream(Stream innerStream)
        {
            Contract.Assert(innerStream != null);
            this.innerStream = innerStream;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                innerStream.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Read

        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return innerStream.Read(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, 
            object? state)
        {
            return innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return innerStream.EndRead(asyncResult);
        }

        public override int ReadByte()
        {
            return innerStream.ReadByte();
        }
#if !NET_4
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }
#endif
        #endregion Read

        #region Write

        public override void Flush()
        {
            innerStream.Flush();
        }
#if !NET_4
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return innerStream.FlushAsync(cancellationToken);
        }
#endif
        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, 
            object? state)
        {
            return innerStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            innerStream.EndWrite(asyncResult);
        }

        public override void WriteByte(byte value)
        {
            innerStream.WriteByte(value);
        }
#if !NET_4
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }
#endif
        #endregion Write
    }
}