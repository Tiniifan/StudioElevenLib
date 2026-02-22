using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
#endif

namespace StudioElevenLib.Tools
{
    /// <summary>
    /// Represents a view over a subsection of data. This data can originate from an existing byte array
    /// or a specific segment (defined by an offset and size) of a larger base Stream.
    /// The class provides methods to lazily load the data from the base stream into memory and perform operations on it.
    /// </summary>
    public class SubMemoryStream
    {
        /// <summary>
        /// The starting offset of the sub-stream within the base stream.
        /// </summary>
        public long Offset;

        /// <summary>
        /// The total size in bytes of the sub-stream.
        /// </summary>
        public long Size;

        /// <summary>
        /// The byte content of the sub-stream. This is populated either directly via the constructor or by reading from the BaseStream.
        /// </summary>
        public byte[] ByteContent;

        /// <summary>
        /// The underlying base stream from which this sub-stream is derived. Can be null if created from a byte array.
        /// </summary>
        public Stream BaseStream;

        /// <summary>
        /// A color associated with this data segment, potentially for UI purposes like highlighting in a hex viewer.
        /// </summary>
        public Color Color = Color.Black;

        // Cache to optimize repeated accesses
        private bool _isContentLoaded = false;
        private readonly object _readLock = new object();

        // Reusable buffer to avoid repeated allocations
        private static readonly ThreadLocal<byte[]> _threadLocalBuffer = new ThreadLocal<byte[]>(() => new byte[81920]); // 80KB buffer

        /// <summary>
        /// Initializes a new instance of the <see cref="SubMemoryStream"/> class from a byte array.
        /// The content is considered immediately loaded.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
        public SubMemoryStream(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Offset = 0;
            Size = data.Length;
            ByteContent = data;
            _isContentLoaded = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubMemoryStream"/> class from a segment of a base stream.
        /// The content is not read until <see cref="Read"/> or <see cref="ReadAsync"/> is called (lazy loading).
        /// </summary>
        /// <param name="baseStream">The base stream to read from.</param>
        /// <param name="offset">The starting offset of the segment in the base stream.</param>
        /// <param name="size">The size of the segment.</param>
        /// <exception cref="ArgumentNullException">Thrown if baseStream is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if offset or size are negative.</exception>
        public SubMemoryStream(Stream baseStream, long offset, long size)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be non-negative");
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative");

            Offset = offset;
            Size = size;
            BaseStream = baseStream;
            _isContentLoaded = false;
        }

        /// <summary>
        /// Synchronously reads the content of the sub-stream from the BaseStream into the <see cref="ByteContent"/> buffer.
        /// This operation is performed only once due to caching. The method is thread-safe.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the base stream is not readable, not seekable, or too short for the defined range.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the stream is reached unexpectedly while reading.</exception>
        public void Read()
        {
            // Double-checked locking to prevent multiple readings
            if (_isContentLoaded)
                return;

            lock (_readLock)
            {
                if (_isContentLoaded)
                    return;

                if (BaseStream != null)
                {
                    // Safety checks
                    if (!BaseStream.CanRead)
                        throw new InvalidOperationException("BaseStream is not readable");

                    if (!BaseStream.CanSeek)
                        throw new InvalidOperationException("BaseStream is not seekable");

                    // Check that the stream size is not exceeded
                    if (BaseStream.Length < Offset + Size)
                        throw new InvalidOperationException("Stream is too short for the requested range");

                    ByteContent = new byte[Size];

                    // Direct reading when possible
                    if (Size > 0)
                    {
                        BaseStream.Seek(Offset, SeekOrigin.Begin);
                        int totalBytesRead = 0;

                        // Chunk playback to manage large files
                        while (totalBytesRead < Size)
                        {
                            int bytesToRead = (int)Math.Min(Size - totalBytesRead, 65536);
                            int bytesRead = BaseStream.Read(ByteContent, totalBytesRead, bytesToRead);

                            if (bytesRead == 0)
                                throw new EndOfStreamException("Unexpected end of stream");

                            totalBytesRead += bytesRead;
                        }
                    }

                    _isContentLoaded = true;
                }
            }
        }

        /// <summary>
        /// Asynchronously reads the content of the sub-stream from the BaseStream into the <see cref="ByteContent"/> buffer.
        /// This operation is performed only once due to caching. The method is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the base stream is not readable, not seekable, or too short for the defined range.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the stream is reached unexpectedly while reading.</exception>
        public async Task ReadAsync(CancellationToken cancellationToken = default)
        {
            if (_isContentLoaded)
                return;

            // Using a SemaphoreSlim for async thread-safety
            using (var semaphore = new SemaphoreSlim(1, 1))
            {
                await semaphore.WaitAsync(cancellationToken);

                try
                {
                    if (_isContentLoaded)
                        return;

                    if (BaseStream != null)
                    {
                        if (!BaseStream.CanRead)
                            throw new InvalidOperationException("BaseStream is not readable");

                        if (!BaseStream.CanSeek)
                            throw new InvalidOperationException("BaseStream is not seekable");

                        if (BaseStream.Length < Offset + Size)
                            throw new InvalidOperationException("Stream is too short for the requested range");

                        ByteContent = new byte[Size];

                        if (Size > 0)
                        {
                            BaseStream.Seek(Offset, SeekOrigin.Begin);
                            int totalBytesRead = 0;

                            while (totalBytesRead < Size)
                            {
                                int bytesToRead = (int)Math.Min(Size - totalBytesRead, 65536);
                                int bytesRead = await BaseStream.ReadAsync(ByteContent, totalBytesRead, bytesToRead, cancellationToken);

                                if (bytesRead == 0)
                                    throw new EndOfStreamException("Unexpected end of stream");

                                totalBytesRead += bytesRead;
                            }
                        }

                        _isContentLoaded = true;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Seeks the base stream to the starting offset of this sub-stream.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="BaseStream"/> is null or not seekable.</exception>
        public void Seek()
        {
            if (BaseStream == null)
                throw new InvalidOperationException("BaseStream is null");

            if (!BaseStream.CanSeek)
                throw new InvalidOperationException("BaseStream is not seekable");

            BaseStream.Seek(Offset, SeekOrigin.Begin);
        }

        /// <summary>
        /// Reads a sequence of bytes from the sub-stream.
        /// If the content is loaded, it reads from the in-memory buffer. Otherwise, it reads directly from the BaseStream
        /// at its current position, respecting the sub-stream's boundaries.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data.</param>
        /// <param name="count">The maximum number of bytes to be read.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if the end of the sub-stream is reached.</returns>
        /// <exception cref="ArgumentNullException">Thrown if buffer is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for invalid offset or count.</exception>
        /// <exception cref="ArgumentException">Thrown if the buffer is too small.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the BaseStream is not readable.</exception>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length)
                throw new ArgumentException("Buffer too small");

            if (BaseStream == null)
                return 0;

            if (!BaseStream.CanRead)
                throw new InvalidOperationException("BaseStream is not readable");

            // Use ByteContent if available
            if (_isContentLoaded && ByteContent != null)
            {
                long currentPosition = BaseStream.Position - Offset;
                if (currentPosition < 0 || currentPosition >= Size)
                    return 0;

                int bytesToRead = (int)Math.Min(count, (int)(Size - currentPosition));
                Array.Copy(ByteContent, currentPosition, buffer, offset, bytesToRead);
                return bytesToRead;
            }

            // Optimized calculation of remaining bytes
            long streamPosition = BaseStream.Position;
            long endPosition = Offset + Size;

            if (streamPosition >= endPosition)
                return 0;

            long remainingBytes = endPosition - streamPosition;
            int bytesToReadFromStream = (int)Math.Min(count, (int)remainingBytes);

            // Play directly from the stream
            return BaseStream.Read(buffer, offset, bytesToReadFromStream);
        }

        /// <summary>
        /// Copies the entire content of this sub-stream to another stream, using a default buffer size.
        /// </summary>
        /// <param name="destination">The stream to which the contents will be copied.</param>
        public void CopyTo(Stream destination)
        {
            CopyTo(destination, 81920);
        }

        /// <summary>
        /// Copies the entire content of this sub-stream to another stream.
        /// Uses the cached <see cref="ByteContent"/> if available; otherwise, reads directly from the <see cref="BaseStream"/>.
        /// </summary>
        /// <param name="destination">The stream to which the contents will be copied.</param>
        /// <param name="bufferSize">The size of the buffer to use for copying.</param>
        /// <exception cref="ArgumentNullException">Thrown if destination is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if bufferSize is not positive.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the destination is not writable or the source is not readable/seekable.</exception>
        public void CopyTo(Stream destination, int bufferSize)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be positive");

            if (!destination.CanWrite)
                throw new InvalidOperationException("Destination stream is not writable");

            // Use ByteContent if available
            if (_isContentLoaded && ByteContent != null)
            {
                destination.Write(ByteContent, 0, ByteContent.Length);
                return;
            }

            // If no BaseStream, try reading first
            if (BaseStream == null)
            {
                if (ByteContent != null)
                {
                    destination.Write(ByteContent, 0, ByteContent.Length);
                }
                return;
            }

            if (!BaseStream.CanRead)
                throw new InvalidOperationException("BaseStream is not readable");

            if (!BaseStream.CanSeek)
                throw new InvalidOperationException("BaseStream is not seekable");

            // Use thread-local buffer to avoid allocations
            byte[] buffer = _threadLocalBuffer.Value;
            if (buffer.Length < bufferSize)
            {
                buffer = new byte[bufferSize];
                _threadLocalBuffer.Value = buffer;
            }

            long currentOffset = Offset;
            long remainingBytes = Size;

            BaseStream.Seek(currentOffset, SeekOrigin.Begin);

            while (remainingBytes > 0)
            {
                int bytesToRead = (int)Math.Min(remainingBytes, buffer.Length);
                int bytesRead = BaseStream.Read(buffer, 0, bytesToRead);

                if (bytesRead == 0)
                    break; // End of stream

                destination.Write(buffer, 0, bytesRead);
                remainingBytes -= bytesRead;
            }
        }

        /// <summary>
        /// Asynchronously copies the entire content of this sub-stream to another stream.
        /// </summary>
        /// <param name="destination">The stream to which the contents will be copied.</param>
        /// <param name="bufferSize">The size of the buffer to use for copying. Defaults to 81920.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if destination is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if bufferSize is not positive.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the destination is not writable or the source is not readable/seekable.</exception>
        public async Task CopyToAsync(Stream destination, int bufferSize = 81920, CancellationToken cancellationToken = default)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be positive");

            if (!destination.CanWrite)
                throw new InvalidOperationException("Destination stream is not writable");

            // Use ByteContent if available
            if (_isContentLoaded && ByteContent != null)
            {
                await destination.WriteAsync(ByteContent, 0, ByteContent.Length, cancellationToken);
                return;
            }

            if (BaseStream == null)
            {
                if (ByteContent != null)
                {
                    await destination.WriteAsync(ByteContent, 0, ByteContent.Length, cancellationToken);
                }
                return;
            }

            if (!BaseStream.CanRead)
                throw new InvalidOperationException("BaseStream is not readable");

            if (!BaseStream.CanSeek)
                throw new InvalidOperationException("BaseStream is not seekable");

            var buffer = new byte[bufferSize];
            long currentOffset = Offset;
            long remainingBytes = Size;

            BaseStream.Seek(currentOffset, SeekOrigin.Begin);

            while (remainingBytes > 0)
            {
                int bytesToRead = (int)Math.Min(remainingBytes, buffer.Length);
                int bytesRead = await BaseStream.ReadAsync(buffer, 0, bytesToRead, cancellationToken);

                if (bytesRead == 0)
                    break;

                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                remainingBytes -= bytesRead;
            }
        }

        /// <summary>
        /// Releases the memory used by the <see cref="ByteContent"/> buffer and resets the content loaded flag.
        /// The content can be re-read from the BaseStream later if needed.
        /// </summary>
        public void ReleaseMemory()
        {
            lock (_readLock)
            {
                ByteContent = null;
                _isContentLoaded = false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the sub-stream's content has been loaded into memory.
        /// </summary>
        public bool IsContentLoaded => _isContentLoaded;

        /// <summary>
        /// Extracts a specific portion of the sub-stream's data as a new byte array.
        /// If content is loaded, it copies from the in-memory buffer. Otherwise, it reads directly from the BaseStream.
        /// </summary>
        /// <param name="startIndex">The starting index within the sub-stream.</param>
        /// <param name="length">The number of bytes to extract.</param>
        /// <returns>A new byte array containing the specified portion of the data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the requested range is outside the bounds of the sub-stream.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no data source is available or the BaseStream is not usable.</exception>
        public byte[] GetBytes(int startIndex, int length)
        {
            if (startIndex < 0 || length < 0 || startIndex + length > Size)
                throw new ArgumentOutOfRangeException();

            if (_isContentLoaded && ByteContent != null)
            {
                var result = new byte[length];
                Array.Copy(ByteContent, startIndex, result, 0, length);
                return result;
            }

            if (BaseStream == null)
                throw new InvalidOperationException("No data source available");

            if (!BaseStream.CanRead || !BaseStream.CanSeek)
                throw new InvalidOperationException("BaseStream must be readable and seekable");

            var buffer = new byte[length];
            BaseStream.Seek(Offset + startIndex, SeekOrigin.Begin);
            int bytesRead = BaseStream.Read(buffer, 0, length);

            if (bytesRead < length)
            {
                Array.Resize(ref buffer, bytesRead);
            }

            return buffer;
        }
    }
}