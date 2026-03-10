using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Base.Core.Email.Models;
using Base.Core.Configuration;
using Base.Core.Email;

namespace Base.Core.Email;

public class SendMailService : ISendMailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SendMailService> _logger;
    private readonly string _apiUrl;
    private readonly string _apiToken;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public SendMailService(IConfiguration configuration, ILogger<SendMailService> logger)
    {
        _logger = logger;

        var zeptoMailSettings = configuration.GetSection("ZeptoMailSettings").Get<ZeptoMailSettings>();
        if (zeptoMailSettings == null)
        {
            _logger.LogWarning("ZeptoMail settings not configured. Running in development mode.");
            _apiUrl = "https://api.zeptomail.com/v1.1";
            _apiToken = "development-token";
            _fromAddress = "noreply@Eradia.com";
            _fromName = "Eradia Development";
        }
        else
        {
            _apiUrl = zeptoMailSettings.Url ?? throw new Exception("ZeptoMail Url not configured.");
            _apiToken = zeptoMailSettings.Token ?? throw new Exception("ZeptoMail Token not configured.");
            _fromAddress = zeptoMailSettings.FromAddress ?? throw new Exception("ZeptoMail FromAddress not configured.");
            _fromName = zeptoMailSettings.FromName ?? throw new Exception("ZeptoMail FromName not configured.");
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_apiUrl + "/email/template")
        };

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
        );

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Zoho-enczapikey {_apiToken}");
    }

    #region Enqueue Methods (Background Jobs)

    public void EnqueueWelcomeEmail(string toEmail, WelcomeModel model)
    {
        try
        {
            BackgroundJob.Enqueue(() => SendWelcomeEmail(toEmail, model));
            _logger.LogDebug("Welcome email job enqueued successfully for {Email}", toEmail);
        }
        catch (Exception ex)
        {
            // Log do erro mas nÃ£o lanÃ§a exceÃ§Ã£o - email Ã© nÃ£o crÃ­tico
            _logger.LogWarning(ex, "Failed to enqueue welcome email job for {Email}. This is non-critical.", toEmail);
            // NÃ£o fazer throw - permitir que o processo continue normalmente
        }
    }

    public void EnqueueVerificationCodeEmail(string toEmail, VerificationCodeModel model)
    {
        try
        {
            BackgroundJob.Enqueue(() => SendVerificationCodeEmail(toEmail, model));
            _logger.LogDebug("Verification code email job enqueued successfully for {Email}", toEmail);
        }
        catch (Exception ex)
        {
            // Log do erro mas nÃ£o lanÃ§a exceÃ§Ã£o - email Ã© nÃ£o crÃ­tico
            _logger.LogWarning(ex, "Failed to enqueue verification code email job for {Email}. This is non-critical.", toEmail);
            // NÃ£o fazer throw - permitir que o processo continue normalmente
        }
    }

    #endregion

    #region Email Sending Methods

    /// <summary>
    /// Envia e-mail de boas-vindas usando template.
    /// </summary>
    public async Task<bool> SendWelcomeEmail(string email, WelcomeModel model)
    {
        try
        {
            var body = new
            {
                mail_template_key = "2d6f.65f0e970e073d629.k1.bed55511-10d0-11f1-8b85-765e7256bde4.19c8b3adce0",
                from = new
                {
                    address = _fromAddress,
                    name = _fromName
                },
                to = new[]
                {
                    new
                    {
                        email_address = new
                        {
                            address = email,
                            name = $"{model.Name} {model.LastName}".Trim()
                        }
                    }
                },
                merge_info = new
                {
                    user_name = $"{model.Name} {model.LastName}".Trim(),
                    app_url = "https://Eradia.com", // TODO: Replace with actual app URL
                    year = DateTime.UtcNow.Year
                }
            };

            return await PostAsync(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Envia e-mail de cÃ³digo de verificaÃ§Ã£o usando template.
    /// </summary>
    public async Task<bool> SendVerificationCodeEmail(string email, VerificationCodeModel model)
    {
        try
        {
            var body = new
            {
                mail_template_key = "2d6f.65f0e970e073d629.k1.d246bfd1-10d0-11f1-8b85-765e7256bde4.19c8b3b5c4c",
                from = new
                {
                    address = _fromAddress,
                    name = _fromName
                },
                to = new[]
                {
                    new
                    {
                        email_address = new
                        {
                            address = email,
                            name = model.Name
                        }
                    }
                },
                merge_info = new
                {
                    user_name = model.Name,
                    verification_code = model.OTP,
                    expiration_minutes = model.ExpirationMinutes.ToString(),
                    year = DateTime.UtcNow.Year
                }
            };

            return await PostAsync(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification code email to {Email}", email);
            return false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Faz o POST para o endpoint da ZeptoMail.
    /// </summary>
    private async Task<bool> PostAsync(object jsonBody)
    {
        try
        {
            // Em modo de desenvolvimento, apenas loga o email
            if (_apiToken == "development-token")
            {
                _logger.LogInformation("Development mode: email sending skipped.");
                return true;
            }

            var json = JsonConvert.SerializeObject(jsonBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully via ZeptoMail");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ZeptoMail API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via ZeptoMail API");
            return false;
        }
    }

    #endregion
}



