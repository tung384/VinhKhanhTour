using OneSProject.Models;
using SQLite;

namespace OneSProject.Services;

public class DatabaseService
{
    public SQLiteAsyncConnection? _database;

    public async Task Init()
    {
        if (_database is not null)
            return;

        _database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);

        // Tạo bảng POI và bảng Dịch thuật
        await _database.CreateTableAsync<Models.POI>();
        await _database.CreateTableAsync<Models.POIImage>();
        await _database.CreateTableAsync<Models.POITranslation>();
        await SeedDataAsync();
    }

    public async Task SeedDataAsync()
    {
        if (_database == null) return;
        var count = await _database!.Table<Models.POI>().CountAsync();
        if (count > 0) return; // Hệ thống đã có dữ liệu.

        // 1. Nạp danh sách POIs
        var samplePOIs = new List<Models.POI>
        {
            new Models.POI { Name = "Ốc Oanh", Latitude = 10.76008, Longitude = 106.70423, Priority = 1, QrCode = "VK01", MainImage = "oco.png" },
            new Models.POI { Name = "Sủi Cảo Vĩnh Khánh", Latitude = 10.76045, Longitude = 106.70448, Priority = 2, QrCode = "VK02", MainImage = "sui_cao.png" },
            new Models.POI { Name = "Phá Lấu Cô Thảo", Latitude = 10.75982, Longitude = 106.70375, Priority = 3, QrCode = "VK03", MainImage = "pha_lau.png" },
            new Models.POI { Name = "Bò Né Thanh Tuyền", Latitude = 10.76088, Longitude = 106.70485, Priority = 4, QrCode = "VK04", MainImage = "bo_ne.png" },
            new Models.POI { Name = "Lẩu Bò Khu Nhà Cháy", Latitude = 10.76125, Longitude = 106.70512, Priority = 5, QrCode = "VK05", MainImage = "lau.png" },
            new Models.POI { Name = "Ốc Đào", Latitude = 10.76150, Longitude = 106.70535, Priority = 6, QrCode = "VK06", MainImage = "ocd_1.png" },
            new Models.POI { Name = "Chè Hà Trâm", Latitude = 10.75950, Longitude = 106.70340, Priority = 7, QrCode = "VK07", MainImage = "che.png" }
        };
        await _database.InsertAllAsync(samplePOIs);

        // 2. Nạp kịch bản đa ngôn ngữ (Ví dụ cho 2 gian hàng đầu)
        var translations = new List<Models.POITranslation>
        {
            // Ốc Oanh
            new Models.POITranslation
            {
                POIId = 1,
                LanguageCode = "vi",
                Description = "Quán ốc nhộn nhịp nhất khu phố.",
                DetailedDescription = "Đây là một trong những quán ốc nổi tiếng với cách chế biến sạch sẽ, gia vị đậm đà. Các món như ốc xào tỏi hay rang muối đều thơm lừng, hấp dẫn. Nước chấm mắm gừng sánh nhẹ, chua ngọt vừa phải, giúp tăng hương vị hải sản. Dù quán khá đông nhưng vẫn gọn gàng, tạo cảm giác dễ chịu.",
            },
            new Models.POITranslation
            {
                POIId = 1,
                LanguageCode = "en",
                Description = "The busiest snail spot on the street.",
                DetailedDescription = "This is one of the most famous snail restaurants on the street, known for its clean preparation and bold seasoning. Dishes like garlic stir-fried snails or salt-roasted snails are fragrant and very tempting. The ginger fish sauce dip has a light thickness with a balanced sweet-and-sour taste that enhances the seafood flavor. Although the place is often crowded, it remains tidy and comfortable.",
                AudioScript = "This is one of the most famous snail restaurants on the street, known for its clean preparation and bold seasoning. Dishes like garlic stir-fried snails or salt-roasted snails are fragrant and very tempting. The ginger fish sauce dip has a light thickness with a balanced sweet-and-sour taste that enhances the seafood flavor. Although the place is often crowded, it remains tidy and comfortable."
            },

            // Sủi Cảo
            new Models.POITranslation
            {
                POIId = 2,
                LanguageCode = "vi",
                Description = "Sủi cảo chuẩn vị Hoa.",
                DetailedDescription = "Đây là điểm đến quen thuộc cho những ai yêu thích ẩm thực Trung Hoa. Sủi cảo ở đây có lớp vỏ mỏng, dai nhẹ, ôm trọn phần nhân tôm thịt tươi ngon. Nước dùng được ninh từ xương nên có vị thanh ngọt, kết hợp cùng mực khô và rau xanh tạo nên hương vị hài hòa. Không gian quán bình dân, gần gũi, phục vụ nhanh, rất thích hợp cho một bữa tối ấm bụng.",
                AudioScript = "Đây là điểm đến quen thuộc cho những ai yêu thích ẩm thực Trung Hoa. Sủi cảo ở đây có lớp vỏ mỏng, dai nhẹ, ôm trọn phần nhân tôm thịt tươi ngon. Nước dùng được ninh từ xương nên có vị thanh ngọt, kết hợp cùng mực khô và rau xanh tạo nên hương vị hài hòa. Không gian quán bình dân, gần gũi, phục vụ nhanh, rất thích hợp cho một bữa tối ấm bụng."
            },
            new Models.POITranslation
            {
                POIId = 2,
                LanguageCode = "en",
                Description = "Authentic Chinese dumplings.",
                DetailedDescription = "This is a familiar spot for those who enjoy Chinese cuisine. The dumplings here have thin yet slightly chewy wrappers that hold a fresh shrimp and pork filling. The broth is simmered from bones, giving it a light natural sweetness, complemented by dried squid and vegetables for a balanced flavor. The restaurant has a simple and friendly atmosphere with quick service, perfect for a warm and satisfying meal.",
                AudioScript = "This is a familiar spot for those who enjoy Chinese cuisine. The dumplings here have thin yet slightly chewy wrappers that hold a fresh shrimp and pork filling. The broth is simmered from bones, giving it a light natural sweetness, complemented by dried squid and vegetables for a balanced flavor. The restaurant has a simple and friendly atmosphere with quick service, perfect for a warm and satisfying meal."
            },

            // Phá Lấu Cô Thảo
            new Models.POITranslation
            {
                POIId = 3,
                LanguageCode = "vi",
                Description = "Quán phá lấu nổi tiếng với hương vị đậm đà.",
                DetailedDescription = "Phá lấu ở đây nổi bật với nước dùng cốt dừa béo, có màu sắc bắt mắt. Lòng bò được làm sạch kỹ, hầm mềm nhưng vẫn giữ độ dai giòn. Khi ăn, chấm bánh mì cùng nước sốt và thêm chút nước mắm tắc sẽ càng đậm đà. Quán nhỏ nhưng luôn đông khách nhờ hương vị đặc trưng.",
                AudioScript = "Phá lấu ở đây nổi bật với nước dùng cốt dừa béo, có màu sắc bắt mắt. Lòng bò được làm sạch kỹ, hầm mềm nhưng vẫn giữ độ dai giòn. Khi ăn, chấm bánh mì cùng nước sốt và thêm chút nước mắm tắc sẽ càng đậm đà. Quán nhỏ nhưng luôn đông khách nhờ hương vị đặc trưng."
            },
            new Models.POITranslation
            {
                POIId = 3,
                LanguageCode = "en",
                Description = "A famous braised offal stall with rich flavor.",
                DetailedDescription = "The braised offal here stands out with a rich coconut-milk broth and an appealing color. The beef offal is carefully cleaned and slowly simmered until tender while still slightly chewy. It is often enjoyed with bread dipped into the sauce and a splash of kumquat fish sauce for extra flavor. Although the shop is small, it is always busy thanks to its distinctive taste.",
                AudioScript = "The braised offal here stands out with a rich coconut-milk broth and an appealing color. The beef offal is carefully cleaned and slowly simmered until tender while still slightly chewy. It is often enjoyed with bread dipped into the sauce and a splash of kumquat fish sauce for extra flavor. Although the shop is small, it is always busy thanks to its distinctive taste."
            },

            // Bò Né Thanh Tuyền
            new Models.POITranslation
            {
                POIId = 4,
                LanguageCode = "vi",
                Description = "Quán bò né hấp dẫn với chảo gang nóng hổi.",
                DetailedDescription = "Đây là quán bò né lâu năm, nổi tiếng với những phần ăn nóng hổi trên chảo gang. Thịt bò được tẩm ướp đậm đà, ăn kèm trứng ốp la, pate béo và chút bơ thơm. Bánh mì luôn giòn, rất hợp để chấm cùng nước sốt đặc trưng của quán. Một lựa chọn lý tưởng cho bữa ăn no nê và đầy năng lượng.",
                AudioScript = "Đây là quán bò né lâu năm, nổi tiếng với những phần ăn nóng hổi trên chảo gang. Thịt bò được tẩm ướp đậm đà, ăn kèm trứng ốp la, pate béo và chút bơ thơm. Bánh mì luôn giòn, rất hợp để chấm cùng nước sốt đặc trưng của quán. Một lựa chọn lý tưởng cho bữa ăn no nê và đầy năng lượng."
            },
            new Models.POITranslation
            {
                POIId = 4,
                LanguageCode = "en",
                Description = "A popular sizzling beef steak stall.",
                DetailedDescription = "This long-standing restaurant is famous for its sizzling Vietnamese steak served on a hot cast-iron plate. The beef is well marinated and served with fried eggs, rich pâté, and fragrant butter. Crispy bread is perfect for dipping into the restaurant’s signature sauce, making it a hearty and energetic meal.",
                AudioScript = "This long-standing restaurant is famous for its sizzling Vietnamese steak served on a hot cast-iron plate. The beef is well marinated and served with fried eggs, rich pâté, and fragrant butter. Crispy bread is perfect for dipping into the restaurant’s signature sauce, making it a hearty and energetic meal."
            },

            // Lẩu Bò Khu Nhà Cháy
            new Models.POITranslation
            {
                POIId = 5,
                LanguageCode = "vi",
                Description = "Quán lẩu bò quen thuộc của người dân địa phương.",
                DetailedDescription = "Bạn đang đến với Lẩu Bò Khu Nhà Cháy, quán ghi điểm nhờ nước lẩu ngọt thanh từ xương, thoang thoảng hương thảo mộc. Thịt bò, gân và đuôi bò được hầm mềm, ăn kèm chao pha đậm đà, béo nhẹ và cay vừa phải. Rau luôn tươi, giúp cân bằng vị giác. Không gian thoáng đãng, thích hợp để tụ tập bạn bè vào buổi tối.",
                AudioScript = "Bạn đang đến với Lẩu Bò Khu Nhà Cháy, quán ghi điểm nhờ nước lẩu ngọt thanh từ xương, thoang thoảng hương thảo mộc. Thịt bò, gân và đuôi bò được hầm mềm, ăn kèm chao pha đậm đà, béo nhẹ và cay vừa phải. Rau luôn tươi, giúp cân bằng vị giác. Không gian thoáng đãng, thích hợp để tụ tập bạn bè vào buổi tối."
            },
            new Models.POITranslation
            {
                POIId = 5,
                LanguageCode = "en",
                Description = "A well-known local beef hotpot restaurant.",
                DetailedDescription = "Welcome to Lau Bo Khu Nha Chay, a popular local spot known for its naturally sweet beef hotpot broth made from simmered bones and aromatic herbs. The beef, tendons, and oxtail are slowly cooked until tender and are often enjoyed with rich fermented tofu sauce. Fresh vegetables balance the flavors, and the open atmosphere makes it a great place to gather with friends in the evening.",
                AudioScript = "Welcome to Lau Bo Khu Nha Chay, a popular local spot known for its naturally sweet beef hotpot broth made from simmered bones and aromatic herbs. The beef, tendons, and oxtail are slowly cooked until tender and are often enjoyed with rich fermented tofu sauce. Fresh vegetables balance the flavors, and the open atmosphere makes it a great place to gather with friends in the evening."
            },

            // Ốc Đào
            new Models.POITranslation
            {
                POIId = 6,
                LanguageCode = "vi",
                Description = "Quán ốc nổi tiếng với nhiều món hải sản đa dạng.",
                DetailedDescription = "Quán mang đậm không khí ẩm thực đường phố sôi động. Các món nướng như nhum mỡ hành hay sò nướng rất được ưa chuộng. Gia vị đậm, hơi cay, tạo cảm giác kích thích vị giác. Đây là điểm hẹn quen thuộc của giới trẻ mỗi tối, lúc nào cũng đông vui và náo nhiệt.",
                AudioScript = "Quán mang đậm không khí ẩm thực đường phố sôi động. Các món nướng như nhum mỡ hành hay sò nướng rất được ưa chuộng. Gia vị đậm, hơi cay, tạo cảm giác kích thích vị giác. Đây là điểm hẹn quen thuộc của giới trẻ mỗi tối, lúc nào cũng đông vui và náo nhiệt."
            },
            new Models.POITranslation
            {
                POIId = 6,
                LanguageCode = "en",
                Description = "A famous seafood stall with many snail dishes.",
                DetailedDescription = "This restaurant captures the lively atmosphere of Vietnamese street food culture. Grilled dishes such as sea urchin with scallion oil and grilled scallops are especially popular. The seasoning is rich and slightly spicy, stimulating the taste buds. It has become a favorite evening gathering spot for young people, always busy and full of energy.",
                AudioScript = "This restaurant captures the lively atmosphere of Vietnamese street food culture. Grilled dishes such as sea urchin with scallion oil and grilled scallops are especially popular. The seasoning is rich and slightly spicy, stimulating the taste buds. It has become a favorite evening gathering spot for young people, always busy and full of energy."
            },

            // Chè Hà Trâm
            new Models.POITranslation
            {
                POIId = 7,
                LanguageCode = "vi",
                Description = "Quán chè nổi tiếng với nhiều món tráng miệng truyền thống.",
                DetailedDescription = "Bạn đang đứng trước quán Chè Hà Trâm, đây là điểm dừng chân lý tưởng sau khi thưởng thức các món mặn. Quán có nhiều loại chè, từ truyền thống đến hiện đại như chè Thái hay sâm bổ lượng. Vị ngọt vừa phải, kết hợp nước cốt dừa béo nhẹ, tạo cảm giác thanh mát. Không gian thoải mái, phù hợp để thư giãn cùng bạn bè hoặc gia đình.",
                AudioScript = "Bạn đang đứng trước quán Chè Hà Trâm, đây là điểm dừng chân lý tưởng sau khi thưởng thức các món mặn. Quán có nhiều loại chè, từ truyền thống đến hiện đại như chè Thái hay sâm bổ lượng. Vị ngọt vừa phải, kết hợp nước cốt dừa béo nhẹ, tạo cảm giác thanh mát. Không gian thoải mái, phù hợp để thư giãn cùng bạn bè hoặc gia đình."
            },
            new Models.POITranslation
            {
                POIId = 7,
                LanguageCode = "en",
                Description = "A dessert shop famous for traditional sweet soups.",
                DetailedDescription = "You are now at Che Ha Tram, a perfect stop after enjoying savory street food. The shop offers many dessert soups ranging from traditional varieties to modern favorites like Thai dessert and herbal sweet soup. The sweetness is balanced with light coconut milk, creating a refreshing taste. The relaxed atmosphere makes it a pleasant place to unwind with friends or family.",
                AudioScript = "You are now at Che Ha Tram, a perfect stop after enjoying savory street food. The shop offers many dessert soups ranging from traditional varieties to modern favorites like Thai dessert and herbal sweet soup. The sweetness is balanced with light coconut milk, creating a refreshing taste. The relaxed atmosphere makes it a pleasant place to unwind with friends or family."
            },
        };
        await _database.InsertAllAsync(translations);

        var poiImages = new List<Models.POIImage>
        {
            // Ốc Oanh (ID 1)
            new Models.POIImage { POIId = 1, FileName = "oco.png" },
            new Models.POIImage { POIId = 1, FileName = "oco_1.png" },
            new Models.POIImage { POIId = 1, FileName = "oco_2.png" },

            // Sủi Cảo Vĩnh Khánh (ID 2)
            new Models.POIImage { POIId = 2, FileName = "sui_cao.png" },
            new Models.POIImage { POIId = 2, FileName = "sui_cao_1.png" },
            new Models.POIImage { POIId = 2, FileName = "sui_cao_2.png" },

            // Phá Lấu Cô Thảo (ID 3)
            new Models.POIImage { POIId = 3, FileName = "pha_lau.png" },
            new Models.POIImage { POIId = 3, FileName = "pha_lau_1.png" },
            new Models.POIImage { POIId = 3, FileName = "pha_lau_2.png" },

            // Bò Né Thanh Tuyền (ID 4)
            new Models.POIImage { POIId = 4, FileName = "bo_ne.png" },
            new Models.POIImage { POIId = 4, FileName = "bo_ne_1.png" },
            new Models.POIImage { POIId = 4, FileName = "bo_ne_2.png" },

            // Lẩu Bò Khu Nhà Cháy (ID 5)
            new Models.POIImage { POIId = 5, FileName = "lau.png" },
            new Models.POIImage { POIId = 5, FileName = "lau_1.png" },
            new Models.POIImage { POIId = 5, FileName = "lau_2.png" },

            // Ốc Đào (ID 6)
            new Models.POIImage { POIId = 6, FileName = "ocd_1.png" },
            new Models.POIImage { POIId = 6, FileName = "ocd_2.png" },
            new Models.POIImage { POIId = 6, FileName = "ocd_3.png" },

            // Chè Hà Trâm (ID 7)
            new Models.POIImage { POIId = 7, FileName = "che.png" },
            new Models.POIImage { POIId = 7, FileName = "che_1.png" },
            new Models.POIImage { POIId = 7, FileName = "che_2.png" },
        };
        await _database.InsertAllAsync(poiImages);
    }

    public async Task<List<POI>> GetAllPOIsAsync()
    {
        await Init();
        return await _database!.Table<POI>().ToListAsync();
    }

    public async Task<List<string>> GetPOIImagesAsync(int poiId)
    {
        // Đảm bảo Database đã được khởi tạo trước khi truy vấn
        await Init();

        // Truy vấn bảng POIImage để lấy tất cả ảnh có POIId trùng với ID quán đang xem
        var images = await _database!.Table<Models.POIImage>()
                                     .Where(x => x.POIId == poiId)
                                     .ToListAsync();

        // Chỉ trả về danh sách tên file (List<string>) để Member 1 nạp vào UI
        return images.Select(x => x.FileName).ToList();
    }

    public async Task<POITranslation?> GetTranslationAsync(int poiId, string languageCode)
    {
        await Init();

        var result = await _database!.Table<POITranslation>()
            .Where(t => t.POIId == poiId && t.LanguageCode == languageCode)
            .FirstOrDefaultAsync();

        // fallback về tiếng Việt
        if (result == null)
        {
            result = await _database.Table<POITranslation>()
                .Where(t => t.POIId == poiId && t.LanguageCode == "vi")
                .FirstOrDefaultAsync();
        }

        return result;
    }

    // API hỗ trợ Thành viên 2 và Thành viên 4
    public async Task<Models.POITranslation> GetPOIWithTranslationAsync(int poiId, string langCode)
    {
        await Init();
        // Ưu tiên tìm ngôn ngữ yêu cầu (langCode), nếu không có thì mặc định lấy Tiếng Việt (vi)
        var translation = await _database!.Table<Models.POITranslation>()
                                         .FirstOrDefaultAsync(x => x.POIId == poiId && x.LanguageCode == langCode);

        return translation ?? await _database.Table<Models.POITranslation>()
                                             .FirstOrDefaultAsync(x => x.POIId == poiId && x.LanguageCode == "vi");
    }

    // Thêm vào trong class DatabaseService
    public async Task<Models.POI?> GetPOIByQrCodeAsync(string qrCode)
    {
        await Init(); // Đảm bảo DB đã khởi tạo
        return await _database!.Table<Models.POI>()
                               .Where(p => p.QrCode == qrCode)
                               .FirstOrDefaultAsync();
    }
}