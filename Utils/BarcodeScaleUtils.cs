using FluentFTP;
using FluentFTP.Helpers;
using SmartSoftware.SmartAgent.Interfaces;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace SmartSoftware.SmartAgent.Utils
{
    public static class BarcodeScaleUtils
    {
        //private AxCasScale _scale = new();
        //private TaskCompletionSource<bool>? _tcs;
        //private Dictionary<string, int> _sentCount = new();

        //private string _ipAddress = "";
        //private string _version = "";
        //private int _sendLimit = 0;

        //private List<BarcodeScaleItemModel> _items = new();
        //private List<BarcodeScaleItemModel> _ingredients = new();


        private static string itemSM120 = "{1},,0,0,0,1,1,0,0,0,0,0,0,0,1,0,0,0,{2:#0},17,0,33,11,{3}0000,,0,0,997,0,0,0,0,0,0,{3},0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,6,7,8,9,10,11,12,13,14,15,16,000000,0000,000000,0000,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,,6,\"{0}\",6,,0,,0,,0,0,0,0,0,0,0,0,0";
        private static string itemAclasLS2 = "{0}	{1}	2	1	{2}			{3}	{5}	{6}	0	0	0	12/30/1899 12:00:01 AM	0	{4}	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0		0	0	0	0	0	60	0	0	0	0	0	0		0		12/30/1899 12:00:01 AM	12/30/1899 12:00:01 AM	0	0	0		";
        private static string itemAclasLS2Old = "{0}	{1}	2	1	{2}			{3}	{5}	99	0	0	0	2014/9/22 15:06:00	0	{4}	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	1	0	0	0	0	0	0	0	0	0	0	0	0		0		1899/12/30	1899/12/30	0	0	";
        private static string ScaleBarCodeType { get; set; }

        public static bool CheckConnection(string ipAddress, int port)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(IPAddress.Parse(ipAddress), port));
                Thread.Sleep(200);
                socket.Disconnect(false);
                return true;
            }
            catch { return false; }
        }

        public static async Task SendDataToScale<T>(string type, string ipAddress, int port, List<T> items, string scaleBarCodeType = "150")
           where T : IBarcodeScaleItem
        {
            ScaleBarCodeType = scaleBarCodeType;
            if (type == "SM-120")
                await SendSM120(ipAddress, items);
            else if (type == "CL5200J")
                await CasScaleUtils.SendPLUAsync(ipAddress, items.Cast<IBarcodeScaleItem>().ToList());
            else if (type == "Aclas LS-2")
                await SendAclasLS2(ipAddress, items);
            else if (type == "Aclas LS-2 EN")
                await SendAclasLS2(ipAddress, items, isLatin: true);
            else if (type == "Aclas LS-2 RU")
                await SendAclasLS2(ipAddress, items, true, true);
            else
                await SendOther(type, ipAddress, port, items);
        }

        #region SM120

        public static async Task SendSM120<T>(string ipAddress, List<T> items)
         where T : IBarcodeScaleItem
        {
            var fileText = string.Empty;
            foreach (var item in items)
            {
                string itemName = Conversion.ConvertStringToLatin(item.ItemName, item.ItemName.Length);
                fileText += string.Format(itemSM120, itemName, item.PLU, item.NewPrice, item.Barcode)
                          + Environment.NewLine;
            }

            var tempDir = Path.GetTempPath();
            var pluFilePath = Path.Combine(tempDir, "plu0uall.csv");

            await File.WriteAllTextAsync(pluFilePath, fileText);

            try
            {
                await UploadFileToFtpAsync(ipAddress, "admin", "admin", pluFilePath, "/plu0uall.csv");
            }
            finally
            {
                if (File.Exists(pluFilePath))
                    File.Delete(pluFilePath);
            }
        }

        private static async Task UploadFileToFtpAsync(string server, string username, string password, string localPath, string remotePath)
        {
            using var client = new AsyncFtpClient(server, username, password);

            client.Config.ConnectTimeout = 10_000;
            client.Config.DataConnectionType = FtpDataConnectionType.PASV;

            FtpStatus status;
            try
            {
                await client.Connect();

                status = await client.UploadFile(
                    localPath,
                    remotePath,
                    FtpRemoteExists.Overwrite,
                    createRemoteDir: true
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"SM-120: FTP холболтын алдаа ({server})", ex);
            }
            finally
            {
                await client.Disconnect();
            }

            if (!status.IsSuccess())
                throw new Exception($"SM-120: файл илгээхэд алдаа гарлаа. {status}");
        }

        #endregion

        #region CL5200J

        //public class CCommon
        //{
        //    public const int DF_ACTION_UPLOAD = 2;
        //    public const int DF_ACTION_DOWNLOAD = 3;
        //    public const int DF_ACTION_DELETE = 4;
        //    public const int DF_ACTION_DELETEALL = 5;

        //    public const int DF_ACTION_FTP_PUT = 31;
        //    public const int DF_ACTION_FTP_GET = 32;
        //    public const int DF_ACTION_FTP_DELETE = 33;

        //    public const int DF_DATA_VERSION_TYPE1 = 61;
        //    public const int DF_DATA_VERSION_TYPE2 = 62;
        //    public const int DF_DATA_PLU_V02 = 9;
        //    public const int DF_DATA_PLU_V04 = 218;
        //    public const int DF_DATA_PLU_V05 = 97;
        //    public const int DF_DATA_PLU_V06 = 98;

        //    public const int DF_DATA_KEY_STANDARD_PLU = 77;
        //    public const int DF_DATA_KEY_STANDARD_FUNCTION = 78;
        //    public const int DF_DATA_KEY_5200_STANDARD_PLU = 187;
        //    public const int DF_DATA_KEY_5200_STANDARD_FUNCTION = 188;
        //    public const int DF_DATA_KEY_POLE_PLU = 79;
        //    public const int DF_DATA_KEY_POLE_FUNCTION = 80;
        //    public const int DF_DATA_KEY_HANGING_PLU = 81;
        //    public const int DF_DATA_KEY_HANGING_FUNCTION = 82;
        //    public const int DF_DATA_KEY_DOUBLE_PLU = 171;
        //    public const int DF_DATA_KEY_DOUBLE_FUNCTION = 172;
        //    public const int DF_DATA_KEY_SELF_PLU_12_EMART = 167;
        //    public const int DF_DATA_KEY_SELF_FUNCTION_12_EMART = 168;
        //    public const int DF_DATA_KEY_SELF_PLU_7200 = 169;
        //    public const int DF_DATA_KEY_SELF_DESIGN = 179;
        //    public const int DF_DATA_KEY_SELF_PRODUCTNO = 181;
        //    public const int DF_DATA_KEY_U_DESIGN = 185;

        //    public const int DF_DATA_SCROLLMSG_V03 = 203;
        //    public const int DF_DATA_SALEMSG = 15;
        //    public const int DF_DATA_STORE = 26;
        //    public const int DF_DATA_SECURITY = 29;
        //    public const int DF_DATA_DISC_V02 = 216;
        //    public const int DF_DATA_NUTRIFACT_V02 = 201;
        //    public const int DF_DATA_REPORT_PLU = 45;
        //    public const int DF_DATA_REPORT_SCALE_V02 = 215;
        //    public const int DF_DATA_REPORT_CLERK_V02 = 211;
        //    public const int DF_DATA_REPORT_DEPT_V02 = 212;
        //    public const int DF_DATA_REPORT_GROUP_V02 = 213;
        //    public const int DF_DATA_REPORT_HOURLY_V02 = 214;
        //    public const int DF_DATA_REPORT_TAX = 49;
        //    public const int DF_DATA_REPORT = 30;
        //    public const int DF_DATA_INGREDIENT = 20;
        //    public const int DF_DATA_SLAUGHTERHOUSE = 87;
        //    public const int DF_DATA_LIVESTOCK_V06 = 186;
        //    public const int DF_DATA_MEATGRADE = 108;
        //    public const int DF_DATA_ORIGIN = 16;

        //    public const int DF_STATE_CONNECT = 1;
        //    public const int DF_STATE_DISCONNECT = 2;
        //    public const int DF_STATE_RETRYOVER = 8;
        //    public const int DF_STATE_CONNERROR = 20;
        //    public const int DF_STATE_SENDFAIL = 21;
        //    public const int DF_STATE_RECEIVETIMEOVER = 22;
        //    public const int DF_STATE_ALREADYCONNECTED = 23;

        //    public const int DF_STATE_CONNECT_FTP = 31;
        //    public const int DF_STATE_DISCONNECT_FTP = 32;
        //    public const int DF_STATE_RETRYOVER_FTP = 33;
        //    public const int DF_STATE_CONNERROR_FTP = 34;
        //    public const int DF_STATE_SENDFAIL_FTP = 35;
        //    public const int DF_STATE_RECEIVETIMEOVER_FTP = 36;
        //    public const int DF_STATE_ALREADYCONNECTED_FTP = 37;

        //    public const int DF_ERROR_ENDOFDATA = 99;
        //    public const int DF_ERROR_DELETEING = 16;

        //    public const int DF_RECV_SUCCESS = 1001;
        //    public const int DF_RECV_FAIL = 1002;
        //    public const int DF_RECV_SUCCESS_FTP = 1011;
        //    public const int DF_RECV_FAIL_FTP = 1012;

        //    public const int DF_SCALE_CL5000 = 5000;
        //    public const int DF_SCALE_CL5000JR = 5010;
        //    public const int DF_SCALE_CL5500 = 5500;
        //    public const int DF_SCALE_CL5200 = 5200;
        //    public const int DF_SCALE_CL7200 = 7200;
        //    public const int DF_SCALE_CL7210 = 7210; //For Korea Emart

        //    public const int DF_KEY_FUNCTION_SPEEDKEY = 65535;

        //    public const int DF_KEY_STEP_PLU = 1;
        //    public const int DF_KEY_STEP_FUNCTION = 2;
        //    public const int DF_KEY_STEP_PRODUCT = 3;
        //    public const int DF_KEY_STEP_DESIGN = 4;

        //    public const int DF_SELFKEY_TYPE_7_4 = 1;
        //    public const int DF_SELFKEY_TYPE_8_5 = 2;
        //    public const int DF_SELFKEY_TYPE_9_6 = 3;
        //}

        //public class CReturn
        //{
        //    public const int NOTHING = 0;
        //    public const int SUCCESS = 1;
        //}

        //public class CCountry
        //{
        //    public const int DF_SCALE_COUNTRY_KOREA = 1;
        //    public const int DF_SCALE_COUNTRY_USA = 2;
        //    public const int DF_SCALE_COUNTRY_EUR = 3;
        //    public const int DF_SCALE_COUNTRY_RUSSIA = 5;
        //}

        //public class CKeyData
        //{
        //    public int nFunctionNormal;
        //    public int nFunctionShift;
        //    public int nPluNoNormal;
        //    public int nPluNoShift;
        //}

        //public async Task SendPLUAsync(string ipAddress, List<BarcodeScaleItemModel> items)
        //{
        //    _ipAddress = ipAddress;
        //    _version = "";
        //    _sentCount.Clear();

        //    // Index тохируулах
        //    int index = 1;
        //    items.ForEach(a => a.Index = index++);
        //    _items = items;
        //    _sendLimit = items.Count;

        //    // Ingredients боловсруулах
        //    _ingredients = new();
        //    var ingredientNotes = items
        //        .Where(a => !string.IsNullOrEmpty(a.BarCodeNote))
        //        .Select(a => a.BarCodeNote)
        //        .Distinct()
        //        .ToList();

        //    for (int i = 0; i < ingredientNotes.Count; i++)
        //    {
        //        items.Where(a => a.BarCodeNote == ingredientNotes[i])
        //             .ToList()
        //             .ForEach(a => a.IngredientNo = i + 1);

        //        _ingredients.Add(new BarcodeScaleItemModel
        //        {
        //            BarCodeNote = ingredientNotes[i],
        //            IngredientNo = i + 1
        //        });
        //    }

        //    _tcs = new TaskCompletionSource<bool>();

        //    // Event handler бүртгэх
        //    _scale.RecvEventString += OnRecvEvent;
        //    _scale.StateEvent += OnStateEvent;

        //    // Version авах
        //    ConnectScale(_ipAddress, -1, CCommon.DF_SCALE_CL5200, "");
        //    _scale.SendDataString(_ipAddress, -1, CCommon.DF_SCALE_CL5200, "",
        //        CCommon.DF_ACTION_UPLOAD, CCommon.DF_DATA_VERSION_TYPE2, "");

        //    // Дуусахыг хүлээх (timeout 60 секунд)
        //    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        //    cts.Token.Register(() => _tcs.TrySetException(new TimeoutException("Scale timeout")));

        //    await _tcs.Task;

        //    // Event handler цэвэрлэх
        //    _scale.RecvEventString -= OnRecvEvent;
        //    _scale.StateEvent -= OnStateEvent;
        //}

        //// ─── Event handlers ──────────────────────────────────────────────────────

        //private void OnRecvEvent(object sender,
        //    _DCasScaleEvents_RecvEventStringEvent e)
        //{
        //    switch (e.iTransType)
        //    {
        //        case CCommon.DF_ACTION_UPLOAD:
        //            HandleUpload(e);
        //            break;
        //        case CCommon.DF_ACTION_DOWNLOAD:
        //            HandleDownload(e);
        //            break;
        //    }
        //}

        //private void HandleUpload(_DCasScaleEvents_RecvEventStringEvent e)
        //{
        //    switch (e.iDataType)
        //    {
        //        // Version авсан — PLU илгээж эхлэнэ
        //        case CCommon.DF_DATA_VERSION_TYPE2:
        //            if (e.iResult == CCommon.DF_RECV_SUCCESS)
        //            {
        //                _version = e.sData;
        //                DisconnectScale(e.sIP, e.shScaleID);

        //                // PLU илгээх эхлэл
        //                _sentCount[e.sIP] = 0;
        //                SendNextPLU(e.sIP, e.iScaleModel, 1);
        //            }
        //            else
        //                _tcs?.TrySetException(new Exception("Version авахад алдаа гарлаа"));
        //            break;
        //    }
        //}

        //private void HandleDownload(_DCasScaleEvents_RecvEventStringEvent e)
        //{
        //    switch (e.iDataType)
        //    {
        //        case CCommon.DF_DATA_PLU_V06:
        //            if (e.iResult == CCommon.DF_RECV_SUCCESS)
        //            {
        //                int sentCount = _sentCount[e.sIP];

        //                if (sentCount >= _sendLimit)
        //                {
        //                    // PLU бүгд илгээгдсэн
        //                    DisconnectScale(e.sIP, e.shScaleID);

        //                    // Ingredient байвал илгээх
        //                    if (_ingredients.Count > 0)
        //                        StartSendIngredient(e.sIP, e.iScaleModel);
        //                    else
        //                        _tcs?.TrySetResult(true);
        //                }
        //                else
        //                {
        //                    // Дараагийн PLU илгээх
        //                    SendNextPLU(e.sIP, e.iScaleModel, sentCount + 1);
        //                }
        //            }
        //            else
        //            {
        //                DisconnectScale(e.sIP, e.shScaleID);
        //                _tcs?.TrySetException(new Exception($"PLU илгээхэд алдаа: {e.sIP}"));
        //            }
        //            break;

        //        case CCommon.DF_DATA_INGREDIENT:
        //            if (e.iResult == CCommon.DF_RECV_SUCCESS)
        //            {
        //                int sentCount = _sentCount.GetValueOrDefault(e.sIP + "_ing", 0);

        //                if (sentCount >= _ingredients.Count)
        //                {
        //                    DisconnectScale(e.sIP, e.shScaleID);
        //                    _tcs?.TrySetResult(true);
        //                }
        //                else
        //                {
        //                    SendNextIngredient(e.sIP, e.iScaleModel, sentCount + 1);
        //                }
        //            }
        //            else
        //            {
        //                DisconnectScale(e.sIP, e.shScaleID);
        //                _tcs?.TrySetException(new Exception($"Ingredient илгээхэд алдаа: {e.sIP}"));
        //            }
        //            break;
        //    }
        //}

        //private void OnStateEvent(object sender,
        //    _DCasScaleEvents_StateEventEvent e)
        //{
        //    switch (e.iState)
        //    {
        //        case CCommon.DF_STATE_CONNERROR:
        //        case CCommon.DF_STATE_SENDFAIL:
        //        case CCommon.DF_STATE_RECEIVETIMEOVER:
        //        case CCommon.DF_STATE_RETRYOVER:
        //            _tcs?.TrySetException(new Exception($"Холболтын алдаа: {e.sIP}"));
        //            break;
        //    }
        //}

        //// ─── Туслах методууд ──────────────────────────────────────────────────────

        //private void SendNextPLU(string sIP, int nModel, int pluIndex)
        //{
        //    string data = MakePLUData_V06(pluIndex);
        //    if (string.IsNullOrEmpty(data)) return;

        //    ConnectScale(sIP, -1, nModel, _version);
        //    _scale.SendDataString(sIP, -1, nModel, _version,
        //        CCommon.DF_ACTION_DOWNLOAD, CCommon.DF_DATA_PLU_V06, data);

        //    _sentCount[sIP] = pluIndex;
        //}

        //private void StartSendIngredient(string sIP, int nModel)
        //{
        //    _sentCount[sIP + "_ing"] = 0;
        //    SendNextIngredient(sIP, nModel, 1);
        //}

        //private void SendNextIngredient(string sIP, int nModel, int ingIndex)
        //{
        //    string data = MakeIngredientData(ingIndex);
        //    if (string.IsNullOrEmpty(data)) return;

        //    ConnectScale(sIP, -1, nModel, _version);
        //    _scale.SendDataString(sIP, -1, nModel, _version,
        //        CCommon.DF_ACTION_DOWNLOAD, CCommon.DF_DATA_INGREDIENT, data);

        //    _sentCount[sIP + "_ing"] = ingIndex;
        //}

        //private int ConnectScale(string sIP, short nID, int nModel, string strVersion)
        //    => _scale.ConnectionEx3(sIP, 20304, nID, nModel, strVersion, 97);

        //private void DisconnectScale(string sIP, short nID)
        //    => _scale.DisconnectOneEx(sIP, nID);

        //// ─── PLU өгөгдөл үүсгэх (GASUtilsForm.MakePLUSampleData_V06-аас) ────────

        //private string MakePLUData_V06(int iPluNo)
        //{
        //    var item = _items.FirstOrDefault(a => a.Index == iPluNo);
        //    if (item == null) return string.Empty;

        //    var isUnit = item.IsFraction != "N";
        //    var sb = new StringBuilder();

        //    sb.Append("0001");                                                    // Department
        //    sb.Append((item.PLU + "").PadLeft(6, '0'));                          // PLU No
        //    sb.Append(isUnit ? "01" : "03");                                     // PLU type
        //    sb.Append("01");                                                      // Unit Weight
        //    sb.Append($"{item.NewPrice:0000000000}");                            // Unit Price
        //    sb.Append("0001");                                                    // Group No
        //    sb.Append((item.Barcode + "").PadLeft(13, '0'));                     // ItemCode
        //    sb.Append("0000");                                                    // Tare No
        //    sb.Append("0000000000");                                              // Tare
        //    sb.Append("000001");                                                  // Pieces
        //    sb.Append("01");                                                      // QuantitySymbol
        //    sb.Append("000000");                                                  // Sell by date
        //    sb.Append("000000");                                                  // Sell by time
        //    sb.Append("000000");                                                  // Pack date
        //    sb.Append("000000");                                                  // Pack time
        //    sb.Append("000000");                                                  // Produce date
        //    sb.Append((item.IngredientNo + "").PadLeft(6, '0'));                 // Ingredient No
        //    sb.Append(isUnit ? "01" : "00");                                     // Fixed price
        //    sb.Append("000000");                                                  // Traceability
        //    sb.Append("000000");                                                  // Origin
        //    sb.Append("0000");                                                    // Nutrifact
        //    sb.Append("0004");                                                    // Label No
        //    sb.Append("0020");                                                    // Aux Label
        //    sb.Append("0001");                                                    // Barcode1
        //    sb.Append("0002");                                                    // Barcode2
        //    sb.Append("0000");                                                    // Sale Message
        //    sb.Append("0000000900");                                              // Special Price
        //    sb.Append("0000000000");                                              // Fixed Weight
        //    sb.Append("05");                                                      // Picture
        //    sb.Append("01");                                                      // Packdate flag
        //    sb.Append("01");                                                      // Packtime flag
        //    sb.Append("01");                                                      // Sellbydate flag
        //    sb.Append("01");                                                      // Sellbytime flag
        //    sb.Append(PadEncoding("", 8));                                        // Reserve
        //    sb.Append("01010101010101 01");                                       // Font sizes x8
        //    sb.Append(PadEncoding(Conversion.ConvertStringToLatin(item.ItemName, 13), 55)); // Name1
        //    sb.Append(PadEncoding(item.NewPrice.ToString("##,##0") + "₮", 55));  // Name2
        //    sb.Append(PadEncoding("", 55));                                       // Name3~8
        //    sb.Append(PadEncoding("", 55));
        //    sb.Append(PadEncoding("", 55));
        //    sb.Append(PadEncoding("", 55));
        //    sb.Append(PadEncoding("", 55));
        //    sb.Append(PadEncoding("", 55));
        //    sb.Append("01");                                                      // Korea Traceability
        //    sb.Append(PadEncoding("", 4096));                                     // Direct Ingredient
        //    sb.Append(PadEncoding("", 50));                                       // Ext Barcode
        //    sb.Append(PadEncoding("", 10));                                       // Prefix
        //    sb.Append("01");                                                      // Tax No
        //    sb.Append("00002");                                                   // % Tare
        //    sb.Append("00010");                                                   // Tare % limit
        //    sb.Append("000010");                                                  // Cook by date
        //    sb.Append("000020");                                                  // Bonus
        //    sb.Append("0002");                                                    // Ref Dept
        //    sb.Append("000003");                                                  // Ref PLU
        //    sb.Append("0004");                                                    // Coupled Dept
        //    sb.Append("000005");                                                  // Coupled PLU
        //    sb.Append("02");                                                      // Link PLU Count
        //    sb.Append("0006");                                                    // Link Dept1
        //    sb.Append("000007");                                                  // Link PLU1
        //    sb.Append("0008");                                                    // Link Dept2
        //    sb.Append("000009");                                                  // Link PLU2
        //    sb.Append(PadEncoding($"test{iPluNo}.jpg", 50));                     // Image name

        //    return sb.ToString();
        //}

        //private string MakeIngredientData(int nIngredientNo)
        //{
        //    var ingredient = _ingredients.FirstOrDefault(a => a.IngredientNo == nIngredientNo);
        //    if (ingredient == null) return string.Empty;

        //    var sb = new StringBuilder();
        //    sb.Append($"{nIngredientNo:000000}");
        //    sb.Append(PadEncoding(ingredient.BarCodeNote ?? "", 1300));
        //    return sb.ToString();
        //}

        //private static string PadEncoding(string data, int totalLength)
        //{
        //    var bytes = Encoding.ASCII.GetByteCount(data);
        //    return data + new string(' ', Math.Max(0, totalLength - bytes));
        //}

        #endregion

        #region ACLAS LS 2

        private const string LibraryName = "AclasSDK.dll";

        private const int ASSDK_Err_Success = 0x0000;
        private const int ASSDK_Err_Progress = 0x0001;
        private const int ASSDK_Err_Terminate = 0x0002;

        private const int ASSDK_ProtocolType_None = 0;
        private const int ASSDK_ProcType_Down = 0;
        private const int ASSDK_DataType_PLU = 0x0000;

        [StructLayout(LayoutKind.Sequential)]
        private struct TASSDKDeviceInfo
        {
            public uint ProtocolType;
            public uint Addr;
            public uint Port;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Name;
            public uint ID;
            public uint Version;
            public byte Country;
            public byte DepartmentID;
            public byte KeyType;
            public ulong PrinterDot;
            public long PrnStartDate;
            public uint LabelPage;
            public uint PrinterNo;
            public ushort PLUStorage;
            public ushort HotKeyCount;
            public ushort NutritionStorage;
            public ushort DiscountStorage;
            public ushort Note1Storage;
            public ushort Note2Storage;
            public ushort Note3Storage;
            public ushort Note4Storage;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 177)]
            public byte[] Adjunct;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void TASSDKOnProgressEvent(uint nErrorCode, uint Index, uint Total, IntPtr lpUserData);

        [DllImport(LibraryName, CharSet = CharSet.Ansi)]
        private static extern bool AclasSDK_GetDevicesInfo(uint Addr, uint Port, uint ProtocolType, ref TASSDKDeviceInfo DeviceInfo);

        [DllImport(LibraryName)]
        private static extern bool AclasSDK_Initialize(IntPtr Adjunct = default);

        [DllImport(LibraryName)]
        private static extern void AclasSDK_Finalize();

        [DllImport(LibraryName)]
        private static extern IntPtr AclasSDK_ExecTaskA(uint Addr, uint Port, uint ProtocolType, uint ProcType, uint DataType,
            string FileName, TASSDKOnProgressEvent OnProgress, IntPtr lpUserData);

        [DllImport(LibraryName)]
        private static extern void AclasSDK_WaitForTask(IntPtr TaskHandle);

        private static async Task SendAclasLS2<T>(string ipAddress, List<T> items,
            bool isOld = false, bool isLatin = false)
            where T : IBarcodeScaleItem
        {
            var pluSb = new StringBuilder();
            pluSb.AppendLine(
                "ID\tItemCode\tDepartmentID\tGroupID\tName1\tName2\tName3\tPrice\t" +
                "UnitID\tBarcodeType1\tBarcodeType2\tLabel1ID\tLabel2ID\tProducedDate\t" +
                "FreshnessDate\tValidDate\tPackageType\tPackageWeight\tPackagePrice\t" +
                "PackageRange\tPackageDays\tPackageHours\tText1ID\tText2ID\tText3ID\t" +
                "Text4ID\tText5ID\tText6ID\tText7ID\tText8ID\tDiscountID\tDiscountRate\t" +
                "HalfDiscount\tQuarterDiscount\tTareID\tTareValue\tIceID\tICEValue\t" +
                "OriginID\tTraceabilityID\tLimitPrice\tTax1\tTax2\tTax3\tTax4\t" +
                "Flag1\tFlag2\tFlag3\tProducedDateRule\tFreshnessDateFrom\tValidDateFrom\t" +
                "PackageDateFrom\tSpeedCode\tPosition1\tSalesCategory\tDiscountBeginTime\t" +
                "DiscountEndTime\tDiscountPrice\tDiscountFlag\tMessage1\tUnitPrintName\t");

            var hotKeySb = new StringBuilder();
            hotKeySb.AppendLine("ButtonIndex\tButtonValue");

            foreach (var item in items)
            {
                string itemName = string.IsNullOrEmpty(item.PrintName) ? item.ItemName : item.PrintName;
                if (isLatin)
                    itemName = Conversion.ConvertStringToLatin(itemName, itemName.Length);

                pluSb.AppendLine(string.Format(
                    isOld ? itemAclasLS2Old : itemAclasLS2,
                    item.PLU,
                    item.Barcode,
                    itemName,
                    item.NewPrice.ToString("##.##"),
                    item.ValidDay,
                    item.IsFraction == "Y" ? "4" : "10",
                    ScaleBarCodeType));

                hotKeySb.AppendLine($"{item.HotKey}\t{item.PLU}");
            }

            string tempDir = Path.GetTempPath();
            string pluPath = Path.Combine(tempDir, "pluItems.txt");
            string hotKeyPath = Path.Combine(tempDir, "HotKey.txt");

            await File.WriteAllTextAsync(pluPath, pluSb.ToString());
            await File.WriteAllTextAsync(hotKeyPath, hotKeySb.ToString());

            await Task.Run(() =>
            {
                UploadAclasData(ipAddress, pluPath);
                UploadAclasData(ipAddress, hotKeyPath);
            });
        }

        private static void UploadAclasData(string ipAddress, string filePath)
        {
            AclasSDK_Initialize();
            uint iAddr = MakeHostToDWord(ipAddress);

            var deviceInfo = new TASSDKDeviceInfo();
            if (AclasSDK_GetDevicesInfo(iAddr, 0, ASSDK_ProtocolType_None, ref deviceInfo))
            {
                TASSDKOnProgressEvent onProgress = OnAclasProgress;
                var handle = AclasSDK_ExecTaskA(
                    deviceInfo.Addr, deviceInfo.Port, deviceInfo.ProtocolType,
                    ASSDK_ProcType_Down, ASSDK_DataType_PLU,
                    filePath, onProgress, IntPtr.Zero);
                AclasSDK_WaitForTask(handle);
            }

            AclasSDK_Finalize();
        }

        private static uint MakeHostToDWord(string sHost)
        {
            var segments = sHost.Split('.');
            if (segments.Length != 4) return 0;

            uint result = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                uint seg = Convert.ToUInt32(segments[i]);
                if (seg > 255) return 0;
                result += seg << ((3 - i) * 8);
            }
            return result;
        }

        private static void OnAclasProgress(uint nErrorCode, uint Index, uint Total, IntPtr lpUserData)
        {
            switch (nErrorCode)
            {
                case ASSDK_Err_Success:
                case ASSDK_Err_Progress:
                    break;
                case ASSDK_Err_Terminate:
                    throw new Exception("Aclas LS-2: илгээлт зогссон.");
                default:
                    throw new Exception($"Aclas LS-2: илгээхэд алдаа гарлаа (код: {nErrorCode}).");
            }
        }

        private static string GetPLUData<T>(string type, T item)
            where T : IBarcodeScaleItem
        {
            string plu = $"0000{item.PLU}"[^4..];
            string barCode = $"0000000{item.Barcode}"[^7..];

            return type switch
            {
                "TM-xA" => BuildTMxA(plu, barCode, item),
                "TM-15A" => BuildTM15A(plu, barCode, item),
                "TM-30A" => BuildTM30A(plu, barCode, item),
                "TM-30Ab" => BuildTM30Ab(plu, barCode, item),
                _ => throw new Exception($"{type}: дэмжигдэхгүй жингийн төрөл.")
            };
        }

        private static string BuildTMxA<T>(string plu, string barCode, T item) where T : IBarcodeScaleItem
        {
            string price = $"000000{item.NewPrice:#0}"[^6..];
            return $"\r\nPLU\t{plu}\t{barCode}\t{barCode}\t1\t{price}\t0,0\t0,0\t1\t10\t0\t1\t1\t0\t9\t{item.PrintName}\t\t\t\t\t\t\t\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0,0\t0,0\t0\t0\t0,0\t0,0\t0,0\t0\t0\t0,0\t0,0\t0,0\t0\t0\t0,0\t0,0\t0,0\t0\t0\t0\t0\t0\t0\t";
        }

        private static string BuildTM15A<T>(string plu, string barCode, T item) where T : IBarcodeScaleItem
        {
            string price = $"00000{item.NewPrice:#0}"[^5..];
            string isFraction = item.IsFraction == "Y" ? "0" : "1";
            string itemName = Conversion.ConvertTextToHexString(item.PrintName);
            return $"!0W{plu}A{barCode}B{price}C{isFraction}D000E00f00000G0H{itemName}\r\n";
        }

        private static string BuildTM30A<T>(string plu, string barCode, T item) where T : IBarcodeScaleItem
        {
            string price = $"000000{item.NewPrice:#0}"[^6..];
            string isFraction = item.IsFraction == "Y" ? "0" : "1";
            string itemName = Conversion.ConvertTextToHexString(item.PrintName);
            return $"!0V{plu}A{barCode}{price}{isFraction}{"0".PadRight(57, '0')}B{itemName}CDE\r\n";
        }

        private static string BuildTM30Ab<T>(string plu, string barCode, T item) where T : IBarcodeScaleItem
        {
            string price = $"000000{item.NewPrice:#0}"[^6..];
            string isFraction = item.IsFraction == "Y" ? "0" : "1";
            string itemName = Conversion.ConvertTextToHexString(
                                    Conversion.ConvertStringToLatin(item.PrintName, 30));
            return $"!0V{plu}A{barCode}{price}{isFraction}{"0".PadRight(57, '0')}B{itemName}CDE\r\n";
        }

        #endregion

        #region Other Scales

        private static async Task SendOther<T>(string type, string ipAddress, int port, List<T> items)
            where T : IBarcodeScaleItem
        {
            var pluItems = items.Select(item => GetPLUData(type, item)).ToArray();
            await SendSocketAsync(type, ipAddress, port, pluItems);
        }

        private static async Task SendSocketAsync(string type, string ipAddress, int port, string[] data)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ipAddress), port));

            if (type == "TM-xA")
            {
                await socket.SendAsync(Encoding.UTF8.GetBytes("DWL\tPLU\t"));
                foreach (var d in data)
                    await socket.SendAsync(Encoding.UTF8.GetBytes(d));
                await Task.Delay(100);
                await socket.SendAsync(Encoding.UTF8.GetBytes("DWL\tEND\t"));
            }
            else if (type is "TM-15A" or "TM-30A" or "TM-30Ab")
            {
                await Task.Delay(200);
                foreach (var d in data)
                {
                    var bytes = Encoding.UTF8.GetBytes(d);
                    await socket.SendAsync(bytes);
                    if (type is "TM-30A" or "TM-30Ab")
                        await socket.ReceiveAsync(new byte[256]);
                    await Task.Delay(300);
                }
                await Task.Delay(200);
            }

            socket.Disconnect(false);
        }

        #endregion
    }
}
