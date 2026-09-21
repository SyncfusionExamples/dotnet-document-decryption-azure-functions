using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Reflection;

namespace PdfDecryptionFunction;

public class UiFunction
{
    [Function("Ui")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "dotnet-document-decryption-azure-functions/{*relativePath}")]
        HttpRequestData req,
        string? relativePath = null)
    {
        var path = (relativePath ?? string.Empty).Trim('/');

        if (string.IsNullOrEmpty(path))
        {
            path = "index.html";
        }

        if (path.Contains("..", StringComparison.Ordinal) ||
            path.Contains('\\'))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        string ext = Path.GetExtension(path).ToLowerInvariant();

        var assembly = Assembly.GetExecutingAssembly();

        string resourceName =
            $"DocumentDecryptionFunction.wwwroot.{path.Replace("/", ".")}";

        using Stream? stream =
            assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            using Stream? fallback =
                assembly.GetManifestResourceStream(
                    "DocumentDecryptionFunction.wwwroot.index.html");

            if (fallback == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            return await WriteAsync(
                req,
                "text/html; charset=utf-8",
                fallback);
        }

        string contentType = ext switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".ico" => "image/x-icon",
            ".pdf" => "application/pdf",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            _ => "application/octet-stream"
        };

        return await WriteAsync(req, contentType, stream);
    }

    private static async Task<HttpResponseData> WriteAsync(
        HttpRequestData req,
        string contentType,
        Stream content)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        response.Headers.Add("Content-Type", contentType);
        response.Headers.Add("Cache-Control", "no-cache");

        await content.CopyToAsync(response.Body);

        return response;
    }
}