using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Services;
using Azure;
using Azure.Storage.Blobs.Models;

namespace Api.Test.Mocks
{
    public class MockBlobService : IBlobService
    {
        public async Task<byte[]> DownloadBlob(string blobName, string containerName, string accountName)
        {
            using var memoryStream = new System.IO.MemoryStream();
            return memoryStream.ToArray();
        }

        public AsyncPageable<BlobItem> FetchAllBlobs(string containerName, string accountName)
        {
            var page = Page<BlobItem>.FromValues(new List<BlobItem>(), continuationToken: null, null);
            var pages = AsyncPageable<BlobItem>.FromPages(new[] { page });
            return pages;
        }

        public async void UploadJsonToBlob(string json, string path, string containerName, string accountName, bool overwrite = false)
        {
        }
    }
}
