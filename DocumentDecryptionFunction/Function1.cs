using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Security;
using Syncfusion.XlsIO;
using System.Net;
using System.Reflection;
using System.Text;

namespace DocumentDecryptionFunction;

public class Function1
{
    [Function("DecryptDocument")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
        AuthorizationLevel.Anonymous,
        "post",
        Route = "dotnet-document-decryption-azure-functions/api/DecryptDocument")]
    HttpRequestData req)
    {
        string? password = null;
        byte[]? fileBytes = null;
        string originalFileName = string.Empty;

        try
        {
            var contentType = req.Headers.TryGetValues("Content-Type", out var ct)
                ? ct.FirstOrDefault()
                : null;

            if (string.IsNullOrEmpty(contentType) ||
                !contentType.Contains("multipart/form-data"))
            {
                return await Json(req, HttpStatusCode.BadRequest,
                    new
                    {
                        error = "Expected multipart/form-data request."
                    });
            }

            var boundary = HeaderUtilities
                .RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary)
                .Value;

            if (string.IsNullOrEmpty(boundary))
            {
                return await Json(req, HttpStatusCode.BadRequest,
                    new
                    {
                        error = "Invalid multipart boundary."
                    });
            }

            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection? section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var hasContentDisposition =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition,
                        out var contentDisposition);

                if (!hasContentDisposition || contentDisposition == null)
                    continue;

                if (contentDisposition.IsFileDisposition())
                {
                    originalFileName =
                        contentDisposition.FileNameStar.Value ??
                        contentDisposition.FileName.Value ??
                        "Input";

                    originalFileName = originalFileName.Trim('"');

                    using var ms = new MemoryStream();
                    await section.Body.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }
                else if (contentDisposition.IsFormDisposition())
                {
                    using var sr = new StreamReader(section.Body, Encoding.UTF8);
                    string value = await sr.ReadToEndAsync();

                    if (string.Equals(
                        contentDisposition.Name.Value,
                        "password",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        password = value;
                    }
                }
            }

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return await Json(req, HttpStatusCode.BadRequest,
                    new
                    {
                        error = "Please upload a PDF or Excel document."
                    });
            }
            string extension =
                Path.GetExtension(originalFileName)
                    .ToLowerInvariant();

            using var outputStream = new MemoryStream();

            // PDF Decryption
            if (extension == ".pdf")
            {
                try
                {
                    using var inputStream = new MemoryStream(fileBytes);

                    //Load the PDF file with password
                    using var loadedDocument =
                        new PdfLoadedDocument(inputStream, password);

                    //Set the security permissions to defaul
                    loadedDocument.Security.Permissions =
                        PdfPermissionsFlags.Default;

                    //Set the owner and user password to empty string to remove the password protection
                    loadedDocument.Security.OwnerPassword =
                        string.Empty;

                    loadedDocument.Security.UserPassword =
                        string.Empty;

                    //Save the decrypted PDF document to the output stream
                    loadedDocument.Save(outputStream);
                }
                catch
                {
                    return await Json(req,
                        HttpStatusCode.Unauthorized,
                        new
                        {
                            error = "Incorrect PDF password."
                        });
                }

                outputStream.Position = 0;

                string outputFileName =
                    $"{Path.GetFileNameWithoutExtension(originalFileName)}-Decrypted.pdf";

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                response.Headers.Add(
                    "Content-Type",
                    "application/pdf");

                response.Headers.Add(
                    "Content-Disposition",
                    $"attachment; filename=\"{outputFileName}\"");

                await response.Body.WriteAsync(outputStream.ToArray());

                return response;
            }

            // Excel Decryption
            else if (extension == ".xlsx" || extension == ".xls")
            {
                try
                {
                    //Initialize the ExcelEngine
                    using ExcelEngine excelEngine = new ExcelEngine();

                    IApplication application =
                        excelEngine.Excel;

                    application.DefaultVersion =
                        ExcelVersion.Xlsx;

                    using var inputStream =
                        new MemoryStream(fileBytes);

                    //Open the Excel workbook with password
                    IWorkbook workbook =
                        application.Workbooks.Open(
                            inputStream,
                            ExcelParseOptions.Default,
                            false,
                            password);

                    //Remove the password protection by setting the PasswordToOpen property to an empty string
                    workbook.PasswordToOpen = string.Empty;
                    //Save the decrypted Excel workbook to the output stream
                    workbook.SaveAs(outputStream);
                    workbook.Close();
                }
                catch
                {
                    return await Json(req,
                        HttpStatusCode.Unauthorized,
                        new
                        {
                            error = "Incorrect Excel password."
                        });
                }

                outputStream.Position = 0;

                string outputFileName =
                    $"{Path.GetFileNameWithoutExtension(originalFileName)}-Decrypted.xlsx";

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                response.Headers.Add(
                    "Content-Type",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

                response.Headers.Add(
                    "Content-Disposition",
                    $"attachment; filename=\"{outputFileName}\"");

                await response.Body.WriteAsync(outputStream.ToArray());

                return response;
            }

            return await Json(req,
                HttpStatusCode.BadRequest,
                new
                {
                    error = "Only PDF, XLSX and XLS files are supported."
                });
        }
        catch (Exception ex)
        {
            return await Json(req,
                HttpStatusCode.InternalServerError,
                new
                {
                    error = "Document decryption failed.",
                    detail = ex.Message
                });
        }
    }


    private static async Task<HttpResponseData> Json(HttpRequestData req, HttpStatusCode status, object payload)
    {
        var resp = req.CreateResponse(status);
        resp.Headers.Add("Content-Type", "application/json");
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        await resp.WriteStringAsync(json);
        return resp;
    }
}