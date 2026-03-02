namespace Core.Utils;

using System;

public static class Uuid
{
    // 설정값 (필요에 따라 변경)
    private const int MachineIdBits = 5;
    private const int SequenceBits = 7;

    private const int MachineIdShift = SequenceBits;
    private const int TimestampShift = SequenceBits + MachineIdBits;
    private const int SequenceMask = -1 ^ (-1 << SequenceBits);

    // 2024년 1월 1일 0시(UTC) 기준 (수정 가능)
    private static readonly long Epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    // 상태 저장용 정적 필드 (전역 공유)
    private static int _machineId = 0; // 기본값 0
    private static long _lastTimestamp = -1L;
    private static int _sequence = 0;

    // 멀티스레드 동기화를 위한 락 객체
    private static readonly object _lock = new object();

    /// <summary>
    /// (옵션) 앱 시작 시 한 번만 호출하여 머신 ID를 설정합니다.
    /// 호출하지 않으면 기본값 0이 사용됩니다.
    /// </summary>
    public static void Configure(int machineId)
    {
        // 범위 체크 등은 생략했으나 추가하는 것이 좋습니다.
        _machineId = machineId;
    }

    /// <summary>
    /// 유니크한 8바이트 ID를 생성합니다.
    /// </summary>
    public static long Create()
    {
        lock (_lock) // static 메서드는 전역 공유되므로 반드시 잠금 필요
        {
            var timestamp = DateTime.UtcNow.Ticks;

            if (timestamp < _lastTimestamp)
            {
                // 시간이 역행한 경우 (NTP 동기화 등) - 예외 처리 혹은 대기 로직
                // 여기서는 단순하게 현재 시간을 이전 시간으로 맞춤 (위험할 수 있음)
                timestamp = _lastTimestamp;
            }

            if (_lastTimestamp == timestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                if (_sequence == 0)
                {
                    // 같은 밀리초 안에 시퀀스가 다 찼다면 다음 밀리초까지 대기
                    while (timestamp <= _lastTimestamp)
                    {
                        timestamp = DateTime.UtcNow.Ticks;
                    }
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - Epoch) << TimestampShift) |
                   ((long)_machineId << MachineIdShift) |
                   (long)_sequence;
        }
    }
}