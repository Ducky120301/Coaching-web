@api
Feature: Import hồ sơ học viên từ file Excel câu trả lời Google Form
  # spec: docs/agentic-workflow/specs/2026-11-26-f02-import-excel.md
  Là coach của Duobexiuuu Coaching
  Tôi muốn đưa cả file Excel câu trả lời Google Form vào hệ thống một lần
  Để không phải gõ lại từng hồ sơ, và biết rõ dòng nào cần sửa để import lại

  Background:
    Given coach "Coach Duobexiuuu" đã đăng nhập

  @BR-f02-import-excel-012
  Rule: Chỉ coach đã đăng nhập mới import được hồ sơ

    @web
    Scenario: Coach import file hợp lệ thì hồ sơ được tạo
      Given một file Excel theo mẫu Google Form có 2 dòng học viên hợp lệ
      When coach import file đó
      Then 2 hồ sơ học viên mới được tạo, có đúng họ tên và email trong file

  @BR-f02-import-excel-013
  Rule: Hồ sơ tạo từ import thuộc coach đã import

    Scenario: Hồ sơ import nằm trong danh sách học viên của coach
      Given một file Excel có 1 dòng học viên hợp lệ với email "lan@gmail.com"
      When coach import file đó
      Then danh sách học viên của coach có hồ sơ "lan@gmail.com"

  @BR-f02-import-excel-014
  Rule: File import phải là file Excel .xlsx đọc được

    Scenario Outline: File không phải Excel .xlsx đọc được bị từ chối cả file
      Given một file <loai_file>
      When coach import file đó
      Then file bị từ chối vì không phải file Excel .xlsx đọc được
      And không hồ sơ nào được tạo

      Examples:
        | loai_file                 |
        | .csv                      |
        | Excel cũ .xls             |
        | .xlsx bị hỏng             |
        | .xlsx có đặt mật khẩu     |

  @BR-f02-import-excel-015
  Rule: File import tối đa 5 MB

    Scenario: File lớn hơn 5 MB bị từ chối cả file
      Given một file Excel nặng 6 MB
      When coach import file đó
      Then file bị từ chối vì vượt giới hạn 5 MB
      And không hồ sơ nào được tạo

  @BR-f02-import-excel-016
  Rule: File import có tối đa 500 dòng dữ liệu

    Scenario: File đúng 500 dòng được xử lý
      Given một file Excel có 500 dòng học viên hợp lệ
      When coach import file đó
      Then 500 hồ sơ học viên mới được tạo

    Scenario: File 501 dòng bị từ chối cả file
      Given một file Excel có 501 dòng học viên
      When coach import file đó
      Then file bị từ chối vì vượt giới hạn 500 dòng
      And không hồ sơ nào được tạo

  @BR-f02-import-excel-017
  Rule: Chỉ sheet đầu tiên của file được đọc

    Scenario: Dữ liệu ở sheet thứ hai không được import
      Given một file Excel có 1 dòng học viên ở sheet đầu và 1 dòng học viên khác ở sheet thứ hai
      When coach import file đó
      Then chỉ học viên ở sheet đầu được tạo hồ sơ

  @BR-f02-import-excel-018
  Rule: Cột được nhận ra theo tiêu đề tiếng Việt của câu hỏi, không phụ thuộc thứ tự cột

    Scenario: Đổi thứ tự cột vẫn đọc đúng
      Given một file Excel có cột "Họ Và Tên (Name)" đứng trước cột "Địa chỉ email" và 1 dòng học viên hợp lệ
      When coach import file đó
      Then hồ sơ được tạo với đúng họ tên và email của dòng đó

  @BR-f02-import-excel-019
  Rule: File thiếu cột email hoặc cột họ tên bị từ chối cả file

    Scenario: Thiếu cột không bắt buộc vẫn được xử lý
      Given một file Excel không có cột "Nghề nghiệp" và 1 dòng học viên hợp lệ
      When coach import file đó
      Then 1 hồ sơ học viên mới được tạo

    Scenario: Thiếu cột email thì từ chối cả file
      Given một file Excel không có cột "Địa chỉ email"
      When coach import file đó
      Then file bị từ chối vì thiếu cột "Địa chỉ email"
      And không hồ sơ nào được tạo

  @BR-f02-import-excel-020
  Rule: Cột không thuộc form hồ sơ bị bỏ qua

    Scenario: Cột Dấu thời gian không làm lỗi dòng
      Given một file Excel có cột "Dấu thời gian" và 1 dòng học viên hợp lệ
      When coach import file đó
      Then 1 hồ sơ học viên mới được tạo
      And kết quả import không có lỗi nào

  @BR-f02-import-excel-021
  Rule: File không có dòng dữ liệu nào thì không tạo hồ sơ và báo không có dòng dữ liệu

    Scenario: File chỉ có dòng tiêu đề
      Given một file Excel chỉ có dòng tiêu đề
      When coach import file đó
      Then coach được báo file không có dòng dữ liệu
      And không hồ sơ nào được tạo

  @BR-f02-import-excel-022
  Rule: Dòng hoàn toàn trống bị bỏ qua và không tính là lỗi

    Scenario: Dòng trống giữa hai dòng hợp lệ
      Given một file Excel có 2 dòng học viên hợp lệ và một dòng trống ở giữa
      When coach import file đó
      Then 2 hồ sơ học viên mới được tạo
      And kết quả import không có lỗi nào

  @BR-f02-import-excel-023
  Rule: Dòng có ô mang giá trị không hợp lệ theo luật của hồ sơ bị bỏ qua cả dòng, và mỗi ô sai được báo kèm số dòng Excel, tên cột và lý do

    Scenario: Dòng có tuổi ngoài khoảng bị bỏ qua, dòng khác vẫn được tạo
      Given một file Excel có dòng 2 hợp lệ và dòng 3 có "Tuổi (Age)" là "5"
      When coach import file đó
      Then chỉ học viên ở dòng 2 được tạo hồ sơ
      And kết quả import báo dòng 3, cột "Tuổi (Age)" phải từ 10 đến 100

    Scenario Outline: Ô có giá trị không hợp lệ làm bỏ cả dòng
      Given một file Excel có 1 dòng học viên với "<cot>" là "<gia_tri>"
      When coach import file đó
      Then không hồ sơ nào được tạo
      And kết quả import báo dòng đó, cột "<cot>", lý do "<ly_do>"

      Examples:
        | cot                 | gia_tri                | ly_do                          |
        | Đính kèm 4 ảnh      | drive.google.com/abc   | link ảnh không hợp lệ          |
        | Địa chỉ email       | abc                    | email không hợp lệ             |

    Scenario: Ngày chuyển khoản sau hôm nay làm bỏ cả dòng
      Given hôm nay là "26/11/2026"
      And một file Excel có 1 dòng học viên với "Ngày chuyển khoản" là "27/11/2026"
      When coach import file đó
      Then không hồ sơ nào được tạo
      And kết quả import báo dòng đó, cột "Ngày chuyển khoản", lý do "không được sau hôm nay"

    Scenario: Dòng có hai ô sai được báo cả hai ô
      Given một file Excel có 1 dòng học viên với "Tuổi (Age)" là "5" và "Chiều cao (Height)" là "300"
      When coach import file đó
      Then kết quả import báo dòng đó có 2 ô sai: "Tuổi (Age)" và "Chiều cao (Height)"

  @BR-f02-import-excel-024
  Rule: Dòng để trống email hoặc họ tên bị bỏ qua và báo ô thiếu

    Scenario: Dòng trống họ tên bị bỏ qua
      Given một file Excel có 1 dòng học viên có email nhưng để trống họ tên
      When coach import file đó
      Then không hồ sơ nào được tạo
      And kết quả import báo dòng đó thiếu "Họ Và Tên"

  @BR-f02-import-excel-025
  Rule: Dòng có email đã thuộc một hồ sơ khác hoặc tài khoản coach, không phân biệt hoa/thường và bỏ khoảng trắng đầu/cuối, bị bỏ qua và báo trùng; hồ sơ có sẵn không bị thay đổi

    Scenario Outline: Email đã có trong hệ thống bị bỏ qua
      Given email "quan@gmail.com" đã thuộc về <chu_so_huu>
      And một file Excel có 1 dòng học viên với email "<email_trong_file>"
      When coach import file đó
      Then không hồ sơ mới nào được tạo
      And kết quả import báo dòng đó trùng email đã có
      And thông tin của <chu_so_huu> không thay đổi

      Examples:
        | chu_so_huu           | email_trong_file    |
        | một hồ sơ học viên   | QUAN@gmail.com      |
        | một hồ sơ học viên   | " quan@gmail.com "  |
        | tài khoản coach      | quan@gmail.com      |

    Scenario: Import lại đúng file đã import không tạo thêm hồ sơ
      Given coach đã import thành công một file Excel có 3 dòng học viên
      When coach import lại đúng file đó
      Then không hồ sơ mới nào được tạo
      And kết quả import báo cả 3 dòng trùng email đã có

  @BR-f02-import-excel-026
  Rule: Khi nhiều dòng trong cùng file có cùng email, chỉ dòng xuất hiện trước được xét; các dòng sau bị bỏ qua và báo trùng với dòng trước

    Scenario: Hai dòng cùng email trong một file
      Given một file Excel có dòng 3 và dòng 7 cùng email "lan@gmail.com", khác hoa/thường
      When coach import file đó
      Then chỉ một hồ sơ "lan@gmail.com" được tạo, theo dòng 3
      And kết quả import báo dòng 7 trùng email với dòng 3

  @BR-f02-import-excel-027
  Rule: Dòng có email và họ tên hợp lệ, các ô có giá trị đều hợp lệ, được tạo hồ sơ dù còn để trống mục bắt buộc khác

    Scenario: Dòng chỉ có email và họ tên vẫn được tạo
      Given một file Excel có 1 dòng học viên chỉ có email và họ tên
      When coach import file đó
      Then 1 hồ sơ học viên mới được tạo
      And hồ sơ đó cho biết còn thiếu các mục bắt buộc

  @BR-f02-import-excel-028
  Rule: Hồ sơ tạo từ import có tài khoản Chờ kích hoạt và coaching Đang coaching

    Scenario: Trạng thái của hồ sơ import
      Given một file Excel có 1 dòng học viên hợp lệ
      When coach import file đó
      Then hồ sơ vừa tạo có tài khoản Chờ kích hoạt và coaching Đang coaching

  @BR-f02-import-excel-029
  Rule: Import không gửi email nào cho học viên

    Scenario: Học viên được import không nhận email
      Given một file Excel có 1 dòng học viên hợp lệ với email "lan@gmail.com"
      When coach import file đó
      Then không email nào được gửi tới "lan@gmail.com"

  @BR-f02-import-excel-030
  Rule: Giá trị của mục có danh sách cho trước được đưa về đúng giá trị trong danh sách khi chỉ khác chữ hoa/thường, khoảng trắng, kiểu dấu gạch hoặc dấu "/", hoặc khi là một cách viết đã biết của giá trị đó; giá trị không đưa về được là ô không hợp lệ

    Scenario Outline: Giá trị lệch cách viết được đưa về giá trị chuẩn
      Given một file Excel có 1 dòng học viên với "<cot>" là "<gia_tri_trong_file>"
      When coach import file đó
      Then hồ sơ vừa tạo ghi nhận "<muc>" là "<gia_tri_chuan>"

      Examples:
        | cot                         | gia_tri_trong_file          | muc                           | gia_tri_chuan                 |
        | Mức độ hoạt động            | Ít vận động (Ngồi nhiều)    | Mức độ hoạt động              | Ít vận động (ngồi nhiều)      |
        | Thời gian ngủ trong 1 ngày  | 5-6 tiếng                   | Thời gian ngủ mỗi ngày        | 5–6 tiếng                     |
        | Bạn mua Pack nào?           | pack 1                      | Gói đã mua                    | Pack 1                        |
        | Phong cách giáo án          | PUSH PULL LEG               | Phong cách giáo án muốn theo  | Push / Pull / Legs            |

    Scenario: Giá trị không thuộc danh sách là ô không hợp lệ
      Given một file Excel có 1 dòng học viên với "Mức độ căng thẳng" là "Cao quá"
      When coach import file đó
      Then không hồ sơ nào được tạo
      And kết quả import báo dòng đó, cột "Mức độ căng thẳng" không hợp lệ

  @BR-f02-import-excel-031
  Rule: Ngày chuyển khoản trong file được nhận khi là ô ngày của Excel hoặc chữ dạng ngày/tháng/năm (dd/MM/yyyy)

    Scenario Outline: Ngày chuyển khoản đúng dạng được nhận
      Given một file Excel có 1 dòng học viên với ngày chuyển khoản là <ngay_trong_file>
      When coach import file đó
      Then hồ sơ vừa tạo có ngày chuyển khoản "<ngay>"

      Examples:
        | ngay_trong_file               | ngay        |
        | ô ngày Excel 12/11/2026       | 12/11/2026  |
        | chữ "25/11/2026"              | 25/11/2026  |

    Scenario Outline: Ngày chuyển khoản sai dạng là ô không hợp lệ
      Given một file Excel có 1 dòng học viên với "Ngày chuyển khoản" là "<gia_tri>"
      When coach import file đó
      Then kết quả import báo dòng đó, cột "Ngày chuyển khoản" không hợp lệ

      Examples:
        | gia_tri     |
        | 11/25/2026  |
        | hôm qua     |

  @BR-f02-import-excel-032
  Rule: Giá trong file được nhận khi là ô số nguyên hoặc chữ gồm số với dấu chấm phân cách nghìn, có thể kèm đuôi "đ" hoặc "VNĐ"

    Scenario Outline: Giá đúng dạng được nhận
      Given một file Excel có 1 dòng học viên với giá là <gia_trong_file>
      When coach import file đó
      Then hồ sơ vừa tạo có giá 10.000.000 đồng

      Examples:
        | gia_trong_file      |
        | ô số 10000000       |
        | chữ "10.000.000đ"   |

    Scenario Outline: Giá sai dạng là ô không hợp lệ
      Given một file Excel có 1 dòng học viên với "Giá gói bạn mua" là "<gia_tri>"
      When coach import file đó
      Then kết quả import báo dòng đó, cột "Giá gói bạn mua" không hợp lệ

      Examples:
        | gia_tri      |
        | 10tr         |
        | 10000000,5   |
        | -100         |

  @BR-f02-import-excel-033
  Rule: Mỗi dòng được xử lý độc lập; hồ sơ đã tạo được giữ lại dù dòng sau bị bỏ qua hoặc quá trình dừng giữa chừng

    Scenario: Dòng lỗi không làm mất các dòng hợp lệ
      Given một file Excel có 10 dòng học viên, trong đó 8 dòng hợp lệ
      When coach import file đó
      Then 8 hồ sơ học viên mới được tạo

  @BR-f02-import-excel-034
  Rule: Kết quả import cho coach biết số dòng đã tạo, số dòng bị bỏ qua và từng lý do kèm số dòng Excel

    @web
    Scenario: Coach thấy kết quả import
      Given một file Excel có 5 dòng học viên: 3 dòng hợp lệ, 1 dòng trùng email đã có, 1 dòng tuổi sai
      When coach import file đó
      Then coach thấy đã tạo 3, bỏ qua 2
      And coach thấy lý do của từng dòng bị bỏ qua kèm số dòng Excel
