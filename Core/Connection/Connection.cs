namespace Core.Connection;

using System.Text;
using System.Net.Sockets;
using System.IO.Pipelines;
using Google.Protobuf;
using System.Buffers;

internal class Connection
{
    private Socket _socket;
    private SocketAsyncEventArgs _sendArgs;
    private Pipe _pipe;

    private IConnectionHandler _connectionHandler;

    internal Connection()
    {
        _socket = null!;

        _sendArgs = new SocketAsyncEventArgs();
        _sendArgs.Completed += OnSent;

        _connectionHandler = null!;

        _pipe = new Pipe();
    }

    internal void Initialize(Socket socket, IConnectionHandler handler)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _connectionHandler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    internal async Task Run(CancellationToken token)
    {
        _connectionHandler.OnConnected();

        var filling = FillPipeAsync(token);
        var reading = ReadPipeAsync(token);

        await Task.WhenAll(filling, reading);
    }

    internal void PostSend(byte[] buffer)
    {
        _sendArgs.SetBuffer(buffer, 0, buffer.Length);

        var pending = _socket.SendAsync(_sendArgs);
        if (!pending)
        {
            OnSent(null, _sendArgs);
            return;
        }
    }

    // [Producer] 소켓에서 데이터를 받아 파이프에 씀
    private async Task FillPipeAsync(CancellationToken ct)
    {
        var minimumBufferSize = 512;
        var writer = _pipe.Writer;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var memory = writer.GetMemory(minimumBufferSize);
                var bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None, ct);
                if (bytesRead == 0)
                {
                    _socket.Close();
                    _connectionHandler.OnDisconnected();
                    break;
                }

                writer.Advance(bytesRead);
                var result = await writer.FlushAsync(ct);
                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
        }

        await writer.CompleteAsync();
    }

    // [Consumer] 파이프에서 데이터를 읽어 패킷 조립
    private async Task ReadPipeAsync(CancellationToken ct)
    {
        var reader = _pipe.Reader;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await reader.ReadAsync(ct);

                var (packetId, packet) = ParsePacket(result.Buffer);
                _connectionHandler.OnReceived(packetId, packet);

                if (result.IsCompleted)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        await reader.CompleteAsync();
    }

    private (short, byte[]) ParsePacket(ReadOnlySequence<byte> buffer)
    {
        // 원래는 헤더부터 읽을 수 있는지 없는지 확인을 해야 하지만...
        // 그냥 한번에 다 받았다라고 가정하고 일단은 넘어기자
        var reader = new SequenceReader<byte>(buffer);
        if (reader.Remaining < 4)
        {
            buffer = buffer.Slice(reader.Position);
            return default;
        }

        reader.TryReadLittleEndian(out short packetId);
        reader.TryReadLittleEndian(out short payload);

        var sequence = buffer.Slice(reader.Position, payload);

        buffer = buffer.Slice(reader.Position);
        return (packetId, sequence.ToArray());
    }

    private void OnSent(object? sender, SocketAsyncEventArgs e)
    {
        _connectionHandler.OnSent();
    }
}
