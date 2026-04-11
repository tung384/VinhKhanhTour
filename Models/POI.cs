using SQLite;

namespace OneSProject.Models;

public class POI
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;// Tên gốc (thường là tiếng Việt)

    // Tọa độ để Thành viên 2 (GPS) sử dụng
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double DetectionRadius { get; set; } = 25; // Mặc định 5m
    public int Priority { get; set; } // Thứ tự ưu tiên (1, 2, 3...)

    public string MainImage { get; set; } = string.Empty; // Đường dẫn ảnh
    public string QrCode { get; set; } = string.Empty; // Mã định danh QR
}

public class POIImage
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int POIId { get; set; } // Liên kết với POI.Id
    public string FileName { get; set; } = string.Empty; // Tên file trong Resources
}

public class POITranslation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int POIId { get; set; } // Liên kết với bảng POI
    public string LanguageCode { get; set; } = "vi"; // vi, en, zh, ko...

    public string Description { get; set; } = string.Empty; // Văn bản hiển thị
    public string DetailedDescription { get; set; } = string.Empty; // Mô tả dài (Chi tiết)
    public string AudioScript { get; set; } = string.Empty; // Kịch bản cho Thành viên 4 (TTS)
}