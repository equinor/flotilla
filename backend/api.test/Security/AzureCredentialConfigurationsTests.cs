using System.Collections.Generic;
using Api.Configurations;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Api.Test.Security
{
    public class AzureCredentialConfigurationsTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void KeyVaultCredentialPreservesConfiguredIdentity(bool useAppSettings)
        {
            var settings = new Dictionary<string, string?>
            {
                ["AZURE_TENANT_ID"] = "environment-tenant",
                ["AZURE_CLIENT_ID"] = "environment-client",
            };
            if (useAppSettings)
            {
                settings["AzureAd:TenantId"] = "configured-tenant";
                settings["AzureAd:ClientId"] = "configured-client";
            }
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            var options = CustomServiceConfigurations.CreateKeyVaultCredentialOptions(
                configuration
            );

            Assert.Equal(
                useAppSettings ? "configured-tenant" : "environment-tenant",
                options.TenantId
            );
            Assert.Equal(
                useAppSettings ? "configured-client" : "environment-client",
                options.WorkloadIdentityClientId
            );
            Assert.False(options.ExcludeWorkloadIdentityCredential);
            Assert.False(options.ExcludeAzureCliCredential);
            Assert.NotNull(new DefaultAzureCredential(options));
        }

        [Theory]
        [InlineData(false, "test-secret", false)]
        [InlineData(true, "test-secret", true)]
        [InlineData(true, null, false)]
        [InlineData(true, "Fill in client secret", false)]
        public void RuntimeCredentialOnlyAddsClientSecretWhenExplicitlyAllowed(
            bool allowClientSecret,
            string? clientSecret,
            bool expectChain
        )
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["AzureAd:TenantId"] = "test-tenant",
                        ["AzureAd:ClientId"] = "test-client",
                        ["AzureAd:ClientSecret"] = clientSecret,
                        ["AzureAd:AllowUsingClientSecret"] = allowClientSecret.ToString(),
                    }
                )
                .Build();

            var credential = CustomServiceConfigurations.CreateCredential(configuration);

            if (expectChain)
                Assert.IsType<ChainedTokenCredential>(credential);
            else
                Assert.IsType<WorkloadIdentityCredential>(credential);
        }
    }
}
