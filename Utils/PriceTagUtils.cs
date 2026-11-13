using SmartSoftware.DeviceAgent.Model;
using System.Text;
using System.Text.Json;

namespace SmartSoftware.DeviceAgent.Utils
{
    public static class PriceTagUtils
    {
        public static async Task<PriceTagResponseTokenModel> SendTags(PriceTagSendRequest req)
        {
            var tokenResp = await GetToken(req.PriceTagURL, req.Username, req.Password);
            if (tokenResp.Code != "0")
                throw new Exception(tokenResp.Message ?? "PriceTag: нэвтрэхэд алдаа гарлаа.");

            var url = $"{req.PriceTagURL}/api/esl/tag/save";
            var model = new { shopCode = req.StoreId, tagIdList = req.TagIdList };

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResp.Body);

            using var response = await client.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PriceTag: илгээхэд алдаа гарлаа. {body}");

            var result = JsonSerializer.Deserialize<PriceTagResponseTokenModel>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("PriceTag: хариуг задлахад алдаа гарлаа.");

            if (result.Code != "0")
                throw new Exception(result.Message ?? "PriceTag: илгээхэд алдаа гарлаа.");

            return result;
        }

        public static async Task<List<PriceTagSendItemResult>> SendItems(PriceTagSendItemsRequest req)
        {
            var results = new List<PriceTagSendItemResult>();

            var tokenResp = await GetToken(req.PriceTagURL, req.Username, req.Password);
            if (tokenResp.Code != "0")
                throw new Exception(tokenResp.Message ?? "PriceTag: нэвтрэхэд алдаа гарлаа.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResp.Body);

            foreach (var item in req.Items)
            {
                var items = new List<object>
                {
                    item.RetailId, item.ItemId, item.ItemName, item.Barcode,
                    string.Empty, string.Empty, item.BasePrice, item.SalePrice, item.DiscountAmount
                };
                for (int i = 1; i <= 9; i++) items.Add(string.Empty);
                items.Add(item.Note);
                for (int i = 1; i <= 9; i++) items.Add(string.Empty);

                var saveUrl = $"{req.PriceTagURL}/api/goods/saveList?NR=false";
                var saveModel = new List<object> { new { shopCode = req.StoreId, template = "SAL", items } };

                using var saveContent = new StringContent(JsonSerializer.Serialize(saveModel), Encoding.UTF8, "application/json");
                using var saveResponse = await client.PostAsync(saveUrl, saveContent);
                var saveBody = await saveResponse.Content.ReadAsStringAsync();

                PriceTagResponseTokenModel? saveResult = null;
                if (saveResponse.IsSuccessStatusCode)
                    saveResult = JsonSerializer.Deserialize<PriceTagResponseTokenModel>(saveBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (saveResult == null || saveResult.Code != "0")
                {
                    var msg = saveResult?.Message ?? saveBody;
                    foreach (var tagId in item.PriceTagIds)
                        results.Add(new PriceTagSendItemResult { PriceTagId = tagId, ItemId = item.ItemId, Success = false, Message = msg });
                    continue;
                }

                foreach (var tagId in item.PriceTagIds)
                {
                    var bindUrl = $"{req.PriceTagURL}/api/esl/tag/bind";
                    var bindModel = new
                    {
                        shopCode = req.StoreId,
                        ap = "",
                        binds = new List<object> { new { tagID = tagId, goodsCode = item.RetailId + item.ItemId } }
                    };

                    using var bindContent = new StringContent(JsonSerializer.Serialize(bindModel), Encoding.UTF8, "application/json");
                    using var bindResponse = await client.PostAsync(bindUrl, bindContent);
                    var bindBody = await bindResponse.Content.ReadAsStringAsync();

                    PriceTagResponseTokenModel? bindResult = null;
                    if (bindResponse.IsSuccessStatusCode)
                        bindResult = JsonSerializer.Deserialize<PriceTagResponseTokenModel>(bindBody,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (bindResult != null && bindResult.Code == "0")
                        results.Add(new PriceTagSendItemResult { PriceTagId = tagId, ItemId = item.ItemId, Success = true, Message = "Success" });
                    else
                        results.Add(new PriceTagSendItemResult { PriceTagId = tagId, ItemId = item.ItemId, Success = false, Message = bindResult?.Message ?? bindBody });
                }
            }

            return results;
        }

        public static async Task<PriceTagResponseTokenModel> GetToken(string priceTagUrl, string username, string password)
        {
            var url = $"{priceTagUrl}/api/login";
            var model = new PriceTagAuthToken { UserName = username, Password = password };

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"PriceTag: нэвтрэхэд алдаа гарлаа. {body}");

            return JsonSerializer.Deserialize<PriceTagResponseTokenModel>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("PriceTag: нэвтрэх хариуг задлахад алдаа гарлаа.");
        }
    }
}
