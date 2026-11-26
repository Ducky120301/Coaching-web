using Coaching.Application.Students.Import;

namespace Coaching.Application.Tests.Students.Import;

public sealed class StudentImportColumnsTests
{
    // @ADR-f02-import-excel-005 @BR-f02-import-excel-018: tiêu đề thật của file mẫu Google Form (và tiêu đề trong
    // living doc) → mã mục; so tiền tố của dòng đầu đã chuẩn hoá, không phụ thuộc thứ tự cột
    [Theory]
    [InlineData("Địa chỉ email", "email")]
    [InlineData("Họ Và Tên (Name)", "fullName")]
    [InlineData("Địa chỉ (Address)", "address")]
    [InlineData("Tuổi (Age)", "age")]
    [InlineData("Chiều cao (Height)", "heightCm")]
    [InlineData("Cân nặng (Weight)", "weightKg")]
    [InlineData("Số đo Vòng 1\n\nChest measurement", "chestCm")]
    [InlineData("Số đo Vòng 2\n\nWaist measurement", "bellyCm")]
    [InlineData("Số đo vòng Eo (trên rốn 4cm)\n\nWaist circumference (4 cm above the navel)", "waistCm")]
    [InlineData("Số đo vòng 3\n\nHip measurement", "hipCm")]
    [InlineData("Số đo tay phải (khi đang gồng Biceps)\n\nRight Arm Circumference", "rightArmCm")]
    [InlineData("Số đo tay trái (khi đang gồng Biceps)\n\nLeft Arm Circumference", "leftArmCm")]
    [InlineData("Số đo đùi phải (đo ở vị trí to nhất)\n\nRight Thigh Circumference", "rightThighCm")]
    [InlineData("Số đo đùi trái (đo ở vị trí to nhất)\n\nLeft Thigh Circumference", "leftThighCm")]
    [InlineData("Mỡ trong cơ thể (%) + cách thức đo \n\n% bodyfat and measurement method", "bodyFatAndMethod")]
    [InlineData("Số năm tập luyện\n\nTraining time", "trainingYears")]
    [InlineData("Mục tiêu Tập luyện của bạn là gì? \n\nTraining target", "trainingGoal")]
    [InlineData("Nếu giả sử có một chương trình tập hiệu quả, bạn có thể dành ra bao nhiêu thời gian trong tuần cho việc luyện tập? \n\nIf provided with an effective training program", "weeklyTrainingTime")]
    [InlineData("Cân nặng của bạn trước khi tập tạ là bao nhiêu và chiều cao của bạn khi đó là bao nhiêu? \n\nWhat was your weight", "weightHeightBeforeLifting")]
    [InlineData("Trình bày chế độ tập luyện của bạn hiện tại (khuyến khích chi tiết):\n\nPlease describe", "currentTraining")]
    [InlineData("Bạn có tiền sử bệnh lý và chấn thương nào không? \n\nHave you experienced", "medicalHistory")]
    [InlineData("CÂU HỎI QUAN TRỌNG: Bạn có sẵn sàng dành ra một khoản tiền kha khá để đầu tư vào những đồ ăn chất lượng như cá\n\nMPORTANT QUESTION", "foodBudgetReadiness")]
    [InlineData("Nghề nghiệp của bạn là gì? (Mục tiêu của câu hỏi này là để tìm hiểu về đồng hồ sinh học):\n\nWhat do you do for work?", "occupation")]
    [InlineData("Mức độ hoạt động\n\nActivity Level", "activityLevel")]
    [InlineData("Mức độ căng thẳng\n\nStress level", "stressLevel")]
    [InlineData("Thời gian ngủ trong 1 ngày:\n\nHow many hours of sleep do you get per day?:", "sleepDuration")]
    [InlineData("Hàng ngày bạn sử dụng bao nhiêu caffeine hoặc trong những ngày học/làm việc bình thường? \n\nHow much caffeine", "caffeine")]
    [InlineData("Nêu tên ra các loại thực phẩm hỗ trợ mà bạn đang sử dụng hiện tại? \n\nPlease list any supplements", "supplements")]
    [InlineData("Trình bày chế độ dinh dưỡng của bạn hiện tại? (khuyến khích chi tiết) \n\nPlease describe", "currentDiet")]
    [InlineData("Bạn sẽ tập được bao nhiêu buổi/ tuần? \n\nHow many days per week can you train?", "sessionsPerWeek")]
    [InlineData("Bạn muốn giáo án tập của bạn được thiết thế theo phong cách nào? \n\nWhat type of training style", "programStyle")]
    [InlineData("Đính kèm 4 ảnh toàn thân mặt truớc, mặt sau và 2 mặt bên với ánh sáng tốt. (Chụp toàn thân, cam để ngang hông) \n\nAttach 4 full-body photos", "bodyPhotoUrl")]
    [InlineData("Bạn mong đợi điều gì từ coach? (What are your expectations from your coach?)", "expectations")]
    [InlineData("Bạn từng theo coach/online coaching trước chưa? Trải nghiệm ra sao? (Have you ever had a coach)", "previousCoaching")]
    [InlineData("Điều gì khiến bạn quyết định đầu tư vào bản thân lúc này? (What inspired you)", "investmentReason")]
    [InlineData("Thời gian bạn có thể ăn các bữa và thời gian đi tập để mình lên mealplan cho chuẩn nha. (khuyến khích chi tiết) \n\nYour available meal times", "mealAndTrainingTimes")]
    [InlineData("Bạn mua Pack nào?", "pack")]
    [InlineData("Giá gói bạn mua", "price")]
    [InlineData("Ngày chuyển khoản", "transferDate")]
    [InlineData("Phong cách giáo án", "programStyle")]
    [InlineData("Đính kèm 4 ảnh", "bodyPhotoUrl")]
    [InlineData("Thời gian ngủ trong 1 ngày", "sleepDuration")]
    [InlineData("Nghề nghiệp", "occupation")]
    public void Form_question_header_maps_to_field(string header, string field) =>
        Assert.Equal(field, StudentImportColumns.FieldFor(header));

    // @BR-f02-import-excel-020: cột không thuộc form hồ sơ bị bỏ qua (kể cả cột gộp Gói + giá + ngày của file mẫu)
    [Theory]
    [InlineData("Dấu thời gian")]
    [InlineData("Cột 36")]
    [InlineData("Bạn mua Pack nào, giá bao nhiêu và ngày chuyển khoản")]
    [InlineData("")]
    public void Header_outside_registration_form_is_ignored(string header) =>
        Assert.Null(StudentImportColumns.FieldFor(header));

    // @BR-f02-import-excel-018: tiêu đề song ngữ, khác hoa/thường, thừa khoảng trắng vẫn khớp
    [Theory]
    [InlineData("Số đo Vòng 1 / Chest measurement", "chestCm")]
    [InlineData("  HỌ VÀ   TÊN  ", "fullName")]
    [InlineData("ĐỊA CHỈ EMAIL\nEmail address", "email")]
    public void Bilingual_or_differently_spaced_header_still_matches(string header, string field) =>
        Assert.Equal(field, StudentImportColumns.FieldFor(header));

    // @ADR-f02-import-excel-005
    [Fact]
    public void Normalize_keeps_first_line_strips_diacritics_and_collapses_spaces()
    {
        Assert.Equal("so do vong eo (tren ron 4cm)",
            StudentImportColumns.NormalizeHeader("Số đo vòng  Eo (trên rốn 4cm)\r\n\r\nWaist circumference"));
        Assert.Equal("dia chi email", StudentImportColumns.NormalizeHeader("  Địa chỉ email "));
        Assert.Equal("Số đo vòng 3", StudentImportColumns.DisplayHeader(" Số đo vòng 3\n\nHip measurement"));
    }
}
