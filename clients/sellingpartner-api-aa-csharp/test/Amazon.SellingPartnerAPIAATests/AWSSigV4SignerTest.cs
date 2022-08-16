using Moq;
using Xunit;
using Amazon.SellingPartnerAPIAA;
using System;
using System.Net.Http;
using RestSharp;

namespace Amazon.SellingPartnerAPIAATests
{
    public class AWSSigV4SignerTest
    {
        private const string TestAccessKeyId = "aKey";
        private const string TestSecretKey = "sKey";
        private const string TestRegion = "us-east-1";
        private const string TestResourcePath = "iam/user";
        private const string TestHost = "sellingpartnerapi.amazon.com";
        private const string TestUri = "https://" + TestHost + "/" + TestResourcePath;

        private HttpRequestMessage request;
        private AWSSigV4Signer sigV4SignerUnderTest;
        private Mock<AWSSignerHelper> mockAWSSignerHelper;

        public AWSSigV4SignerTest()
        {
            request = new HttpRequestMessage(HttpMethod.Get, TestUri);

            AWSAuthenticationCredentials authenticationCredentials = new AWSAuthenticationCredentials
            {
                AccessKeyId = TestAccessKeyId,
                SecretKey = TestSecretKey,
                Region = TestRegion
            };
            mockAWSSignerHelper = new Mock<AWSSignerHelper>();
            sigV4SignerUnderTest = new AWSSigV4Signer(authenticationCredentials);
            sigV4SignerUnderTest.AwsSignerHelper = mockAWSSignerHelper.Object;
        }

        [Fact]
        public void TestSignRequest()
        {
            DateTime signingDate = DateTime.UtcNow;
            string expectedHashedCanonicalRequest = "b7a5ea4c3179fcebed77f19ccd7d85795d4b7a1810709b55fa7ad3fd79ab6adc";
            string expectedSignedHeaders = "testSignedHeaders";
            string expectedSignature = "testSignature";
            string expectedStringToSign = "testStringToSign";
            mockAWSSignerHelper.Setup(signerHelper => signerHelper.InitializeHeaders(request))
                               .Returns(signingDate);
            mockAWSSignerHelper.Setup(signerHelper => signerHelper.ExtractCanonicalURIParameters(request))
                               .Returns("testURIParameters");
            mockAWSSignerHelper.Setup(signerHelper => signerHelper.ExtractCanonicalQueryString(request))
                               .Returns("testCanonicalQueryString");
            mockAWSSignerHelper.Setup(signerHelper => signerHelper.ExtractCanonicalHeaders(request))
                               .Returns("testCanonicalHeaders");
            mockAWSSignerHelper.Setup(signerHelper => signerHelper.ExtractSignedHeaders(request))
                               .Returns(expectedSignedHeaders);
            mockAWSSignerHelper.Setup(signerHelper => signerHelper.HashRequestBody(request))
                               .Returns("testHashRequestBody");
            mockAWSSignerHelper.Setup(signerHelper => signerHelper.BuildStringToSign(signingDate,
                                      expectedHashedCanonicalRequest, TestRegion))
                               .Returns(expectedStringToSign);
            mockAWSSignerHelper.Setup(signerHelper => signerHelper.CalculateSignature(expectedStringToSign,
                                      signingDate, TestSecretKey, TestRegion))
                               .Returns(expectedSignature);

            HttpRequestMessage actualRestRequest = sigV4SignerUnderTest.Sign(request);

            mockAWSSignerHelper.Verify(signerHelper => signerHelper.InitializeHeaders(request));
            mockAWSSignerHelper.Verify(signerHelper => signerHelper.ExtractCanonicalURIParameters(request));
            mockAWSSignerHelper.Verify(signerHelper => signerHelper.ExtractCanonicalQueryString(request));
            mockAWSSignerHelper.Verify(signerHelper => signerHelper.ExtractCanonicalHeaders(request));
            mockAWSSignerHelper.Verify(signerHelper => signerHelper.ExtractSignedHeaders(request));
            mockAWSSignerHelper.Verify(signerHelper => signerHelper.HashRequestBody(request));
            mockAWSSignerHelper.Verify(signerHelper => signerHelper.BuildStringToSign(signingDate,
                                                                                      expectedHashedCanonicalRequest,
                                                                                      TestRegion));
            mockAWSSignerHelper.Verify(signerHelper => signerHelper.CalculateSignature(expectedStringToSign,
                                                                                       signingDate,
                                                                                       TestSecretKey,
                                                                                       TestRegion));
            mockAWSSignerHelper.Verify(signerHelper => signerHelper.AddSignature(request,
                                                                                 TestAccessKeyId,
                                                                                 expectedSignedHeaders,
                                                                                 expectedSignature,
                                                                                 TestRegion,
                                                                                 signingDate));

            Assert.Equal(request, actualRestRequest);
        }

        [Fact]
        public void TestExtractCanonicalURIParameters()
        {
            var expectedCanonicalUri = "/listings/2021-08-01/items/A2GP3WG5N6CP41/M_24.50_B00E5DSYL0_42.32_83%252C564_06%252F20";

            var restRequest = new HttpRequestMessage();
            restRequest.RequestUri = new Uri("https://sellingpartnerapi-na.amazon.com/listings/2021-08-01/items/A2GP3WG5N6CP41/{sku}?marketplaceIds=ATVPDKIKX0DER&issueLocale=&includedData=issues");
            restRequest.Properties.Add("sku", "M_24.50_B00E5DSYL0_42.32_83,564_06/20");
            var generatedCanonicalUri = (new AWSSignerHelper()).ExtractCanonicalURIParameters(restRequest);

            Assert.Equal(expectedCanonicalUri, generatedCanonicalUri) ;
        }
    }
}
