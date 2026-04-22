using Microsoft.AspNetCore.Http;

namespace OneSBackend.DTOs
{
    public class UploadImageRequestDto
    {
        public IFormFile? File { get; set; }
    }
}

