using EDeals.Api.Settings;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;

namespace EDeals.Api.GatewayServices
{
    public class GatewayService : IGatewayService
    {
        private readonly ILogger<GatewayService> _logger;
        private readonly HttpClient _httpClient;
        private readonly RestServiceSettings _restServiceSettings;

        public GatewayService(ILogger<GatewayService> logger, IHttpClientFactory httpClientFactory, IOptions<RestServiceSettings> restServiceSettings)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _restServiceSettings = restServiceSettings.Value;
        }

        public async Task ForwardRequest(HttpContext context, EDealsMicroserviceTypes type, CancellationToken cancellationToken)
        {
            var targetRequestMessage = CreateProxyRequest(context, type);

            using var responseMessage = await _httpClient.SendAsync(targetRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            context.Response.StatusCode = (int)responseMessage.StatusCode;
            CopyFromTargetResponseHeaders(context, responseMessage);
            await responseMessage.Content.CopyToAsync(context.Response.Body, cancellationToken);

            return;
        }

        private HttpRequestMessage CreateProxyRequest(HttpContext context, EDealsMicroserviceTypes serviceName)
        {
            var targetUri = BuildTargetUri(context.Request, serviceName);

            var targetRequestMessage = CreateTargetMessage(context, targetUri);

            return targetRequestMessage;
        }

        private void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            context.Response.Headers.Remove("transfer-encoding");
        }

        private HttpRequestMessage CreateTargetMessage(HttpContext context, Uri targetUri)
        {
            var requestMessage = new HttpRequestMessage();

            CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

            requestMessage.RequestUri = targetUri;
            requestMessage.Headers.Host = targetUri.Host;
            requestMessage.Method = GetMethod(context.Request.Method);

            return requestMessage;
        }

        private void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
        {
            try
            {
                var requestMethod = context.Request.Method;
                var contentType = context.Request.ContentType;

                if (!HttpMethods.IsGet(requestMethod) &&
                    !HttpMethods.IsHead(requestMethod) &&
                    !HttpMethods.IsDelete(requestMethod) &&
                    !HttpMethods.IsTrace(requestMethod))
                {
                    if (!string.IsNullOrEmpty(contentType) && contentType.StartsWith("multipart/form-data"))
                    {
                        var multiPartContent = new MultipartFormDataContent();

                        foreach (var file in context.Request.Form.Files)
                        {
                            var fileStreamContent = new StreamContent(file.OpenReadStream());
                            multiPartContent.Add(fileStreamContent, file.Name, file.FileName);
                        }

                        foreach (var field in context.Request.Form)
                        {
                            multiPartContent.Add(new StringContent(field.Value!), field.Key);
                        }


                        requestMessage.Content = multiPartContent;
                    }
                    else
                    {
                        var streamContent = new StreamContent(context.Request.Body);
                        requestMessage.Content = streamContent;
                    }                  

                }

                foreach (var header in context.Request.Headers)
                {
                    var success = requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());

                    if (!success)
                    {
                        requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
            } catch(Exception ex)
            {
                _logger.LogError("Something went wrong: {message}", ex.Message);
            }
        }

        private HttpMethod GetMethod(string method) =>
            method switch
            {
                string when HttpMethods.IsDelete(method) => HttpMethod.Delete,
                string when HttpMethods.IsGet(method) => HttpMethod.Get,
                string when HttpMethods.IsHead(method) => HttpMethod.Head,
                string when HttpMethods.IsOptions(method) => HttpMethod.Options,
                string when HttpMethods.IsPost(method) => HttpMethod.Post,
                string when HttpMethods.IsPut(method) => HttpMethod.Put,
                string when HttpMethods.IsTrace(method) => HttpMethod.Trace,
                string when HttpMethods.IsPatch(method) => HttpMethod.Patch,
                _ => new HttpMethod(method)
            };

        private Uri BuildTargetUri(HttpRequest request, EDealsMicroserviceTypes types)
        {
            var uriBuilder = new UriBuilder(request.GetEncodedUrl())
            {
                Scheme = _restServiceSettings.ApiProtocol
            };

            switch (types){
                case EDealsMicroserviceTypes.Core:
                    {
                        uriBuilder.Host = _restServiceSettings.CoreApiDomain.Split(':').FirstOrDefault();
                        _ = int.TryParse(_restServiceSettings.CoreApiDomain.Split(":").LastOrDefault(), out int parsedPort);
                        uriBuilder.Port = parsedPort;
                        break;
                    }
                case EDealsMicroserviceTypes.Catalog:
                    {
                        uriBuilder.Host = _restServiceSettings.CatalogApiDomain.Split(':').FirstOrDefault();
                        _ = int.TryParse(_restServiceSettings.CatalogApiDomain.Split(':').LastOrDefault(), out int parsedPort);
                        uriBuilder.Port = parsedPort;
                        break;
                    }
                default:
                    break;
            }

            if (uriBuilder.Port == 0)
            {
                if (uriBuilder.Scheme == "http")
                {
                    uriBuilder.Port = 80;
                }
                if (uriBuilder.Scheme == "https")
                {
                    uriBuilder.Port = 443;
                }
            }

            return uriBuilder.Uri;
        }
    }
}
