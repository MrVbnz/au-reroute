using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AuReroute;

public class CircularBufferWaveProvider : IWaveProvider
{
    private readonly WaveFormat waveFormat;
    private readonly byte[] buffer;
    private int writePosition;
    private int readPosition;
    private int bufferLength;
    private readonly object @lock = new object();

    public CircularBufferWaveProvider(WaveFormat waveFormat, int bufferSize)
    {
        this.waveFormat = waveFormat;
        buffer = new byte[bufferSize];
    }

    public void AddSamples(byte[] data, int offset, int count)
    {
        lock (@lock)
        {
            var spaceAtEnd = buffer.Length - writePosition;

            if (count <= spaceAtEnd)
            {
                Buffer.BlockCopy(data, offset, buffer, writePosition, count);
                writePosition = (writePosition + count) % buffer.Length;
            }
            else
            {
                Buffer.BlockCopy(data, offset, buffer, writePosition, spaceAtEnd);
                Buffer.BlockCopy(data, offset + spaceAtEnd, buffer, 0, count - spaceAtEnd);
                writePosition = count - spaceAtEnd;
            }

            var newBufferLength = bufferLength + count;
            if (newBufferLength > buffer.Length)
            {
                readPosition = (readPosition + (newBufferLength - buffer.Length)) % buffer.Length;
                bufferLength = buffer.Length;
            }
            else
            {
                bufferLength = newBufferLength;
            }
        }
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = 0;

        lock (@lock)
        {
            var bytesToRead = Math.Min(count, bufferLength);
            bytesRead = bytesToRead;

            var spaceAtEnd = this.buffer.Length - readPosition;

            if (bytesToRead <= spaceAtEnd)
            {
                Buffer.BlockCopy(this.buffer, readPosition, buffer, offset, bytesToRead);
                readPosition = (readPosition + bytesToRead) % this.buffer.Length;
            }
            else
            {
                Buffer.BlockCopy(this.buffer, readPosition, buffer, offset, spaceAtEnd);
                Buffer.BlockCopy(this.buffer, 0, buffer, offset + spaceAtEnd, bytesToRead - spaceAtEnd);
                readPosition = bytesToRead - spaceAtEnd;
            }

            bufferLength -= bytesToRead;
        }

        for (var i = bytesRead; i < count; i++)
            buffer[offset + i] = 0;

        return count;
    }

    public WaveFormat WaveFormat => waveFormat;
}


internal sealed class AudioRouter
{
    private readonly MMDevice captureDevice;
    private readonly int outputDeviceNumber;

    private WasapiLoopbackCapture? loopbackCapture;
    private WaveOutEvent? waveOut;
    private CircularBufferWaveProvider? bufferedWaveProvider;

    public AudioRouter(string captureDeviceId, int outputDeviceNumber)
    {
        var enumerator = new MMDeviceEnumerator();
        captureDevice = enumerator.GetDevice(captureDeviceId)
                        ?? throw new ArgumentException("Capture device not found.");
        this.outputDeviceNumber = outputDeviceNumber;
    }

    private int GetBufferLength()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length == 2 && int.TryParse(args[1], out var argsSize))
            return argsSize;
        return 4194304;
    }

    public void Start()
    {
        loopbackCapture = new WasapiLoopbackCapture(captureDevice);
        
        bufferedWaveProvider = new CircularBufferWaveProvider(loopbackCapture.WaveFormat, GetBufferLength());
  
        waveOut = new WaveOutEvent { DeviceNumber = outputDeviceNumber };
        waveOut.Init(bufferedWaveProvider);
        waveOut.Play();

        loopbackCapture.DataAvailable += (s, e) => { bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded); };
        loopbackCapture.RecordingStopped += (s, e) => Stop();
        loopbackCapture.StartRecording();
    }

    public void Stop()
    {
        loopbackCapture?.StopRecording();
        loopbackCapture?.Dispose();
        loopbackCapture = null;
        waveOut?.Stop();
        waveOut?.Dispose();
        waveOut = null;
        bufferedWaveProvider = null;
    }
}