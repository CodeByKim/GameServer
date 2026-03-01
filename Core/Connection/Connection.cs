namespace Core.Connection;

using System.Text;
using System.Net.Sockets;
using System.IO.Pipelines;

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

    internal async Task Run()
    {
        _connectionHandler.OnConnected();

        var ct = new CancellationToken();
        var filling = FillPipeAsync(ct);
        var reading = ReadPipeAsync(ct);

        await Task.WhenAll(filling, reading);
    }

    internal void PostSend(string message)
    {
        var buf = Encoding.UTF8.GetBytes(message);
        _sendArgs.SetBuffer(buf, 0, buf.Length);

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

        await writer.CompleteAsync();
    }

    // [Consumer] 파이프에서 데이터를 읽어 패킷 조립
    private async Task ReadPipeAsync(CancellationToken ct)
    {
        var reader = _pipe.Reader;

        while (!ct.IsCancellationRequested)
        {
            var result = await reader.ReadAsync(ct);
            var buffer = result.Buffer;

            var message = Encoding.UTF8.GetString(buffer);
            _connectionHandler.OnReceived(message);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync();
    }

    private void OnSent(object? sender, SocketAsyncEventArgs e)
    {
        _connectionHandler.OnSent();
    }
}
