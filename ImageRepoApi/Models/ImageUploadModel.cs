using System.ComponentModel.DataAnnotations;

namespace ImageConverterApi.Models
{
    public class ImageUploadModel
    {
        public int TargetWidth { get; set; }
        public int TargetHeight { get; set; }
        public string? TargetFormat { get; set; }
        public bool KeepAspectRatio { get; set; }
    }

    public class ImageUploadResult
    {
        public string ImageId { get; set; }
        public bool AlreadyExists { get; set; }
    }
}
