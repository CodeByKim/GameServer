namespace Core.Connection;

using System.Text;
using System.Net.Sockets;
using System.IO.Pipelines;
using Google.Protobuf;
using System.Buffers;
using System.Runtime.InteropServices;

struct PacketHeader
{
    public short Id { get; set; }
    public short Length { get; set; }
}

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

        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(ct);
                var buffer = result.Buffer;

                /*
                 * consumed:
                 *  - 파이프에게 이 위치 이전의 데이터는 확인 안해도 된다고 알려주는 위치
                 *  - 즉, 이미 패킷으로 완전히 처리해서 볼 필요가 없는 데이터
                 *
                 * examined:
                 *  - 파이프에게 여기까지 확인은 했다고 알려주는 위치
                 *  - 예를 들어 패킷 헤더를 확인했고, 페이로드의 사이즈도 확인함
                 *  - 하지만 페이로드가 다 도착하지 못해 패킷으로 파싱불가
                 *  - 이럴 때 examined는 buffer의 끝이 됨 (파싱은 못했지만, 끝까지 확인함)
                 *
                 * examined가 왜 필요할까?
                 *  - 이것은 reader.ReadAsync(ct)가 리턴되는 시점에 큰 영향이 있다.
                 *  - examined는 파이프에게 여기까진 확인했다는 위치를 알려주는 것이다.
                 *  - 만약 examined를 consumed와 같은 값으로 넘기면 어떻게 될까?
                 *  - 그럼 consumed위치 까지는 패킷을 파싱했고, consumed위치 이후는 데이터는 확인하지 않았다라는 뜻이된다.
                 *  - 따라서 추가 데이터가 수신되지 않더라도 즉시 다음 루프에서 reader.ReadAsync가 다시 리턴되고 examined 이 시작 지점으로 넘어온다.
                 *  - 이 때 examined를 버퍼의 끝으로 넘기면 버퍼의 끝까지는 확인 했으니, 버퍼의 끝 이후로 추가 데이터가 수신됐을 때 reader.ReadAsync(ct)가 리턴된다.
                 */
                var consumed = buffer.Start;
                var examined = buffer.End;

                try
                {
                    // 헤더와, 페이로드, 그리고 다음 reader의 시작 위치를 넘겨받음
                    while (TryParsePacket(buffer, out var header, out var payload, out var next))
                    {
                        _connectionHandler.OnReceived(header.Id, payload!);
                        buffer = buffer.Slice(next);
                        consumed = next;
                    }

                    /*
                     * result.IsCompleted: 더이상 writer가 데이터를 추가하지 않는다라는 뜻
                     *  - 네트워크에서 상대가 송신을 종료했거나, writer에서 Complete를 호출할 때를 의미
                     *  - 현재 버퍼가 비어있거나, 메시지가 완성되었다라는 의미가 아님
                     *  - 이 파이프에는 더이상 데이터가 쌓이지 않는다, writer의 생명주기가 끝났다라는 의미
                     */
                    if (result.IsCompleted)
                    {
                        if (buffer.Length > 0)
                        {
                            /*
                             * 비정상 종료로 볼 수 있음
                             * 남은 데이터가 완전한 패킷이 아님
                             * 로그 정도만 남기고 그냥 종료처리해도 무방함
                             */
                        }
                        break;
                    }
                }
                finally
                {
                    reader.AdvanceTo(consumed, examined);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }

    private bool TryParsePacket(
        ReadOnlySequence<byte> buffer,
        out PacketHeader header,
        out byte[]? payload,
        out SequencePosition nextPosition)
    {
        header = default;
        payload = default;
        nextPosition = buffer.Start;

        var reader = new SequenceReader<byte>(buffer);
        if (reader.Remaining < 4)
        {
            return false;
        }

        if (!SequenceMarshal.TryRead(ref reader, out header))
        {
            return false;
        }

        int length = header.Length;
        if (length < 0)
        {
            throw new InvalidDataException("Negative packet length.");
        }

        if (reader.Remaining < length)
        {
            return false;
        }

        // 현재 reader의 위치에서 패킷 길이까지 자름
        // reader의 위치는 헤더는 읽었으니 4바이트 뒤
        payload = buffer.Slice(reader.Position, length).ToArray();

        // 읽은 만큼 reader의 pos를 뒤로 옮기고
        reader.Advance(length);

        // 다음 시작 위치를 out 인자로 넘김
        nextPosition = reader.Position;
        return true;
    }

    private void OnSent(object? sender, SocketAsyncEventArgs e)
    {
        _connectionHandler.OnSent();
    }
}
