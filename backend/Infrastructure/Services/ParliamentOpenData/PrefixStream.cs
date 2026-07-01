namespace Parlamento.Infrastructure.Services.ParliamentOpenData;

internal sealed class PrefixStream : Stream
{
    private readonly Stream _prefix;
    private readonly Stream _inner;
    private bool _prefixComplete;

    public PrefixStream(Stream prefix, Stream inner)
    {
        _prefix = prefix;
        _inner = inner;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!_prefixComplete)
        {
            var read = _prefix.Read(buffer, offset, count);
            if (read > 0)
            {
                return read;
            }

            _prefixComplete = true;
        }

        return _inner.Read(buffer, offset, count);
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (!_prefixComplete)
        {
            var read = await _prefix.ReadAsync(buffer, cancellationToken);
            if (read > 0)
            {
                return read;
            }

            _prefixComplete = true;
        }

        return await _inner.ReadAsync(buffer, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _prefix.Dispose();
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
