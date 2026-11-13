using AxCASSCALELib;
using SmartSoftware.SmartAgent.Interfaces;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace SmartSoftware.SmartAgent.Utils
{
    internal static class CCommon
    {
        public const int DF_ACTION_UPLOAD = 2;
        public const int DF_ACTION_DOWNLOAD = 3;

        public const int DF_DATA_VERSION_TYPE2 = 62;
        public const int DF_DATA_PLU_V06 = 98;

        public const int DF_STATE_CONNERROR = 20;
        public const int DF_STATE_SENDFAIL = 21;
        public const int DF_STATE_RECEIVETIMEOVER = 22;
        public const int DF_STATE_RETRYOVER = 8;

        public const int DF_RECV_SUCCESS = 1001;
        public const int DF_RECV_FAIL = 1002;
    }

    public static class CasScaleUtils
    {
        private const int Port = 20304;

        public static async Task SendPLUAsync(string ipAddress, List<IBarcodeScaleItem> items)
        {
            // ConnectionEx3 (the CAS SDK's own connect call) blocks its thread synchronously
            // and has no cancellation - if the IP is unreachable it can hang far past any
            // watchdog we set. Fail fast here with a real, cancellable TCP check first.
            using (var probe = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await probe.ConnectAsync(ipAddress, Port, cts.Token);
                }
                catch (Exception ex) when (ex is SocketException or OperationCanceledException)
                {
                    throw new Exception($"CL5200J: жинд холбогдож чадсангүй (IP: {ipAddress}:{Port})", ex);
                }
            }

            var tcs = new TaskCompletionSource();

            var thread = new Thread(() =>
            {
                Exception? failure = null;
                try
                {
                    using var worker = new CasScaleWorker(ipAddress, items);
                    worker.Run();
                    failure = worker.Error;
                }
                catch (Exception ex)
                {
                    failure = ex;
                }

                if (failure != null)
                    tcs.SetException(failure);
                else
                    tcs.SetResult();
            })
            {
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // Belt-and-suspenders: even after the TCP probe passes, the OCX's own connect/send
            // calls can still block past the internal 60s watchdog (it can only ask the STA
            // thread's message loop to exit - it can't interrupt a call already blocking that
            // thread). If that happens we give up and return an error rather than hang the
            // caller forever; the STA thread is left to finish/die on its own in the background.
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(75)));
            if (completed != tcs.Task)
                throw new Exception($"CL5200J: жингээс хариу ирэхгүй байна, хугацаа дууслаа (IP: {ipAddress})");

            await tcs.Task;
        }

        private sealed class CasScaleWorker : IDisposable
        {
            private const int Port = 20304;
            private const int DeviceModel = 5200;

            private readonly string _ip;
            private readonly List<IBarcodeScaleItem> _items;
            private readonly AxCasScale _scale = new();
            private readonly Form _host = new()
            {
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(-2000, -2000),
                Size = new System.Drawing.Size(1, 1),
                Opacity = 0
            };

            private string _version = "";
            private int _sentCount;
            private System.Threading.Timer? _watchdog;

            public Exception? Error { get; private set; }

            public CasScaleWorker(string ip, List<IBarcodeScaleItem> items)
            {
                _ip = ip;
                _items = items;
                _host.Controls.Add(_scale);
            }

            public void Run()
            {
                if (_items.Count == 0)
                    return;

                _scale.RecvEventString += OnRecvEventString;
                _scale.StateEvent += OnStateEvent;

                _host.HandleCreated += (_, _) =>
                {
                    _watchdog = new System.Threading.Timer(_ =>
                    {
                        Error ??= new Exception("CL5200J: хугацаа дууслаа (60 сек хариу ирсэнгүй)");
                        try { Application.ExitThread(); } catch { }
                    }, null, TimeSpan.FromSeconds(60), Timeout.InfiniteTimeSpan);

                    var rtn = _scale.ConnectionEx3(_ip, Port, -1, DeviceModel, "", 97);
                    if (rtn <= 0)
                    {
                        Error = new Exception($"CL5200J: холбогдож чадсангүй (IP: {_ip})");
                        Application.ExitThread();
                        return;
                    }

                    _scale.SendDataString(_ip, -1, DeviceModel, "", CCommon.DF_ACTION_UPLOAD, CCommon.DF_DATA_VERSION_TYPE2, "");
                };

                _host.CreateControl();
                var _ = _host.Handle;

                Application.Run();
            }

            private void OnStateEvent(object sender, _DCasScaleEvents_StateEventEvent e)
            {
                switch (e.iState)
                {
                    case CCommon.DF_STATE_CONNERROR:
                    case CCommon.DF_STATE_SENDFAIL:
                    case CCommon.DF_STATE_RECEIVETIMEOVER:
                    case CCommon.DF_STATE_RETRYOVER:
                        Error ??= new Exception($"CL5200J холболтын алдаа (IP: {e.sIP})");
                        Application.ExitThread();
                        break;
                }
            }

            private void OnRecvEventString(object sender, _DCasScaleEvents_RecvEventStringEvent e)
            {
                if (e.iTransType == CCommon.DF_ACTION_UPLOAD && e.iDataType == CCommon.DF_DATA_VERSION_TYPE2)
                {
                    if (e.iResult != CCommon.DF_RECV_SUCCESS)
                    {
                        Error = new Exception("CL5200J: хувилбар авахад алдаа гарлаа");
                        Application.ExitThread();
                        return;
                    }

                    _version = e.sData;
                    _scale.DisconnectOneEx(e.sIP, e.shScaleID);

                    var rtn = _scale.ConnectionEx3(_ip, Port, -1, DeviceModel, _version, 97);
                    if (rtn <= 0)
                    {
                        Error = new Exception($"CL5200J: холбогдож чадсангүй (IP: {_ip})");
                        Application.ExitThread();
                        return;
                    }

                    SendCurrentItem(e.sIP);
                }
                else if (e.iTransType == CCommon.DF_ACTION_DOWNLOAD && e.iDataType == CCommon.DF_DATA_PLU_V06)
                {
                    if (e.iResult != CCommon.DF_RECV_SUCCESS)
                    {
                        Error = new Exception($"CL5200J: PLU илгээхэд алдаа гарлаа (PLU: {_items[_sentCount].PLU})");
                        _scale.DisconnectOneEx(e.sIP, e.shScaleID);
                        Application.ExitThread();
                        return;
                    }

                    _sentCount++;
                    if (_sentCount >= _items.Count)
                    {
                        _scale.DisconnectOneEx(e.sIP, e.shScaleID);
                        Application.ExitThread();
                    }
                    else
                    {
                        SendCurrentItem(e.sIP);
                    }
                }
            }

            private void SendCurrentItem(string ip)
            {
                var data = BuildPLUData(_items[_sentCount], _sentCount + 1);
                _scale.SendDataString(ip, -1, DeviceModel, _version, CCommon.DF_ACTION_DOWNLOAD, CCommon.DF_DATA_PLU_V06, data);
            }

            private string BuildPLUData(IBarcodeScaleItem item, int pluNo)
            {
                var isUnit = item.IsFraction != "N";
                var sb = new StringBuilder();

                sb.Append("0001"); // Department No.
                sb.Append((item.PLU + "").PadLeft(6, '0')); // PLU No.
                sb.Append(isUnit ? "01" : "03"); // PLU type
                sb.Append("01"); // Unit Weight
                sb.Append($"{item.NewPrice:0000000000}"); // Unit Price
                sb.Append("0001"); // Group No.
                sb.Append((item.Barcode + "").PadLeft(13, '0')); // Item code
                sb.Append("0000"); // Tare No.
                sb.Append("0000000000"); // Tare
                sb.Append("000001"); // Pieces
                sb.Append("01"); // Quantity symbol
                sb.Append("000000"); // Sell by date
                sb.Append("000000"); // Sell by time
                sb.Append("000000"); // Pack date
                sb.Append("000000"); // Pack time
                sb.Append("000000"); // Produce date
                sb.Append((item.IngredientNo + "").PadLeft(6, '0')); // Ingredient No.
                sb.Append(isUnit ? "01" : "00"); // Use fixed price
                sb.Append("000000"); // Traceability No.
                sb.Append("000000"); // Origin No.
                sb.Append("0000"); // Nutrifact
                sb.Append("0004"); // Label No.
                sb.Append("0020"); // Aux Label No.
                sb.Append("0001"); // Barcode No.
                sb.Append("0002"); // Barcode2 No.
                sb.Append("0000"); // Sale Message
                sb.Append("0000000900"); // Special Price
                sb.Append("0000000000"); // Fixed Weight
                sb.Append("05"); // Picture No.
                sb.Append("01"); // Packdate flag
                sb.Append("01"); // Packtime flag
                sb.Append("01"); // Sellbydate flag
                sb.Append("01"); // Sellbytime flag
                sb.Append(Blank("", 8)); // Reserve
                sb.Append("0101010101010101"); // Name1~8 font sizes

                sb.Append(Blank(Conversion.ConvertStringToLatin(item.ItemName, 13), 55)); // Name1
                sb.Append(Blank(item.NewPrice.ToString("##,##0") + "₮", 55)); // Name2 (price + MNT symbol)
                sb.Append(Blank("name3", 55)); // Name3
                sb.Append(Blank($"SLPLU_{pluNo} name4", 55)); // Name4
                sb.Append(Blank($"SLPLU_{pluNo} name5", 55)); // Name5
                sb.Append(Blank($"SLPLU_{pluNo} name6", 55)); // Name6
                sb.Append(Blank($"SLPLU_{pluNo} name7", 55)); // Name7
                sb.Append(Blank($"SLPLU_{pluNo} name8", 55)); // Name8

                sb.Append("01"); // Korea Traceability flag
                sb.Append(Blank(DirectIngredientFiller, 4096)); // Direct Ingredient
                sb.Append(Blank("123456789012", 50)); // Ext Barcode
                sb.Append(Blank("02", 10)); // Prefix
                sb.Append("01"); // Tax No.
                sb.Append("00002"); // % Tare
                sb.Append("00010"); // Tare % limit
                sb.Append("000010"); // Cook By Date
                sb.Append("000020"); // Bonus
                sb.Append("0002"); // Reference Dept
                sb.Append("000003"); // Reference PLU
                sb.Append("0004"); // Coupled Dept
                sb.Append("000005"); // Coupled PLU
                sb.Append("02"); // Link PLU Count
                sb.Append("0006"); // Link Dept1
                sb.Append("000007"); // Link PLU1
                sb.Append("0008"); // Link Dept2
                sb.Append("000009"); // Link PLU2
                sb.Append(Blank($"test{pluNo}.jpg", 50)); // Image name

                return sb.ToString();
            }

            private const string DirectIngredientFiller =
                "1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890" +
                "2234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890" +
                "2234567890123456789012345678901234567890123456789012345678901234567890123456789012345678903333333333" +
                "44444444444444444444444444";

            private Encoding GetEncodingFromVersion()
            {
                if (string.IsNullOrEmpty(_version))
                    return Encoding.ASCII;

                var parts = _version.Split(',');
                if (!int.TryParse(parts[^1], out var countryCode))
                    return Encoding.ASCII;

                return countryCode == 1 /* Korea */ ? Encoding.GetEncoding(949) : Encoding.ASCII;
            }

            private string Blank(string data, int totalLength)
            {
                var encoding = GetEncodingFromVersion();
                var byteCount = encoding.GetByteCount(data);
                return data + new string(' ', Math.Max(0, totalLength - byteCount));
            }

            public void Dispose()
            {
                _watchdog?.Dispose();
                _scale.RecvEventString -= OnRecvEventString;
                _scale.StateEvent -= OnStateEvent;
                _host.Dispose();
            }
        }
    }
}
