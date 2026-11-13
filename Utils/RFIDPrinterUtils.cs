using RestSharp;
using SmartSoftware.DeviceAgent.Model;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SmartSoftware.DeviceAgent.Utils
{
    public static class RFIDPrinterUtils
    {
        public static async Task<RFIDResponse> SendCP30(string printerModel, string rfidId, byte[] imageBytes)
        {
            var tempDir = Path.GetTempPath();
            var filePath = Path.Combine(tempDir, rfidId + ".bmp");
            await File.WriteAllBytesAsync(filePath, imageBytes);

            try
            {
                var printerConfig = new RFIDPrinterConfig
                {
                    Model = printerModel,
                    PrinterID = "ZPL",
                    Interface = "USB",
                    Printers = new List<RFIDPrinter>
                    {
                        new RFIDPrinter
                        {
                            Items = new List<RFIDItem>
                            {
                                new RFIDItem
                                {
                                    ItemType = "ZPL_RfidWrite",
                                    Format = "H",
                                    Begin = 2,
                                    Size = 4,
                                    MemoryBlock = "1",
                                    Text = rfidId
                                },
                                new RFIDItem { ItemType = "ZPL_StartFormat" },
                                new RFIDItem
                                {
                                    ItemType = "ZPL_PrintImage",
                                    XPos = 0,
                                    YPos = 0,
                                    ImgName = "file:///" + filePath.Replace("\\", "/")
                                },
                                new RFIDItem { ItemType = "ZPL_EndFormat" }
                            }
                        }
                    }
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var client = new ClientWebSocket();
                await client.ConnectAsync(new Uri("ws://127.0.0.1:9099"), cts.Token);

                var message = JsonSerializer.Serialize(printerConfig);
                var messageBuffer = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, cts.Token);

                var buffer = new byte[1024];
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                var response = Encoding.UTF8.GetString(buffer, 0, result.Count);

                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cts.Token);

                return JsonSerializer.Deserialize<RFIDResponse>(response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new Exception("RFID CP30: принтерийн хариуг задлахад алдаа гарлаа.");
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        public static async Task<RFIDResponse> SendZT411(string rfidId, byte[] imageBytes)
        {
            using var client = new RestClient("http://127.0.0.1:9100");

            var checkRequest = new RestRequest("default", Method.Get);
            checkRequest.AddQueryParameter("type", "printer");
            var checkResponse = await client.ExecuteAsync(checkRequest);
            if (!checkResponse.IsSuccessful)
                throw new Exception($"RFID ZT411: принтер шалгахад алдаа гарлаа. {checkResponse.Content}");

            var checkModel = JsonSerializer.Deserialize<RFIDZebraPrinterModel>(checkResponse.Content!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("RFID ZT411: принтер шалгах хариуг задлахад алдаа гарлаа.");

            var tempDir = Path.GetTempPath();
            var filePath = Path.Combine(tempDir, rfidId + ".bmp");
            var monoFilePath = Path.Combine(tempDir, rfidId + "_mono.bmp");
            await File.WriteAllBytesAsync(filePath, imageBytes);

            try
            {
                using (var imageToConvert = new Bitmap(filePath))
                {
                    var rectangle = new Rectangle(0, 0, imageToConvert.Width, imageToConvert.Height);
                    using var monochromeImage = imageToConvert.Clone(rectangle, PixelFormat.Format1bppIndexed);
                    monochromeImage.RotateFlip(RotateFlipType.Rotate180FlipX);
                    monochromeImage.Save(monoFilePath, ImageFormat.Bmp);
                }

                var imageZpl = ConvertImage(monoFilePath, rfidId);

                var payload = new
                {
                    device = new
                    {
                        name = checkModel.Name,
                        uid = checkModel.Uid,
                        connection = checkModel.Connection,
                        deviceType = checkModel.DeviceType,
                        version = checkModel.Version,
                        provider = checkModel.Provider,
                        manufacturer = checkModel.Manufacturer
                    },
                    data = imageZpl
                };

                var payloadJson = JsonSerializer.Serialize(payload);

                var writeRequest = new RestRequest("write", Method.Post);
                writeRequest.AddHeader("Origin", "localhost");
                writeRequest.AddHeader("Accept", "*/*");
                writeRequest.AddStringBody(payloadJson, "text/plain");

                var postResponse = await client.ExecuteAsync(writeRequest);

                if (!postResponse.IsSuccessful)
                    throw new Exception($"RFID ZT411: хэвлэхэд алдаа гарлаа. {postResponse.Content}");

                return JsonSerializer.Deserialize<RFIDResponse>(postResponse.Content!,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new Exception("RFID ZT411: принтерийн хариуг задлахад алдаа гарлаа.");
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                if (File.Exists(monoFilePath))
                    File.Delete(monoFilePath);
            }
        }

        public static string ConvertImage(string bitmapFilePath, string rfidId)
        {
            int w, h;
            using (var b = new Bitmap(bitmapFilePath))
            {
                w = b.Width;
                h = b.Height;
            }

            byte[] bitmapFileData = File.ReadAllBytes(bitmapFilePath);

            int bitmapDataOffset = bitmapFileData[10];
            int width = w;
            int bitmapDataLength = bitmapFileData.Length - bitmapDataOffset;
            double widthInBytes = Math.Ceiling(width / 8.0);

            while (widthInBytes % 4 != 0)
                widthInBytes++;

            byte[] bitmap = new byte[bitmapDataLength];
            Buffer.BlockCopy(bitmapFileData, bitmapDataOffset, bitmap, 0, bitmapDataLength);

            for (int i = 0; i < bitmapDataLength; i++)
                bitmap[i] ^= 0xFF;

            string zplImageDataString = BitConverter.ToString(bitmap).Replace("-", string.Empty);
            string zplImageDataStringWithNewLine = SpliceText(zplImageDataString, 1023);

            string zplCommand = "^XA";
            zplCommand += "^FO20,20";
            zplCommand +=
                "^GFA," +
                bitmapDataLength + "," +
                bitmapDataLength + "," +
                widthInBytes + "," +
                Environment.NewLine +
                zplImageDataStringWithNewLine;

            zplCommand += "^RFW,H,1,2,1^FD3000^FS";
            zplCommand += "^RFW,H,2,12,1^FD" + rfidId + "^FS";
            zplCommand += "^XZ";

            return zplCommand;
        }

        public static string SpliceText(string text, int lineLength)
        {
            return System.Text.RegularExpressions.Regex.Replace(text, "(.{" + lineLength + "})", "$1" + Environment.NewLine);
        }
    }
}
