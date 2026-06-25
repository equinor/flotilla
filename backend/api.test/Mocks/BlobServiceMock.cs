using System.Threading.Tasks;
using Api.Services;
using Api.Services.Models;
using Azure;
using Azure.Storage.Blobs.Models;

namespace Api.Test.Mocks
{
    public class MockBlobService : IBlobService
    {
        public async Task<BlobDownload?> DownloadBlob(
            string blobName,
            string containerName,
            string accountName
        )
        {
            await Task.CompletedTask;
            return new BlobDownload([], null);
        }

        public AsyncPageable<BlobItem> FetchAllBlobs(string containerName, string accountName)
        {
            var page = Page<BlobItem>.FromValues([], continuationToken: null, response: null!);
            var pages = AsyncPageable<BlobItem>.FromPages([page]);
            return pages;
        }

        public async Task UploadJsonToBlob(
            string json,
            string path,
            string containerName,
            string accountName,
            bool overwrite = false
        )
        {
            await Task.CompletedTask;
        }
    }
}
