using System.Text.RegularExpressions;
using Api.Utilities;
using Azure.Core;
using Azure.Storage.Blobs;

namespace Api.Configurations;

public static class MissionRecordingConfigurations
{
    public static string GetRecordingContainerName(IConfiguration configuration)
    {
        string? name = configuration["MissionRecording:ContainerName"];
        if (
            name is null
            || !Regex.IsMatch(name, @"\A[a-z0-9][a-z0-9-]{1,61}[a-z0-9]\z")
            || name.Contains("--", StringComparison.Ordinal)
        )
            throw new ConfigException(
                "MissionRecording:ContainerName must be explicitly set to a valid Blob container name: "
                    + "3-63 lowercase letters, digits, or hyphens; start/end with a letter or digit; no consecutive hyphens."
            );
        return name;
    }

    public static BlobServiceClient CreateRecordingStorageClient(
        IConfiguration configuration,
        TokenCredential credential
    ) =>
        (configuration["MissionRecording:AuthenticationMode"] ?? "ConnectionString") switch
        {
            "ConnectionString" => CreateSharedKeyClient(configuration),
            "WorkloadIdentity" => CreateIdentityClient(configuration, credential),
            _ => throw new InvalidOperationException(
                "MissionRecording:AuthenticationMode must be ConnectionString or WorkloadIdentity."
            ),
        };

    private static BlobServiceClient CreateSharedKeyClient(IConfiguration configuration)
    {
        string? connectionString = configuration["MissionRecording:StorageConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "MissionRecording:StorageConnectionString is required in ConnectionString mode."
            );

        BlobServiceClient client;
        try
        {
            client = new BlobServiceClient(connectionString);
        }
        catch (Exception error) when (error is ArgumentException or FormatException)
        {
            throw new InvalidOperationException(
                "MissionRecording:StorageConnectionString must be a valid account-key connection string."
            );
        }

        if (!client.CanGenerateAccountSasUri)
            throw new InvalidOperationException(
                "MissionRecording:StorageConnectionString must contain an account key to sign video URLs."
            );
        return client;
    }

    private static BlobServiceClient CreateIdentityClient(
        IConfiguration configuration,
        TokenCredential credential
    )
    {
        if (
            !Uri.TryCreate(
                configuration["MissionRecording:StorageAccountUrl"],
                UriKind.Absolute,
                out var endpoint
            )
            || endpoint.Scheme != Uri.UriSchemeHttps
            || endpoint.AbsolutePath != "/"
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment)
            || !string.IsNullOrEmpty(endpoint.UserInfo)
        )
            throw new InvalidOperationException(
                "MissionRecording:StorageAccountUrl must be an HTTPS Blob service URL without a path, credentials, query, or fragment."
            );

        return new BlobServiceClient(endpoint, credential);
    }
}
