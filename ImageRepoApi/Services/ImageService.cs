using ImageConverterApi.Models;
using System.Security.Cryptography;
using SkiaSharp;
using Storage;
using Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImageConverterApi.Services
{
    public class ImageService : IImageService
    {
        private readonly DatabaseContext _dbContext;

        public ImageService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid> ImportImage(ImageUploadModel model, Stream imageData, string fileName)
        {
            if (!Enum.TryParse<SKEncodedImageFormat>(model.TargetFormat, true, out var format))
                throw new ArgumentException($"Invalid image format: {model.TargetFormat}");
            
            var resizedImage = ResizeImage(imageData, format, model.TargetWidth, model.TargetHeight, model.KeepAspectRatio);
            string dataHash;
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(resizedImage);
                dataHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

            var existingImage = await _dbContext.Images.FirstOrDefaultAsync(i => i.DataHash == dataHash);
            if (existingImage != null)
            {
                // Return the existing image ID and indicate the duplicate was detected
                return existingImage.ImageId; // Adjustments needed in controller to handle this scenario
            }

            var image = new Image
            {
                CreatedAt = DateTime.UtcNow,
                Data = resizedImage,
                FileName = fileName,
                Width = model.TargetWidth,
                Height = model.TargetHeight,
                ImageFormat = format.ToString().ToLower(),
                DataHash = dataHash
            };

            _dbContext.Images.Add(image);
            await _dbContext.SaveChangesAsync();

            return image.ImageId;
        }


        private byte[] ResizeImage(Stream sourceImage, SKEncodedImageFormat newFormat, int targetWidth, int targetHeight, bool keepAspectRatio = true)
        {
            using var img = SKImage.FromEncodedData(sourceImage);
            int newWidth, newHeight;
            if(keepAspectRatio){
                float aspectRatio = (float)img.Width / img.Height;
                if (targetWidth > 0){
                    newWidth = targetWidth;
                    newHeight = (int)(targetWidth / aspectRatio);
                } else if (targetHeight > 0){
                    newHeight = targetHeight;
                    newWidth = (int)(targetHeight * aspectRatio);
                } else {
                    throw new ArgumentException("Invalid target dimensions");
                }
            } else {
                newWidth = targetWidth;
                newHeight = targetHeight;
            }
            using var resizedImage = ResizeImage(img, newWidth, newHeight);
            using var data = resizedImage.Encode(newFormat, 100);
            return data.ToArray();
        }

        private SKImage ResizeImage(SKImage sourceImage, int newWidth, int newHeight)
        {
            var destRect = new SKRect(0, 0, newWidth, newHeight);
            var srcRect = new SKRect(0, 0, sourceImage.Width, sourceImage.Height);

            using var result = new SKBitmap((int)destRect.Width, (int)destRect.Height);
            using var g = new SKCanvas(result);
            using var p = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            g.DrawImage(sourceImage, srcRect, destRect, p);

            return SKImage.FromBitmap(result);
        }
    }
}
