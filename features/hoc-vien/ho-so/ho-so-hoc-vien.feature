@api
Feature: Hồ sơ học viên và trạng thái coaching
  # spec: docs/agentic-workflow/specs/2026-11-25-f02-ho-so-hoc-vien.md
  # design: docs/duobexiuuu-coaching-design.html (AdTaoHoSo, AdHoSo)
  Là coach của Duobexiuuu Coaching
  Tôi muốn mỗi học viên có một hồ sơ duy nhất theo form đăng ký, có ghi chú riêng và trạng thái coaching có lịch sử
  Để mọi việc soạn phase, review và theo dõi tiến độ đều bắt đầu từ đúng một nơi

  Background:
    Given coach "Coach Duobexiuuu" đã đăng nhập

  @BR-f02-ho-so-hoc-vien-001
  Rule: Email học viên là duy nhất trong toàn hệ thống; hai email chỉ khác chữ hoa/thường hoặc khoảng trắng đầu/cuối là cùng một email

    Scenario: Email có chữ hoa và khoảng trắng được ghi nhận ở dạng chuẩn
      Given chưa có hồ sơ nào dùng email "an@mail.com"
      When coach tạo hồ sơ học viên với email " An@Mail.com "
      Then hồ sơ được tạo với email "an@mail.com"

    Scenario Outline: Email đã thuộc về người khác thì không tạo được hồ sơ
      Given email "an@mail.com" đã thuộc về <chu_so_huu>
      When coach tạo hồ sơ học viên với email "<email_nhap>"
      Then hồ sơ không được tạo vì email đã được dùng

      Examples:
        | chu_so_huu                          | email_nhap       |
        | một hồ sơ đang coaching             | AN@mail.com      |
        | tài khoản coach                     | an@mail.com      |
        | một hồ sơ đã kết thúc coaching      | an@mail.com      |

  @BR-f02-ho-so-hoc-vien-002
  Rule: Hồ sơ chỉ được lưu khi có email đúng định dạng

    Scenario: Thiếu email thì hồ sơ không được lưu
      When coach tạo hồ sơ học viên tên "Lan" mà không có email
      Then hồ sơ không được lưu vì thiếu email

    Scenario: Email sai định dạng thì hồ sơ không được lưu
      When coach tạo hồ sơ học viên tên "Lan" với email "an@mail"
      Then hồ sơ không được lưu vì email không hợp lệ

  @BR-f02-ho-so-hoc-vien-003
  Rule: Hồ sơ chỉ được lưu khi có họ tên không rỗng và dài tối đa 100 ký tự

    @web
    Scenario: Hồ sơ chỉ có họ tên và email vẫn lưu được
      When coach tạo hồ sơ học viên chỉ với họ tên "Lan" và email "lan@gmail.com"
      Then hồ sơ của "Lan" được lưu

    Scenario: Họ tên chỉ toàn khoảng trắng thì hồ sơ không được lưu
      When coach tạo hồ sơ học viên với họ tên chỉ gồm khoảng trắng và email "lan@gmail.com"
      Then hồ sơ không được lưu vì thiếu họ tên

  @BR-f02-ho-so-hoc-vien-004
  Rule: Hồ sơ luôn cho biết danh sách mục bắt buộc còn thiếu; mục chỉ chứa khoảng trắng tính là thiếu

    Scenario: Hồ sơ còn thiếu mục bắt buộc vẫn lưu được và cho biết các mục thiếu
      When coach lưu hồ sơ học viên đủ mọi mục bắt buộc trừ "Gói đã mua", "Mục tiêu tập luyện" và "Ảnh mặt sau"
      Then hồ sơ được lưu
      And hồ sơ cho biết còn thiếu đúng 3 mục: "Gói đã mua", "Mục tiêu tập luyện", "Ảnh mặt sau"

    Scenario: Hồ sơ đủ mọi mục bắt buộc thì không còn mục thiếu
      When coach lưu hồ sơ học viên đủ mọi mục bắt buộc
      Then hồ sơ cho biết không còn mục bắt buộc nào thiếu

  @BR-f02-ho-so-hoc-vien-005
  Rule: Hồ sơ mới tạo có trạng thái tài khoản Chờ kích hoạt

    Scenario: Hồ sơ mới ở trạng thái tài khoản Chờ kích hoạt
      When coach tạo hồ sơ học viên "Lan"
      Then tài khoản của "Lan" ở trạng thái "Chờ kích hoạt"

  @BR-f02-ho-so-hoc-vien-006
  Rule: Hồ sơ mới tạo có trạng thái coaching Đang coaching

    Scenario: Hồ sơ mới ở trạng thái coaching Đang coaching
      When coach tạo hồ sơ học viên "Lan"
      Then trạng thái coaching của "Lan" là "Đang coaching"

  @BR-f02-ho-so-hoc-vien-007
  Rule: Hồ sơ thuộc coach đã tạo ra nó

    Scenario: Hồ sơ thuộc coach tạo ra nó
      When coach tạo hồ sơ học viên "Lan"
      Then hồ sơ của "Lan" thuộc coach "Coach Duobexiuuu"

  @BR-f02-ho-so-hoc-vien-008
  Rule: Số liệu của hồ sơ khi có nhập phải nằm trong khoảng cho phép (tuổi 10–100, chiều cao 100–250 cm, cân nặng 20–300 kg, mỡ cơ thể 3–70 %, vòng đo 10–250 cm, giá từ 0 đồng)

    Scenario Outline: Số liệu trong khoảng cho phép được ghi nhận
      When coach lưu hồ sơ học viên có "<muc>" là "<gia_tri>"
      Then hồ sơ ghi nhận "<muc>" là "<gia_tri>"

      Examples:
        | muc        | gia_tri |
        | Cân nặng   | 72,5    |
        | Mỡ cơ thể  | 3       |
        | Mỡ cơ thể  | 70      |
        | Giá        | 0       |

    Scenario Outline: Số liệu ngoài khoảng cho phép bị từ chối
      When coach lưu hồ sơ học viên có "<muc>" là "<gia_tri>"
      Then hồ sơ không được lưu vì "<muc>" ngoài khoảng cho phép

      Examples:
        | muc        | gia_tri |
        | Cân nặng   | 720     |
        | Tuổi       | 9       |
        | Tuổi       | 101     |
        | Chiều cao  | 250,1   |
        | Giá        | -1      |

  @BR-f02-ho-so-hoc-vien-009
  Rule: Mục có danh sách giá trị cho trước chỉ nhận một giá trị trong danh sách của mục đó

    Scenario: Giá trị có trong danh sách được ghi nhận
      When coach lưu hồ sơ học viên có "Số buổi tập mỗi tuần" là "4 buổi"
      Then hồ sơ ghi nhận "Số buổi tập mỗi tuần" là "4 buổi"

  @BR-f02-ho-so-hoc-vien-010
  Rule: Ngày chuyển khoản không được sau ngày hôm nay theo giờ Việt Nam

    Scenario: Ngày chuyển khoản là hôm nay được ghi nhận
      Given hôm nay là "25/11/2026" theo giờ Việt Nam
      When coach lưu hồ sơ học viên có ngày chuyển khoản "25/11/2026"
      Then hồ sơ ghi nhận ngày chuyển khoản "25/11/2026"

    Scenario: Ngày hôm nay được tính theo giờ Việt Nam
      Given thời điểm hiện tại là 23:30 ngày "24/11/2026" giờ UTC
      When coach lưu hồ sơ học viên có ngày chuyển khoản "25/11/2026"
      Then hồ sơ ghi nhận ngày chuyển khoản "25/11/2026"

    Scenario: Ngày chuyển khoản sau hôm nay bị từ chối
      Given hôm nay là "25/11/2026" theo giờ Việt Nam
      When coach lưu hồ sơ học viên có ngày chuyển khoản "26/11/2026"
      Then hồ sơ không được lưu vì ngày chuyển khoản sau hôm nay

  @BR-f02-ho-so-hoc-vien-011
  Rule: Link ảnh của hồ sơ phải là địa chỉ web http hoặc https

    Scenario: Link ảnh là địa chỉ web được ghi nhận
      When coach lưu hồ sơ học viên có ảnh mặt trước "https://drive.google.com/open?id=abc"
      Then hồ sơ ghi nhận ảnh mặt trước "https://drive.google.com/open?id=abc"

    Scenario: Link ảnh không phải địa chỉ web bị từ chối
      When coach lưu hồ sơ học viên có ảnh mặt trước "drive.google.com/abc"
      Then hồ sơ không được lưu vì link ảnh không hợp lệ

  @BR-f02-ho-so-hoc-vien-012
  Rule: Coach sửa được mọi mục của hồ sơ ngoài email, kể cả số đo và ảnh ban đầu

    Scenario: Coach sửa số đo ban đầu
      Given hồ sơ của "Lan" có cân nặng 72 kg
      When coach sửa cân nặng của "Lan" thành 70 kg
      Then hồ sơ của "Lan" ghi nhận cân nặng 70 kg

  @BR-f02-ho-so-hoc-vien-013
  Rule: Hồ sơ ghi nhận người sửa và thời điểm sửa gần nhất

    Scenario: Hồ sơ cho biết lần sửa gần nhất
      Given hồ sơ của "Lan" đã có
      And bây giờ là 10:00 ngày "23/11/2026" giờ Việt Nam
      When coach sửa mục tiêu tập luyện của "Lan"
      Then hồ sơ của "Lan" cho biết sửa lần cuối lúc 10:00 ngày "23/11/2026" bởi "Coach Duobexiuuu"

  @BR-f02-ho-so-hoc-vien-014
  Rule: Ghi chú nội bộ có thời điểm sửa gần nhất riêng, chỉ đổi khi ghi chú đổi

    Scenario: Sửa ghi chú cập nhật thời điểm sửa ghi chú
      Given hồ sơ của "Lan" đã có
      And hôm nay là "23/11/2026" theo giờ Việt Nam
      When coach sửa ghi chú nội bộ của "Lan"
      Then ghi chú nội bộ của "Lan" cho biết sửa lần cuối ngày "23/11/2026"

    Scenario: Sửa mục khác không đổi thời điểm sửa ghi chú
      Given ghi chú nội bộ của "Lan" sửa lần cuối ngày "23/11/2026"
      And hôm nay là "24/11/2026" theo giờ Việt Nam
      When coach sửa mục tiêu tập luyện của "Lan"
      Then ghi chú nội bộ của "Lan" vẫn cho biết sửa lần cuối ngày "23/11/2026"

  @BR-f02-ho-so-hoc-vien-015
  Rule: Lần lưu dựa trên một phiên bản hồ sơ đã cũ bị từ chối, không ghi đè lần lưu trước

    Scenario: Sửa dựa trên phiên bản mới nhất thì lưu được
      Given coach đang xem phiên bản mới nhất của hồ sơ "Lan"
      When coach lưu thay đổi mục tiêu tập luyện của "Lan"
      Then thay đổi được lưu

    Scenario: Sửa dựa trên phiên bản đã có người lưu đè thì bị từ chối
      Given coach mở hồ sơ "Lan" ở hai nơi cùng một phiên bản
      And thay đổi ở nơi thứ nhất đã được lưu
      When coach lưu thay đổi ở nơi thứ hai
      Then thay đổi ở nơi thứ hai không được lưu vì hồ sơ đã được sửa ở nơi khác
      And hồ sơ của "Lan" giữ thay đổi của nơi thứ nhất

  @BR-f02-ho-so-hoc-vien-016
  Rule: Ghi chú nội bộ không có trong bất kỳ dữ liệu nào học viên nhận được

    Scenario: Coach thấy ghi chú nội bộ trong hồ sơ
      Given ghi chú nội bộ của "Lan" là "Tránh lunge sâu vì gối phải"
      When coach xem hồ sơ của "Lan"
      Then coach thấy ghi chú nội bộ "Tránh lunge sâu vì gối phải"

    Scenario: Học viên không nhận được ghi chú nội bộ trong dữ liệu của mình
      Given ghi chú nội bộ của "Lan" là "Tránh lunge sâu vì gối phải"
      When "Lan" lấy dữ liệu của chính mình
      Then dữ liệu "Lan" nhận được không chứa ghi chú nội bộ

  @BR-f02-ho-so-hoc-vien-017
  Rule: Trạng thái coaching chỉ đổi sang một trạng thái khác trạng thái hiện tại

    Scenario: Đổi sang trạng thái khác thì được
      Given trạng thái coaching của "Lan" là "Đang coaching"
      When coach đổi trạng thái coaching của "Lan" sang "Tạm dừng" với lý do "Đi công tác 2 tuần"
      Then trạng thái coaching của "Lan" là "Tạm dừng"

    Scenario: Đổi sang đúng trạng thái hiện tại bị từ chối
      Given trạng thái coaching của "Lan" là "Đang coaching"
      When coach đổi trạng thái coaching của "Lan" sang "Đang coaching" với lý do "Nhầm"
      Then việc đổi trạng thái bị từ chối vì trạng thái không thay đổi

  @BR-f02-ho-so-hoc-vien-018
  Rule: Đổi trạng thái coaching cần lý do không rỗng, dài tối đa 500 ký tự

    Scenario: Có lý do thì đổi được
      Given trạng thái coaching của "Lan" là "Tạm dừng"
      When coach đổi trạng thái coaching của "Lan" sang "Đã kết thúc" với lý do "Không gia hạn gói"
      Then trạng thái coaching của "Lan" là "Đã kết thúc"

    Scenario: Lý do chỉ toàn khoảng trắng thì không đổi được
      Given trạng thái coaching của "Lan" là "Đang coaching"
      When coach đổi trạng thái coaching của "Lan" sang "Tạm dừng" với lý do chỉ gồm khoảng trắng
      Then việc đổi trạng thái bị từ chối vì thiếu lý do
      And trạng thái coaching của "Lan" vẫn là "Đang coaching"

  @BR-f02-ho-so-hoc-vien-019
  Rule: Trạng thái coaching đổi được giữa Đang coaching, Tạm dừng và Đã kết thúc theo mọi chiều

    Scenario Outline: Chuyển giữa các trạng thái coaching
      Given trạng thái coaching của "Lan" là "<tu>"
      When coach đổi trạng thái coaching của "Lan" sang "<sang>" với lý do "<ly_do>"
      Then trạng thái coaching của "Lan" là "<sang>"

      Examples:
        | tu            | sang          | ly_do            |
        | Tạm dừng      | Đang coaching | Đi công tác về   |
        | Đang coaching | Đã kết thúc   | Hết gói          |

    Scenario: Mở lại hồ sơ đã kết thúc coaching
      Given trạng thái coaching của "Lan" là "Đã kết thúc"
      And hồ sơ của "Lan" có mục tiêu tập luyện "Giảm mỡ, giữ cơ"
      When coach đổi trạng thái coaching của "Lan" sang "Đang coaching" với lý do "Mua gói mới"
      Then trạng thái coaching của "Lan" là "Đang coaching"
      And hồ sơ của "Lan" vẫn có mục tiêu tập luyện "Giảm mỡ, giữ cơ"

  @BR-f02-ho-so-hoc-vien-020
  Rule: Mỗi lần trạng thái coaching đổi thành công đều có đúng một dòng lịch sử gồm trạng thái cũ, trạng thái mới, người thực hiện, thời điểm theo giờ Việt Nam và lý do

    Scenario: Đổi trạng thái thêm đúng một dòng lịch sử
      Given trạng thái coaching của "Lan" là "Đang coaching"
      And bây giờ là 09:00 ngày "25/11/2026" giờ Việt Nam
      When coach đổi trạng thái coaching của "Lan" sang "Tạm dừng" với lý do "Đi công tác"
      Then lịch sử trạng thái của "Lan" có thêm đúng một dòng:
        | tu            | sang     | nguoi_thuc_hien  | thoi_diem        | ly_do       |
        | Đang coaching | Tạm dừng | Coach Duobexiuuu | 09:00 25/11/2026 | Đi công tác |

  @BR-f02-ho-so-hoc-vien-021
  Rule: Lịch sử trạng thái coaching không sửa và không xóa được

    Scenario: Coach xem đủ lịch sử trạng thái, mới nhất trước
      Given "Lan" đã được chuyển "Đang coaching" sang "Tạm dừng" rồi "Tạm dừng" sang "Đang coaching"
      When coach xem lịch sử trạng thái của "Lan"
      Then lịch sử có 2 dòng, dòng "Tạm dừng" sang "Đang coaching" đứng đầu

  @BR-f02-ho-so-hoc-vien-022
  Rule: Hồ sơ đang Chờ kích hoạt vẫn được coach sửa, ghi chú và đổi trạng thái coaching như hồ sơ đã kích hoạt

    Scenario: Tạm dừng hồ sơ còn Chờ kích hoạt
      Given tài khoản của "Lan" ở trạng thái "Chờ kích hoạt"
      When coach đổi trạng thái coaching của "Lan" sang "Tạm dừng" với lý do "Chưa chuyển khoản đủ"
      Then trạng thái coaching của "Lan" là "Tạm dừng"

    Scenario: Sửa hồ sơ còn Chờ kích hoạt
      Given tài khoản của "Lan" ở trạng thái "Chờ kích hoạt"
      When coach sửa mục tiêu tập luyện của "Lan" thành "Tăng cơ"
      Then hồ sơ của "Lan" có mục tiêu tập luyện "Tăng cơ"

  @BR-f02-ho-so-hoc-vien-023
  Rule: Danh sách học viên của coach gồm mọi hồ sơ ở mọi trạng thái, hồ sơ tạo sau đứng trước

    Scenario: Danh sách có đủ hồ sơ ở mọi trạng thái, mới nhất trước
      Given coach đã tạo lần lượt hồ sơ "Nam", "Quân", "Lan"
      And trạng thái coaching của "Nam" là "Đã kết thúc"
      When coach xem danh sách học viên
      Then danh sách lần lượt là "Lan", "Quân", "Nam"
      And mỗi dòng cho biết tên, email, trạng thái coaching và trạng thái tài khoản

  @BR-f02-ho-so-hoc-vien-024
  Rule: Tìm học viên khớp khi từ khóa nằm trong họ tên hoặc email, không phân biệt hoa/thường và không phân biệt dấu tiếng Việt

    @web
    Scenario Outline: Tìm theo một phần họ tên hoặc email
      Given có hồ sơ "Phạm Hoàng Quân" với email "hquanbilly@gmail.com"
      When coach tìm học viên với từ khóa "<tu_khoa>"
      Then kết quả có hồ sơ "Phạm Hoàng Quân"

      Examples:
        | tu_khoa |
        | quan    |
        | Quân    |
        | hquan   |

    Scenario: Không hồ sơ nào khớp từ khóa
      Given có hồ sơ "Phạm Hoàng Quân" với email "hquanbilly@gmail.com"
      When coach tìm học viên với từ khóa "zzz"
      Then kết quả tìm kiếm rỗng

  @BR-f02-ho-so-hoc-vien-025
  Rule: Hồ sơ học viên không bị xóa; ngừng coaching bằng cách chuyển sang Đã kết thúc

    Scenario: Hồ sơ đã kết thúc coaching vẫn còn trong danh sách
      Given trạng thái coaching của "Lan" là "Đang coaching"
      When coach đổi trạng thái coaching của "Lan" sang "Đã kết thúc" với lý do "Hết gói"
      Then hồ sơ của "Lan" vẫn có trong danh sách học viên
