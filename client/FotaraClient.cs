using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.properties;
using istd_offline_csharp.utils;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;


namespace istd_offline_csharp.client;

public class FotaraClient
{
    private readonly ILogger<FotaraClient> log;

    private readonly PropertiesManager propertiesManager;

    public FotaraClient(PropertiesManager propertiesManager)
    {
        this.propertiesManager = propertiesManager;
    }
    
    public CertificateResponse complianceCsr(string otp, string csrEncoded)
    {
        try
        {
            var httpClient = new HttpClient();
            var requestBody = $"{{ \"csr\":\"{csrEncoded}\" }}";
            var url = propertiesManager.getProperty("fotara.api.url.compliance.csr");

            var request = getComplianceCsrHttpRequest(otp, url, requestBody);

            log.LogDebug("compliance CSR [{Url}]", url);

            var response = httpClient.Send(request);

            log.LogDebug("Response Code: {StatusCode}", response.StatusCode);

            if ((int)response.StatusCode / 100 == 2)
            {
                using var reader = new StreamReader(response.Content.ReadAsStream());
                var body = reader.ReadToEnd();
                return JsonUtils.readJson<CertificateResponse>(body);
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed to compliance CSR");
        }

        return null;
    }
    
    public ComplianceInvoiceResponse complianceInvoice(CertificateResponse complianceCsrResponse, string jsonBody)
    {
        try
        {
            var httpClient = new HttpClient();

            string url = propertiesManager.getProperty("fotara.api.url.compliance.invoice");
            string auth = $"{complianceCsrResponse.binarySecurityToken}:{complianceCsrResponse.binarySecurityToken}";
            string encodedAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
            string authHeader = $"Basic {encodedAuth}";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", authHeader);

            HttpResponseMessage httpResponse = httpClient.Send(request);
            string responseBody = httpResponse.Content.ReadAsStringAsync().Result;

            if ((int)httpResponse.StatusCode / 100 != 5)
            {
                responseBody = responseBody.Replace("\n", "");
                return JsonUtils.readJson<ComplianceInvoiceResponse>(responseBody);
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed to compliance invoice");
        }

        return null;
    }
    
    public CertificateResponse getProdCertificate(CertificateResponse complianceResponse, long requestId)
    {
        try
        {
            var httpClient = new HttpClient();

            string url = propertiesManager.getProperty("fotara.api.url.prod.certificate");
            string auth = $"{complianceResponse.binarySecurityToken}:{complianceResponse.binarySecurityToken}";
            string encodedAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
            string authHeader = $"Basic {encodedAuth}";

            string jsonBody = $"{{\"compliance_request_id\":\"{requestId}\"}}";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", authHeader);

            HttpResponseMessage response = httpClient.Send(request);
            log.LogDebug($"Response Code: {(int)response.StatusCode}");

            if ((int)response.StatusCode / 100 == 2)
            {
                string responseBody = response.Content.ReadAsStringAsync().Result;
                return JsonUtils.readJson<CertificateResponse>(responseBody);
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "failed to compliance CSR");
        }

        return null;
    }
    
    public EInvoiceResponse submitInvoice(CertificateResponse productionCertificateResponse, string jsonBody)
    {
        try
        {
            var httpClient = new HttpClient();
            string url = propertiesManager.getProperty("fotara.api.url.prod.invoice");

            string auth = $"{productionCertificateResponse.binarySecurityToken}:{productionCertificateResponse.binarySecurityToken}";
            string encodedAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
            string authHeader = $"Basic {encodedAuth}";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", authHeader);

            var response = httpClient.Send(request);
            Console.WriteLine($"Response Code: {(int)response.StatusCode}");

            if ((int)response.StatusCode / 100 == 2)
            {
                string responseBody = response.Content.ReadAsStringAsync().Result;
                return JsonUtils.readJson<EInvoiceResponse>(responseBody);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"failed to compliance CSR [{e.Message}]");
        }

        return null;
    }

    private HttpRequestMessage getDefaultHttpRequest(string jsonBody, string url, string authHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Accept-Language", "en");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Accept-Version", "V2");
        request.Headers.Add("Authorization", authHeader);

        return request;
    }
    
    private HttpRequestMessage getComplianceCsrHttpRequest(string otp, string url, string requestBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("OTP", otp);
        request.Headers.Add("Accept-Language", "en");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Accept-Version", "V2");

        return request;
    }
    
    public EInvoiceResponse reportInvoice(CertificateResponse productionCertificateResponse, string jsonBody)
    {
        try
        {
            var httpClient = new HttpClient();
            var url = propertiesManager.getProperty("fotara.api.url.prod.report.invoice");

            var auth = $"{productionCertificateResponse.binarySecurityToken}:{productionCertificateResponse.binarySecurityToken}";
            var encodedAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
            var authHeader = $"Basic {encodedAuth}";

            var request = getDefaultHttpRequest(jsonBody, url, authHeader);
            var response = httpClient.Send(request);

            log.LogDebug("Response Code: {StatusCode}", (int)response.StatusCode);

            if ((int)response.StatusCode / 100 == 2)
            {
                var responseBody = response.Content.ReadAsStringAsync().Result;
                return JsonUtils.readJson<EInvoiceResponse>(responseBody);
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "failed to report invoice");
        }

        return null;
    }
    
}