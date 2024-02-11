using ImageConverterApi.Models;

namespace ImageConverterApi.Services
{
    public interface IImageService
    {
        Task<ImageUploadResult> ImportImage(ImageUploadModel model, Stream imageData, string fileName, string username);
    }
}