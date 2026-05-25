using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace SmartWasteManagement.Services;

public interface ICloudinaryService
{
    bool IsConfigured { get; }
    Task<string?> UploadImageAsync(Stream stream, string fileName, string folder);
}

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var url = Environment.GetEnvironmentVariable("CLOUDINARY_URL")
            ?? configuration["CLOUDINARY_URL"]
            ?? configuration["Cloudinary:Url"];

        if (!string.IsNullOrWhiteSpace(url))
            _cloudinary = new Cloudinary(url);
    }

    public bool IsConfigured => _cloudinary is not null;

    public async Task<string?> UploadImageAsync(Stream stream, string fileName, string folder)
    {
        if (_cloudinary is null)
            return null;

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = folder,
            Overwrite = true,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        return string.IsNullOrEmpty(result.SecureUrl?.ToString())
            ? null
            : result.SecureUrl.ToString();
    }
}
