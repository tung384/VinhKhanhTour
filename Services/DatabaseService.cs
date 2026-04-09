using OneSProject.Models;
using Microsoft.Maui.Storage;
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

        // Khởi tạo tất cả các bảng
        await _database.CreateTableAsync<POI>();
        await _database.CreateTableAsync<POIImage>();
        await _database.CreateTableAsync<POITranslation>();
        await _database.CreateTableAsync<RecentHistory>(); // Bảng mới cho Phase 6.1

        await SeedDataAsync();
    }

    // --- LOGIC CHO PHASE 6.1: RECENT POIS ---

    public async Task AddPOIToHistoryAsync(int poiId)
    {
        await Init();

        // Kiểm tra nếu đã tồn tại trong lịch sử thì xóa cũ để đẩy lên đầu
        var existing = await _database!.Table<RecentHistory>().FirstOrDefaultAsync(x => x.POIId == poiId);
        if (existing != null) await _database.DeleteAsync(existing);

        // Thêm bản ghi mới
        await _database.InsertAsync(new RecentHistory { POIId = poiId, VisitedDate = DateTime.Now });

        // Giới hạn tối đa 5 bản ghi (Xóa các bản ghi cũ nhất)
        var history = await _database.Table<RecentHistory>().OrderByDescending(x => x.VisitedDate).ToListAsync();
        if (history.Count > 5)
        {
            for (int i = 5; i < history.Count; i++)
            {
                await _database.DeleteAsync(history[i]);
            }
        }
    }

    public async Task<List<POI>> GetRecentPOIsAsync()
    {
        await Init();
        var history = await _database!.Table<RecentHistory>()
                                     .OrderByDescending(x => x.VisitedDate)
                                     .ToListAsync();

        var recentIds = history.Select(x => x.POIId).ToList();
        var allPOIs = await _database.Table<POI>().ToListAsync();

        // Trả về danh sách POI theo đúng thứ tự thời gian trong lịch sử
        return allPOIs.Where(p => recentIds.Contains(p.Id))
                      .OrderBy(p => recentIds.IndexOf(p.Id))
                      .ToList();
    }

    // --- LOGIC TRUY VẤN ĐA NGÔN NGỮ ---

    public async Task<POITranslation> GetPOIWithTranslationAsync(int poiId, string? langCode = null)
    {
        await Init();

        // Nếu langCode không được truyền vào, lấy từ Preferences (do Settings set)
        string targetLang = langCode ?? Preferences.Get("SelectedLanguage", "vi");

        var translation = await _database!.Table<POITranslation>()
            .FirstOrDefaultAsync(x => x.POIId == poiId && x.LanguageCode == targetLang);

        // Fallback logic: Tìm tiếng Việt nếu không có ngôn ngữ yêu cầu
        if (translation == null && targetLang != "vi")
        {
            translation = await _database.Table<POITranslation>()
                .FirstOrDefaultAsync(x => x.POIId == poiId && x.LanguageCode == "vi");
        }

        return translation!;
    }

    public async Task<POI?> GetPOIByQrCodeAsync(string qrCode)
    {
        await Init();
        var poi = await _database!.Table<POI>().FirstOrDefaultAsync(x => x.QrCode == qrCode);
        if (poi != null)
        {
            await AddPOIToHistoryAsync(poi.Id); // Tự động lưu vào lịch sử khi quét QR
        }
        return poi;
    }

    // --- CÁC API KHÁC ---
    public async Task<List<POI>> GetAllPOIsAsync()
    {
        await Init();
        return await _database!.Table<POI>().ToListAsync();
    }

    public async Task<List<string>> GetPOIImagesAsync(int poiId)
    {
        await Init();
        var images = await _database!.Table<POIImage>().Where(x => x.POIId == poiId).ToListAsync();
        return images.Select(x => x.FileName).ToList();
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
                AudioScript = "Đây là một trong những quán ốc nổi tiếng với cách chế biến sạch sẽ, gia vị đậm đà. Các món như ốc xào tỏi hay rang muối đều thơm lừng, hấp dẫn. Nước chấm mắm gừng sánh nhẹ, chua ngọt vừa phải, giúp tăng hương vị hải sản. Dù quán khá đông nhưng vẫn gọn gàng, tạo cảm giác dễ chịu.",
            },
            new Models.POITranslation
            {
                POIId = 1,
                LanguageCode = "en",
                Description = "The busiest snail spot on the street.",
                DetailedDescription = "This is one of the most famous snail restaurants on the street, known for its clean preparation and bold seasoning. Dishes like garlic stir-fried snails or salt-roasted snails are fragrant and very tempting. The ginger fish sauce dip has a light thickness with a balanced sweet-and-sour taste that enhances the seafood flavor. Although the place is often crowded, it remains tidy and comfortable.",
                AudioScript = "This is one of the most famous snail restaurants on the street, known for its clean preparation and bold seasoning. Dishes like garlic stir-fried snails or salt-roasted snails are fragrant and very tempting. The ginger fish sauce dip has a light thickness with a balanced sweet-and-sour taste that enhances the seafood flavor. Although the place is often crowded, it remains tidy and comfortable."
            },

            new Models.POITranslation
            {
                POIId = 1,
                LanguageCode = "ko",
                Description = "이 거리에서 가장 붐비는 달팽이 요리 전문점.",
                DetailedDescription = "이곳은 깔끔한 조리법과 풍부하고 맛있는 양념으로 유명한 달팽이 요리 전문점 중 하나입니다. 마늘 달팽이 볶음이나 소금구이 달팽이 같은 요리는 향긋하고 먹음직스럽습니다. 생강 피시 소스는 묽으면서도 진하고, 새콤달콤한 맛의 완벽한 균형을 이루어 해산물의 풍미를 더욱 살려줍니다. 식당은 꽤 붐비지만 깔끔하고 쾌적한 분위기를 유지합니다.",
                AudioScript = "이곳은 깔끔한 조리법과 풍부하고 맛있는 양념으로 유명한 달팽이 요리 전문점 중 하나입니다. 마늘 달팽이 볶음이나 소금구이 달팽이 같은 요리는 향긋하고 먹음직스럽습니다. 생강 피시 소스는 묽으면서도 진하고, 새콤달콤한 맛의 완벽한 균형을 이루어 해산물의 풍미를 더욱 살려줍니다. 식당은 꽤 붐비지만 깔끔하고 쾌적한 분위기를 유지합니다."
            },

            new Models.POITranslation
            {
                POIId = 1,
                LanguageCode = "fr",
                Description = "Le stand d'escargots le plus fréquenté de la rue.",
                DetailedDescription = "C'est l'un des restaurants d'escargots les plus réputés, reconnu pour la propreté de sa préparation et la richesse de ses assaisonnements. Les escargots sautés à l'ail ou les escargots rôtis au sel, par exemple, sont parfumés et appétissants. La sauce nuoc-mâm au gingembre, légère et onctueuse, offre un parfait équilibre entre le sucré et l'acidulé, sublimant ainsi la saveur des fruits de mer. Malgré son affluence, le restaurant reste propre et agréable.",
                AudioScript = "C'est l'un des restaurants d'escargots les plus réputés, reconnu pour la propreté de sa préparation et la richesse de ses assaisonnements. Les escargots sautés à l'ail ou les escargots rôtis au sel, par exemple, sont parfumés et appétissants. La sauce nuoc-mâm au gingembre, légère et onctueuse, offre un parfait équilibre entre le sucré et l'acidulé, sublimant ainsi la saveur des fruits de mer. Malgré son affluence, le restaurant reste propre et agréable."
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

            new Models.POITranslation
            {
                POIId = 2,
                LanguageCode = "ko",
                Description = "정통 중국식 만두.",
                DetailedDescription = "이곳은 중국 음식을 좋아하는 사람들에게는 친숙한 곳입니다. 만두는 얇고 살짝 쫄깃한 피 속에 신선한 새우와 고기가 완벽하게 어우러져 있습니다. 육수는 뼈를 끓여 은은하고 달콤한 맛을 내며, 말린 오징어와 푸른 채소가 어우러져 조화로운 맛을 선사합니다. 편안하고 친근한 분위기에 빠른 서비스까지 더해져 따뜻하고 만족스러운 저녁 식사를 즐기기에 안성맞춤입니다.",
                AudioScript = "이곳은 중국 음식을 좋아하는 사람들에게는 친숙한 곳입니다. 만두는 얇고 살짝 쫄깃한 피 속에 신선한 새우와 고기가 완벽하게 어우러져 있습니다. 육수는 뼈를 끓여 은은하고 달콤한 맛을 내며, 말린 오징어와 푸른 채소가 어우러져 조화로운 맛을 선사합니다. 편안하고 친근한 분위기에 빠른 서비스까지 더해져 따뜻하고 만족스러운 저녁 식사를 즐기기에 안성맞춤입니다."
            },

            new Models.POITranslation
            {
                POIId = 2,
                LanguageCode = "fr",
                Description = "Authentiques raviolis chinois.",
                DetailedDescription = "C'est une adresse incontournable pour les amateurs de cuisine chinoise. Les raviolis, à la pâte fine et légèrement élastique, renferment à merveille une farce de crevettes et de viande fraîches. Le bouillon, mijoté avec des arêtes, possède une saveur douce et légère, harmonieusement complétée par des calamars séchés et des légumes verts. L'atmosphère y est décontractée et conviviale, et le service rapide, ce qui en fait l'endroit idéal pour un dîner chaleureux et savoureux.",
                AudioScript = "C'est une adresse incontournable pour les amateurs de cuisine chinoise. Les raviolis, à la pâte fine et légèrement élastique, renferment à merveille une farce de crevettes et de viande fraîches. Le bouillon, mijoté avec des arêtes, possède une saveur douce et légère, harmonieusement complétée par des calamars séchés et des légumes verts. L'atmosphère y est décontractée et conviviale, et le service rapide, ce qui en fait l'endroit idéal pour un dîner chaleureux et savoureux."
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

            new Models.POITranslation
            {
                POIId = 3,
                LanguageCode = "ko",
                Description = "풍미 가득한 유명한 내장찜 전문점.",
                DetailedDescription = "이곳의 내장찜은 진한 코코넛 밀크 육수와 눈길을 사로잡는 색감으로 유명합니다. 소 내장은 깨끗하게 손질하여 부드러우면서도 쫄깃한 식감을 살렸습니다. 빵을 소스에 찍어 먹고 라임 피시소스를 살짝 곁들이면 더욱 풍미가 살아납니다. 작은 가게지만 독특한 맛 덕분에 언제나 손님들로 북적입니다.",
                AudioScript = "이곳의 내장찜은 진한 코코넛 밀크 육수와 눈길을 사로잡는 색감으로 유명합니다. 소 내장은 깨끗하게 손질하여 부드러우면서도 쫄깃한 식감을 살렸습니다. 빵을 소스에 찍어 먹고 라임 피시소스를 살짝 곁들이면 더욱 풍미가 살아납니다. 작은 가게지만 독특한 맛 덕분에 언제나 손님들로 북적입니다."
            },

            new Models.POITranslation
            {
                POIId = 3,
                LanguageCode = "fr",
                Description = "Un stand réputé pour ses abats braisés savoureux.",
                DetailedDescription = "Ici, les abats braisés se distinguent par leur riche bouillon au lait de coco et leur couleur appétissante. Les abats de bœuf sont soigneusement nettoyés puis braisés jusqu'à tendreté, tout en conservant une texture légèrement ferme. Pour une dégustation optimale, trempez du pain dans la sauce et ajoutez un filet de sauce nuoc-mâm au citron vert. Ce petit restaurant est toujours bondé grâce à sa saveur unique.",
                AudioScript = "Ici, les abats braisés se distinguent par leur riche bouillon au lait de coco et leur couleur appétissante. Les abats de bœuf sont soigneusement nettoyés puis braisés jusqu'à tendreté, tout en conservant une texture légèrement ferme. Pour une dégustation optimale, trempez du pain dans la sauce et ajoutez un filet de sauce nuoc-mâm au citron vert. Ce petit restaurant est toujours bondé grâce à sa saveur unique."
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
            new Models.POITranslation
            {
                POIId = 4,
                LanguageCode = "ko",
                Description = "지글지글 끓는 소고기 스테이크로 유명한 곳.",
                DetailedDescription = "오랜 역사를 자랑하는 소고기 국수 전문점으로, 뜨거운 김이 모락모락 나는 무쇠 팬에 담아 나오는 국수가 일품입니다. 소고기는 풍부한 양념에 재워 계란 프라이, 크리미한 파테, 향긋한 버터와 함께 제공됩니다. 빵은 언제나 바삭해서 특제 소스에 찍어 먹기에 제격입니다. 든든하고 만족스러운 한 끼 식사로 제격입니다.",
                AudioScript = "오랜 역사를 자랑하는 소고기 국수 전문점으로, 뜨거운 김이 모락모락 나는 무쇠 팬에 담아 나오는 국수가 일품입니다. 소고기는 풍부한 양념에 재워 계란 프라이, 크리미한 파테, 향긋한 버터와 함께 제공됩니다. 빵은 언제나 바삭해서 특제 소스에 찍어 먹기에 제격입니다. 든든하고 만족스러운 한 끼 식사로 제격입니다."
            },
            new Models.POITranslation
            {
                POIId = 4,
                LanguageCode = "fr",
                Description = "Un stand de steak de bœuf grillé très populaire.",
                DetailedDescription = "Ce restaurant de soupes de nouilles au bœuf est une institution, réputé pour ses plats servis fumants dans une poêle en fonte. Le bœuf, mariné dans des saveurs riches, est servi avec des œufs au plat, un pâté onctueux et une touche de beurre parfumé. Le pain, toujours croustillant, est parfait pour tremper dans la sauce signature du restaurant. Un choix idéal pour un repas copieux et revigorant.",
                AudioScript = "Ce restaurant de soupes de nouilles au bœuf est une institution, réputé pour ses plats servis fumants dans une poêle en fonte. Le bœuf, mariné dans des saveurs riches, est servi avec des œufs au plat, un pâté onctueux et une touche de beurre parfumé. Le pain, toujours croustillant, est parfait pour tremper dans la sauce signature du restaurant. Un choix idéal pour un repas copieux et revigorant."
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
            new Models.POITranslation
            {
                POIId = 5,
                LanguageCode = "ko",
                Description = "현지에서 잘 알려진 소고기 훠궈 전문점.",
                DetailedDescription = "여기는 '라우 보 쿠 냐 차이(소고기 전골집)'입니다. 뼈로 만든 달콤짭짤한 육수에 은은한 허브 향이 더해져 일품인 곳이죠. 소고기, 힘줄, 소꼬리를 부드러워질 때까지 푹 끓여 진하고 크리미하면서도 적당히 매콤한 두부 소스를 곁들여 제공합니다. 신선한 채소가 맛의 균형을 잡아줍니다. 넓은 공간은 저녁 시간에 친구들과 모임을 갖기에 안성맞춤입니다.",
                AudioScript = "여기는 '라우 보 쿠 냐 차이(소고기 전골집)'입니다. 뼈로 만든 달콤짭짤한 육수에 은은한 허브 향이 더해져 일품인 곳이죠. 소고기, 힘줄, 소꼬리를 부드러워질 때까지 푹 끓여 진하고 크리미하면서도 적당히 매콤한 두부 소스를 곁들여 제공합니다. 신선한 채소가 맛의 균형을 잡아줍니다. 넓은 공간은 저녁 시간에 친구들과 모임을 갖기에 안성맞춤입니다."
            },
            new Models.POITranslation
            {
                POIId = 5,
                LanguageCode = "fr",
                Description = "Un restaurant de fondue chinoise réputé du coin.",
                DetailedDescription = "Vous êtes chez Lẩu Bò Khu Nhà Cháy (Fondue de bœuf du quartier de la Maison Brûlée), un restaurant réputé pour son bouillon aigre-doux à base d'os, subtilement parfumé aux herbes. Le bœuf, les tendons et la queue de bœuf mijotent jusqu'à tendreté, puis sont servis avec une sauce onctueuse, légèrement crémeuse et moyennement épicée au tofu fermenté. Les légumes, toujours frais, équilibrent parfaitement les saveurs. L'atmosphère spacieuse est idéale pour se retrouver entre amis en soirée.",
                AudioScript = "Vous êtes chez Lẩu Bò Khu Nhà Cháy (Fondue de bœuf du quartier de la Maison Brûlée), un restaurant réputé pour son bouillon aigre-doux à base d'os, subtilement parfumé aux herbes. Le bœuf, les tendons et la queue de bœuf mijotent jusqu'à tendreté, puis sont servis avec une sauce onctueuse, légèrement crémeuse et moyennement épicée au tofu fermenté. Les légumes, toujours frais, équilibrent parfaitement les saveurs. L'atmosphère spacieuse est idéale pour se retrouver entre amis en soirée."
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
            new Models.POITranslation
            {
                POIId = 6,
                LanguageCode = "ko",
                Description = "다양한 달팽이 요리를 제공하는 유명한 해산물 전문점.",
                DetailedDescription = "이 식당은 활기 넘치는 길거리 음식 분위기를 자랑합니다. 성게알 파 구이, 가리비 구이 등 구이 요리가 특히 인기가 많습니다. 강렬하면서도 살짝 매콤한 양념이 미각을 자극합니다. 매일 저녁 젊은이들의 만남의 장소로, 언제나 활기차고 북적입니다.",
                AudioScript = "이 식당은 활기 넘치는 길거리 음식 분위기를 자랑합니다. 성게알 파 구이, 가리비 구이 등 구이 요리가 특히 인기가 많습니다. 강렬하면서도 살짝 매콤한 양념이 미각을 자극합니다. 매일 저녁 젊은이들의 만남의 장소로, 언제나 활기차고 북적입니다."
            },
            new Models.POITranslation
            {
                POIId = 6,
                LanguageCode = "fr",
                Description = "Un stand de fruits de mer réputé pour ses nombreux plats d'escargots.",
                DetailedDescription = "Ce restaurant offre une ambiance de street food animée. Les grillades, comme l'oursin aux oignons verts et les pétoncles grillés, sont très appréciées. L'assaisonnement, relevé et légèrement piquant, éveille les papilles. C'est un lieu de rencontre prisé des jeunes chaque soir, toujours vivant et animé.",
                AudioScript = "Ce restaurant offre une ambiance de street food animée. Les grillades, comme l'oursin aux oignons verts et les pétoncles grillés, sont très appréciées. L'assaisonnement, relevé et légèrement piquant, éveille les papilles. C'est un lieu de rencontre prisé des jeunes chaque soir, toujours vivant et animé."
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
            new Models.POITranslation
            {
                POIId = 7,
                LanguageCode = "ko",
                Description = "전통 달콤한 수프로 유명한 디저트 가게.",
                DetailedDescription = "하 트람 디저트 가게 앞에 서 계시네요. 맛있는 음식을 즐긴 후 들르기 딱 좋은 곳이죠. 태국 전통 디저트부터 허브 토닉까지, 다양한 종류의 디저트를 판매하고 있어요. 적당한 단맛과 가볍고 크리미한 코코넛 밀크의 조화가 상쾌한 맛을 선사합니다. 편안한 분위기에서 친구나 가족과 함께 여유로운 시간을 보내실 수 있어요.",
                AudioScript = "하 트람 디저트 가게 앞에 서 계시네요. 맛있는 음식을 즐긴 후 들르기 딱 좋은 곳이죠. 태국 전통 디저트부터 허브 토닉까지, 다양한 종류의 디저트를 판매하고 있어요. 적당한 단맛과 가볍고 크리미한 코코넛 밀크의 조화가 상쾌한 맛을 선사합니다. 편안한 분위기에서 친구나 가족과 함께 여유로운 시간을 보내실 수 있어요."
            },
            new Models.POITranslation
            {
                POIId = 7,
                LanguageCode = "fr",
                Description = "Une pâtisserie célèbre pour ses soupes sucrées traditionnelles.",
                DetailedDescription = "Vous vous trouvez devant la pâtisserie Ha Tram, l'endroit idéal pour une pause gourmande après un bon repas. La boutique propose une grande variété de desserts, des plus traditionnels aux plus modernes, comme des desserts thaïlandais ou une infusion tonique. Le juste équilibre de douceur, associé à la légèreté et à l'onctuosité du lait de coco, crée une sensation de fraîcheur incomparable. L'atmosphère chaleureuse est parfaite pour se détendre entre amis ou en famille.",
                AudioScript = "Vous vous trouvez devant la pâtisserie Ha Tram, l'endroit idéal pour une pause gourmande après un bon repas. La boutique propose une grande variété de desserts, des plus traditionnels aux plus modernes, comme des desserts thaïlandais ou une infusion tonique. Le juste équilibre de douceur, associé à la légèreté et à l'onctuosité du lait de coco, crée une sensation de fraîcheur incomparable. L'atmosphère chaleureuse est parfaite pour se détendre entre amis ou en famille."
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
}