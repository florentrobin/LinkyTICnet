using System.IO.Ports;
using Microsoft.Extensions.Logging;

namespace LinkyTIC.API;
public class Reader
{
    private SerialPort? serialPort;
    private List<byte> bytes = new List<byte>();
    private readonly ILogger? logger;

    public Reader(ILogger? logger = null)
    {
        this.logger = logger;
    }

    public event EventHandler<FrameReceivedEventArgs>? FrameReceived;

    public void Start(string portName, TICMode ticMode)
    {
        var portNames = SerialPort.GetPortNames().ToList();
        if (!portNames.Contains(portName))
        {
            portNames = portNames.Where(_ => _.StartsWith("cu.") || _.StartsWith("tty.") || _.StartsWith("COM")).ToList();
            throw new ArgumentException($"The port {portName} does not exists. Availables ports : {string.Join(", ", portNames)}", nameof(portName));
        }
        logger?.LogInformation("Starting Serial connection to port {port}", portName);
        serialPort = new SerialPort(portName, ticMode.BaudRate(), Parity.Odd, 7, StopBits.One);
        serialPort.DataReceived += SerialPort_DataReceived;

        serialPort.Open();
    }

    public void Stop()
    {
        if (serialPort != null)
        {
            serialPort.Close();
            serialPort.DataReceived -= SerialPort_DataReceived;
        }
        bytes.Clear();

        logger?.LogInformation("Serial connection stopped");
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;
        var buffer = new byte[sp.ReadBufferSize];
        var read = sp.Read(buffer, 0, sp.BytesToRead);
        bytes.AddRange(buffer.Take(read));

        var startIndex = bytes.LastIndexOf(Frame.StartTeXt);
        var endIndex = bytes.LastIndexOf(Frame.EndTeXt);

        if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
        {
            var frameData = bytes.Skip(startIndex).Take(endIndex - startIndex).ToList();
            bytes = bytes.Skip(endIndex).ToList();

            FrameReceived?.Invoke(this, new FrameReceivedEventArgs(new Frame(frameData, logger)));
        }
    }

    public class FrameReceivedEventArgs : EventArgs
    {
        public FrameReceivedEventArgs(Frame frame)
        {
            Frame = frame;
        }

        public Frame Frame { get; }
    }
}

