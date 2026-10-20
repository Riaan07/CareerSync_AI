using System.Net;
using System.Text;
using CareerSyncAI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CareerSyncAI.Tests;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("082 345 6789", "+27823456789")]
    [InlineData("0823456789", "+27823456789")]
    [InlineData("(082) 345-6789", "+27823456789")]
    [InlineData("+27 82 345 6789", "+27823456789")]
    [InlineData("0027823456789", "+27823456789")]
    [InlineData("27823456789", "+27823456789")]
    [InlineData("+44 7700 900123", "+447700900123")]
    public void ToE164_ConvertsCommonFormats(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumbers.ToE164(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("082 345 678")]      // SA number one digit short
    [InlineData("082 345 67890")]    // one digit too many
    [InlineData("12345")]            // no leading 0 or +, ambiguous
    public void ToE164_RejectsNumbersThatCantBeTexted(string? input)
    {
        Assert.Null(PhoneNumbers.ToE164(input));
    }

    [Fact]
    public void Mask_ShowsOnlyCountryCodeAndLastFourDigits()
    {
        Assert.Equal("+27 •• ••• 6789", PhoneNumbers.Mask("+27823456789"));
    }
}

public class TwilioVerifyServiceTests
{
    /// <summary>Records each request and replies with a canned response.</summary>
    private sealed class FakeTwilio : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public HttpRequestMessage? Request;
        public string? RequestBody;
        public FakeTwilio(HttpStatusCode status, string body) { _status = status; _body = body; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
        }
    }

    private static (TwilioVerifyService Service, FakeTwilio Fake) Create(HttpStatusCode status, string body)
    {
        var fake = new FakeTwilio(status, body);
        var options = new TwilioOptions { AccountSid = "ACtest123", AuthToken = "secret-token", VerifyServiceSid = "VAservice456" };
        return (new TwilioVerifyService(new HttpClient(fake), options, NullLogger<TwilioVerifyService>.Instance), fake);
    }

    [Fact]
    public async Task SendCode_PostsToVerificationsWithBasicAuthAndSmsChannel()
    {
        var (service, fake) = Create(HttpStatusCode.Created, """{"status":"pending"}""");

        var result = await service.SendCodeAsync("+27823456789");

        Assert.Equal(SmsSendStatus.Sent, result);
        Assert.Equal(HttpMethod.Post, fake.Request!.Method);
        Assert.Equal("https://verify.twilio.com/v2/Services/VAservice456/Verifications", fake.Request.RequestUri!.ToString());
        Assert.Equal("Basic", fake.Request.Headers.Authorization!.Scheme);
        Assert.Equal("ACtest123:secret-token",
            Encoding.ASCII.GetString(Convert.FromBase64String(fake.Request.Headers.Authorization.Parameter!)));
        Assert.Equal("To=%2B27823456789&Channel=sms", fake.RequestBody);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 60200, SmsSendStatus.InvalidNumber)]
    [InlineData(HttpStatusCode.TooManyRequests, 60203, SmsSendStatus.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized, 20003, SmsSendStatus.Failed)]
    [InlineData(HttpStatusCode.BadRequest, 21608, SmsSendStatus.Failed)]
    public async Task SendCode_MapsTwilioErrors(HttpStatusCode status, int code, SmsSendStatus expected)
    {
        var (service, _) = Create(status, $$"""{"code":{{code}},"message":"error","status":{{(int)status}}}""");

        Assert.Equal(expected, await service.SendCodeAsync("+27823456789"));
    }

    [Fact]
    public async Task CheckCode_PostsToVerificationCheckAndReturnsApproved()
    {
        var (service, fake) = Create(HttpStatusCode.OK, """{"status":"approved","valid":true}""");

        var result = await service.CheckCodeAsync("+27823456789", "123456");

        Assert.Equal(SmsCheckStatus.Approved, result);
        Assert.Equal("https://verify.twilio.com/v2/Services/VAservice456/VerificationCheck", fake.Request!.RequestUri!.ToString());
        Assert.Equal("To=%2B27823456789&Code=123456", fake.RequestBody);
    }

    [Fact]
    public async Task CheckCode_WrongCodeIsIncorrect()
    {
        var (service, _) = Create(HttpStatusCode.OK, """{"status":"pending","valid":false}""");

        Assert.Equal(SmsCheckStatus.Incorrect, await service.CheckCodeAsync("+27823456789", "000000"));
    }

    [Fact]
    public async Task CheckCode_NotFoundMeansExpiredOrUsed()
    {
        var (service, _) = Create(HttpStatusCode.NotFound, """{"code":20404,"message":"The requested resource was not found"}""");

        Assert.Equal(SmsCheckStatus.ExpiredOrMissing, await service.CheckCodeAsync("+27823456789", "123456"));
    }

    [Fact]
    public async Task NetworkFailureIsReportedNotThrown()
    {
        var options = new TwilioOptions { AccountSid = "AC1", AuthToken = "t", VerifyServiceSid = "VA1", ApiBaseUrl = "http://127.0.0.1:1" };
        var service = new TwilioVerifyService(new HttpClient(), options, NullLogger<TwilioVerifyService>.Instance);

        Assert.Equal(SmsSendStatus.Failed, await service.SendCodeAsync("+27823456789"));
        Assert.Equal(SmsCheckStatus.Failed, await service.CheckCodeAsync("+27823456789", "123456"));
    }
}
