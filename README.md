# Document Decryption Tool Using Azure Functions and Syncfusion® Libraries

A serverless Azure Functions application that demonstrates how to decrypt password-protected PDF and Excel documents using the **Syncfusion .NET PDF Library** and **Syncfusion XlsIO Library**. The application provides a modern web interface for uploading encrypted documents, entering passwords, and downloading decrypted files securely.

## Pages

| Page | Route | Description |
| --- | --- | --- |
| **Document Decryption Tool** | `/api/Ui` | Upload password-protected PDF or Excel documents, provide the password, and download the decrypted file. |
| **Document Decryption API** | `/api/DecryptDocument` | Backend API endpoint that processes uploaded PDF and Excel files and returns the decrypted document. |

## Features

* Decrypt password-protected PDF documents
* Decrypt password-protected Excel workbooks (`.xlsx`, `.xls`)
* Browser-based drag-and-drop file upload
* Secure server-side document processing
* Password input with visibility toggle
* Automatic download of decrypted documents
* Supports PDF and Excel file validation before processing
* Clear error reporting for invalid passwords and unsupported file types
* Azure Functions isolated worker (.NET 8) architecture

## Supported File Types

| File Type | Extension |
| ---------- | ---------- |
| PDF Document | `.pdf` |
| Excel Workbook | `.xlsx` |
| Excel Workbook (Legacy) | `.xls` |

## Project Structure

```text
DocumentDecryptionFunction
├── Functions
│   ├── Function1.cs
│   └── UiFunction.cs
├── wwwroot
│   ├── index.html
│   ├── styles.css
│   └── script.js
├── Properties
│   └── launchSettings.json
├── Program.cs
├── host.json
├── local.settings.json
└── DocumentDecryptionFunction.csproj
```

## How It Works

### PDF Decryption

The application uses the Syncfusion PDF Library to:

* Open encrypted PDF documents
* Validate the provided password
* Remove user and owner passwords
* Save and return the decrypted PDF document

### Excel Decryption

The application uses Syncfusion XlsIO to:

* Open encrypted Excel workbooks
* Validate the provided password
* Remove workbook open-password protection
* Save and return the decrypted workbook

## Run

```powershell
cd DocumentDecryptionFunction
dotnet restore
dotnet build
func start
```

Alternatively:

```powershell
dotnet run
```

Open the application in your browser:

```text
http://localhost:7154/dotnet-document-decryption-azure-functions/

## Required NuGet Packages

```xml
<PackageReference Include="Syncfusion.Pdf.Net.Core" Version="34.2.8" />
<PackageReference Include="Syncfusion.XlsIO.Net.Core" Version="34.2.8" />
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.52.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore" Version="2.1.0" />
```

## Syncfusion License

Register your Syncfusion license using one of the following methods:

* Environment variable: `SYNCFUSION_LICENSE_KEY`
* Azure Application Settings
* `Program.cs` using:

```csharp
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
    "YOUR LICENSE KEY");
```

A free community license is available at:[License](
https://www.syncfusion.com/products/communitylicense)

## Sample Request

The application accepts a multipart/form-data request containing:

```text
file       - PDF or Excel document
password   - Document password
```

## Security Notes

* Documents are processed in memory.
* Uploaded files are not permanently stored.
* Passwords are used only during document processing.
* Decrypted files are returned directly to the client for download.

## Technology Stack

* .NET 8
* Azure Functions v4 (Isolated Worker)
* Syncfusion PDF Library
* Syncfusion XlsIO
* HTML, CSS, JavaScript
* OpenTelemetry
* Application Insights

## Use Cases

* Remove password protection from PDF files
* Remove workbook passwords from Excel files
* Document migration and archival workflows
* Secure document processing in cloud environments
* Integration with enterprise document management systems
