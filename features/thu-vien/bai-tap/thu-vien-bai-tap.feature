@api
Feature: Thư viện bài tập của coach
  # spec: docs/agentic-workflow/specs/2026-11-26-f03-thu-vien-bai-tap.md
  # design: docs/duobexiuuu-coaching-design.html (AdThuVien)
  Là coach của Duobexiuuu Coaching
  Tôi muốn lưu mỗi bài tập một lần với nhóm cơ, dụng cụ, video mẫu, liều mặc định và hướng dẫn
  Để khi soạn giáo án chỉ cần chọn bài thay vì gõ lại cho từng học viên

  Background:
    Given coach "Coach Duobexiuuu" đã đăng nhập

  @BR-f03-thu-vien-bai-tap-001
  Rule: Mỗi bài trong thư viện của coach là duy nhất theo cặp (tên bài, dụng cụ), kể cả bài đang ẩn; hai tên chỉ khác chữ hoa/thường, dấu tiếng Việt, khoảng trắng thừa hoặc kiểu dấu gạch được coi là trùng

    @web
    Scenario: Cùng tên khác dụng cụ là hai bài khác nhau
      Given thư viện có bài "Lateral Raise" dụng cụ "Cable"
      When coach thêm bài "Lateral Raise" dụng cụ "Dumbbell" nhóm cơ "Vai"
      Then thư viện có cả "Lateral Raise" dụng cụ "Cable" và "Lateral Raise" dụng cụ "Dumbbell"

    Scenario Outline: Tên chỉ khác cách viết vẫn là bài trùng
      Given thư viện có bài "<ten_co_san>" dụng cụ "Cable"
      When coach thêm bài "<ten_moi>" dụng cụ "Cable" nhóm cơ "Lưng"
      Then bài không được thêm vì đã có bài cùng tên và dụng cụ

      Examples:
        | ten_co_san      | ten_moi              |
        | Lateral Raise   |   lateral   raise    |
        | Ép ngực         | Ep nguc              |
        | T–Bar Row       | T-Bar Row            |

    Scenario: Bài trùng với một bài đang ẩn
      Given thư viện có bài "Face Pull" dụng cụ "Cable" đang ẩn
      When coach thêm bài "Face Pull" dụng cụ "Cable" nhóm cơ "Vai sau"
      Then bài không được thêm vì đã có bài cùng tên và dụng cụ
      And coach được gợi ý bỏ ẩn bài "Face Pull" dụng cụ "Cable"

    Scenario: Sửa chữ hoa/thường tên của chính bài đó vẫn lưu được
      Given thư viện có bài "lateral raise" dụng cụ "Cable"
      When coach đổi tên bài đó thành "Lateral Raise"
      Then thư viện có bài "Lateral Raise" dụng cụ "Cable"

    Scenario: Sửa một bài thành tên và dụng cụ của bài khác
      Given thư viện có bài "Leg Press" dụng cụ "Machine" và bài "Hack Squat" dụng cụ "Machine"
      When coach đổi tên bài "Hack Squat" thành "Leg Press"
      Then bài không được lưu vì đã có bài cùng tên và dụng cụ
      And bài "Hack Squat" giữ nguyên

  @BR-f03-thu-vien-bai-tap-002
  Rule: Tên bài là bắt buộc và dài tối đa 100 ký tự sau khi bỏ khoảng trắng đầu/cuối

    Scenario: Bài có tên được lưu
      When coach thêm bài "Barbell Bench Press" dụng cụ "Barbell" nhóm cơ "Ngực"
      Then thư viện có bài "Barbell Bench Press" dụng cụ "Barbell"

    Scenario: Bài không có tên không được lưu
      When coach thêm một bài không có tên, dụng cụ "Barbell" nhóm cơ "Ngực"
      Then bài không được thêm vì thiếu tên

  @BR-f03-thu-vien-bai-tap-003
  Rule: Mỗi bài có đúng một nhóm cơ thuộc danh mục nhóm cơ của coach

    Scenario: Bài có nhóm cơ trong danh mục được lưu
      When coach thêm bài "Chest Press" dụng cụ "Machine" nhóm cơ "Ngực"
      Then bài "Chest Press" thuộc nhóm cơ "Ngực"

    Scenario: Bài thiếu nhóm cơ không được lưu
      When coach thêm bài "Chest Press" dụng cụ "Machine", không có nhóm cơ
      Then bài không được thêm vì thiếu nhóm cơ

  @BR-f03-thu-vien-bai-tap-004
  Rule: Mỗi bài có đúng một dụng cụ thuộc danh mục dụng cụ của coach

    Scenario: Bài có dụng cụ trong danh mục được lưu
      When coach thêm bài "Cable Crossover" dụng cụ "Cable" nhóm cơ "Ngực"
      Then bài "Cable Crossover" dùng dụng cụ "Cable"

    Scenario: Bài thiếu dụng cụ không được lưu
      When coach thêm bài "Cable Crossover" nhóm cơ "Ngực", không có dụng cụ
      Then bài không được thêm vì thiếu dụng cụ

  @BR-f03-thu-vien-bai-tap-005
  Rule: Danh mục ban đầu của coach gồm 11 nhóm cơ và 7 dụng cụ

    Scenario: Coach có sẵn danh mục ban đầu
      When coach xem danh mục của thư viện bài tập
      Then danh mục nhóm cơ gồm "Ngực, Vai, Tay sau, Tay trước, Lưng, Vai sau, Đùi trước, Đùi sau, Mông, Bắp chân, Bụng"
      And danh mục dụng cụ gồm "Machine, Cable, Dumbbell, Barbell, Smith Machine, Kettlebell, Bodyweight"

  @BR-f03-thu-vien-bai-tap-006
  Rule: Coach thêm được nhóm cơ hoặc dụng cụ mới có tên không trùng (không phân biệt hoa/thường, dấu, khoảng trắng thừa) với mục cùng loại đã có

    Scenario: Thêm dụng cụ mới rồi dùng ngay cho bài mới
      Given coach đã thêm dụng cụ "Resistance Band"
      When coach thêm bài "Band Pull Apart" dụng cụ "Resistance Band" nhóm cơ "Vai sau"
      Then thư viện có bài "Band Pull Apart" dụng cụ "Resistance Band"

    Scenario: Nhóm cơ trùng tên không được thêm
      When coach thêm nhóm cơ "ngực"
      Then nhóm cơ không được thêm vì đã có "Ngực"

  @BR-f03-thu-vien-bai-tap-007
  Rule: Link video mẫu, khi có, phải là địa chỉ web http hoặc https của YouTube (youtube.com hoặc youtu.be)

    Scenario Outline: Link YouTube được lưu
      When coach thêm bài "Chest Dip" dụng cụ "Bodyweight" nhóm cơ "Ngực" với video "<link>"
      Then bài "Chest Dip" có video "<link>"

      Examples:
        | link                                            |
        | https://www.youtube.com/watch?v=vcBig73ojpE     |
        | https://youtube.com/shorts/PfmfAeMpym4          |
        | https://youtu.be/abc123XYZ09                    |
        | https://www.youtube.com/playlist?list=PL123abc  |
        | https://www.youtube.com/@duobexiuuu             |

    Scenario Outline: Link không phải YouTube không được lưu
      When coach thêm bài "Chest Dip" dụng cụ "Bodyweight" nhóm cơ "Ngực" với video "<link>"
      Then bài không được thêm vì link video không phải YouTube

      Examples:
        | link                                     |
        | https://vimeo.com/123                    |
        | https://youtube.com.evil.com/watch?v=x   |
        | javascript:alert(1)                      |
        | abc                                      |

  @BR-f03-thu-vien-bai-tap-008
  Rule: Bài không có link video được đánh dấu là thiếu video

    Scenario: Bài không có video bị đánh dấu thiếu video
      When coach thêm bài "Dumbbell Fly" dụng cụ "Dumbbell" nhóm cơ "Ngực" không có video
      Then bài "Dumbbell Fly" bị đánh dấu thiếu video

    Scenario: Bài có video không bị đánh dấu thiếu video
      When coach thêm bài "Dumbbell Fly" dụng cụ "Dumbbell" nhóm cơ "Ngực" với video "https://www.youtube.com/watch?v=Nhvz9EzdJ4U"
      Then bài "Dumbbell Fly" không bị đánh dấu thiếu video

  @BR-f03-thu-vien-bai-tap-009
  Rule: Số hiệp (Sets) mặc định, khi có, là số nguyên từ 1 đến 10

    Scenario: Sets mặc định trong khoảng được lưu
      Given thư viện có bài "Chest Press" dụng cụ "Machine"
      When coach cập nhật bài "Chest Press" với Sets mặc định 3
      Then bài "Chest Press" có Sets mặc định 3

    Scenario: Bài không có Sets mặc định vẫn lưu được
      Given thư viện có bài "Chest Press" dụng cụ "Machine"
      When coach cập nhật bài "Chest Press" không có Sets mặc định
      Then bài "Chest Press" được lưu, chưa có Sets mặc định

    Scenario Outline: Sets mặc định ngoài khoảng không được lưu
      Given thư viện có bài "Chest Press" dụng cụ "Machine"
      When coach cập nhật bài "Chest Press" với Sets mặc định <sets>
      Then bài không được lưu vì Sets phải từ 1 đến 10

      Examples:
        | sets |
        | 0    |
        | 11   |

  @BR-f03-thu-vien-bai-tap-010
  Rule: Số lần (Reps) mặc định, khi có, là một số hoặc một khoảng thấp–cao trong 1–100, số thấp không lớn hơn số cao

    Scenario Outline: Reps mặc định hợp lệ được lưu
      Given thư viện có bài "Chest Press" dụng cụ "Machine"
      When coach cập nhật bài "Chest Press" với Reps mặc định "<reps_nhap>"
      Then bài "Chest Press" có Reps mặc định "<reps_luu>"

      Examples:
        | reps_nhap | reps_luu |
        | 10        | 10       |
        | 10–12     | 10–12    |
        | 10-12     | 10–12    |

    Scenario Outline: Reps mặc định không hợp lệ không được lưu
      Given thư viện có bài "Chest Press" dụng cụ "Machine"
      When coach cập nhật bài "Chest Press" với Reps mặc định "<reps_nhap>"
      Then bài không được lưu vì Reps không hợp lệ

      Examples:
        | reps_nhap |
        | 12–10     |
        | 0         |
        | 101       |

  @BR-f03-thu-vien-bai-tap-011
  Rule: RIR mặc định, khi có, là một số hoặc một khoảng thấp–cao trong 0–5, số thấp không lớn hơn số cao

    Scenario Outline: RIR mặc định hợp lệ được lưu
      Given thư viện có bài "Chest Press" dụng cụ "Machine"
      When coach cập nhật bài "Chest Press" với RIR mặc định "<rir>"
      Then bài "Chest Press" có RIR mặc định "<rir>"

      Examples:
        | rir |
        | 1   |
        | 1–2 |

    Scenario Outline: RIR mặc định không hợp lệ không được lưu
      Given thư viện có bài "Chest Press" dụng cụ "Machine"
      When coach cập nhật bài "Chest Press" với RIR mặc định "<rir>"
      Then bài không được lưu vì RIR không hợp lệ

      Examples:
        | rir |
        | 6   |
        | 2–1 |

  @BR-f03-thu-vien-bai-tap-012
  Rule: Thời gian nghỉ mặc định, khi có, tính bằng giây từ 15 đến 600

    Scenario: Thời gian nghỉ trong khoảng được lưu
      Given thư viện có bài "Chest Press" dụng cụ "Machine"
      When coach cập nhật bài "Chest Press" với thời gian nghỉ mặc định 90 giây
      Then bài "Chest Press" có thời gian nghỉ mặc định "90s"

    Scenario Outline: Thời gian nghỉ ngoài khoảng không được lưu
      Given thư viện có bài "Chest Press" dụng cụ "Machine"
      When coach cập nhật bài "Chest Press" với thời gian nghỉ mặc định <giay> giây
      Then bài không được lưu vì thời gian nghỉ phải từ 15 đến 600 giây

      Examples:
        | giay |
        | 10   |
        | 601  |

  @BR-f03-thu-vien-bai-tap-013
  Rule: Hướng dẫn kỹ thuật không bắt buộc, dài tối đa 2000 ký tự

    Scenario: Hướng dẫn kỹ thuật được lưu
      Given thư viện có bài "Lateral Raise" dụng cụ "Cable"
      When coach cập nhật bài "Lateral Raise" với hướng dẫn kỹ thuật "Giữ khuỷu hơi cong, kéo bằng vai"
      Then bài "Lateral Raise" có hướng dẫn kỹ thuật "Giữ khuỷu hơi cong, kéo bằng vai"

  @BR-f03-thu-vien-bai-tap-014
  Rule: Lỗi hay gặp không bắt buộc, dài tối đa 2000 ký tự

    Scenario: Lỗi hay gặp được lưu
      Given thư viện có bài "Lateral Raise" dụng cụ "Cable"
      When coach cập nhật bài "Lateral Raise" với lỗi hay gặp "Nhún vai khi kéo"
      Then bài "Lateral Raise" có lỗi hay gặp "Nhún vai khi kéo"

  @BR-f03-thu-vien-bai-tap-015
  Rule: Coach ẩn và bỏ ẩn được bất kỳ bài nào trong thư viện của mình

    Scenario: Ẩn một bài
      Given thư viện có bài "Chest Dip" dụng cụ "Bodyweight"
      When coach ẩn bài "Chest Dip"
      Then bài "Chest Dip" ở trạng thái đã ẩn

    Scenario: Bỏ ẩn một bài đã ẩn
      Given thư viện có bài "Chest Dip" dụng cụ "Bodyweight" đang ẩn
      When coach bỏ ẩn bài "Chest Dip"
      Then bài "Chest Dip" không còn ở trạng thái đã ẩn

  @BR-f03-thu-vien-bai-tap-016
  Rule: Danh sách và kết quả tìm bài mặc định không gồm bài đã ẩn; coach chỉ thấy bài đã ẩn khi chọn xem bài đã ẩn

    Scenario: Bài đã ẩn không có trong danh sách mặc định
      Given thư viện có bài "Chest Dip" đang ẩn và bài "Chest Press" đang hiện
      When coach xem danh sách bài tập
      Then danh sách có "Chest Press" và không có "Chest Dip"

    Scenario: Coach xem được bài đã ẩn khi chọn xem bài đã ẩn
      Given thư viện có bài "Chest Dip" đang ẩn
      When coach xem các bài đã ẩn
      Then danh sách có "Chest Dip"

  @BR-f03-thu-vien-bai-tap-017
  Rule: Bài đang được dùng trong giáo án không xoá được, chỉ ẩn được; bài chưa được dùng xoá được và cặp (tên, dụng cụ) của nó được dùng lại

    Scenario: Xoá bài chưa được dùng
      Given thư viện có bài "Chest Dip" dụng cụ "Bodyweight" chưa có giáo án nào dùng
      When coach xoá bài "Chest Dip"
      Then thư viện không còn bài "Chest Dip" dụng cụ "Bodyweight"

    Scenario: Xoá bài chưa được dùng rồi thêm lại cùng tên và dụng cụ
      Given thư viện có bài "Chest Dip" dụng cụ "Bodyweight" chưa có giáo án nào dùng
      And coach đã xoá bài "Chest Dip"
      When coach thêm bài "Chest Dip" dụng cụ "Bodyweight" nhóm cơ "Ngực"
      Then thư viện có bài "Chest Dip" dụng cụ "Bodyweight"

    # Cần F05 (giáo án) để có bài "đang được dùng"
    @manual
    Scenario: Bài đang có trong một phase không xoá được
      Given bài "Chest Press" đang có trong giáo án của một học viên
      When coach xoá bài "Chest Press"
      Then bài không bị xoá vì đang được dùng trong giáo án
      And coach được gợi ý ẩn bài thay vì xoá

  @BR-f03-thu-vien-bai-tap-018
  Rule: Tìm bài theo tên khớp một phần, không phân biệt hoa/thường và dấu tiếng Việt

    @web
    Scenario Outline: Tìm theo một phần tên, không dấu
      Given thư viện có bài "<ten_bai>"
      When coach tìm bài với từ khoá "<tu_khoa>"
      Then kết quả có "<ten_bai>"

      Examples:
        | ten_bai                | tu_khoa  |
        | 45° Incline Chest Fly  | incline  |
        | Ép ngực                | ep nguc  |

    Scenario: Từ khoá không khớp bài nào
      Given thư viện có bài "Chest Press"
      When coach tìm bài với từ khoá "zzz"
      Then kết quả tìm rỗng

  @BR-f03-thu-vien-bai-tap-019
  Rule: Lọc theo nhóm cơ, lọc theo dụng cụ và từ khoá tên kết hợp với nhau; bài phải thoả mọi điều kiện đã chọn

    Scenario: Kết hợp nhóm cơ, dụng cụ và từ khoá
      Given thư viện có "Dumbbell Fly" (Ngực, Dumbbell), "Cable Fly" (Ngực, Cable) và "Rear Delt Fly" (Vai sau, Dumbbell)
      When coach lọc nhóm cơ "Ngực", dụng cụ "Dumbbell" và tìm "fly"
      Then kết quả chỉ có "Dumbbell Fly"

    Scenario: Chỉ lọc theo dụng cụ
      Given thư viện có "Cable Fly" (Ngực, Cable) và "Face Pull" (Vai sau, Cable) và "Dumbbell Fly" (Ngực, Dumbbell)
      When coach lọc dụng cụ "Cable"
      Then kết quả có "Cable Fly" và "Face Pull", không có "Dumbbell Fly"

  @BR-f03-thu-vien-bai-tap-020
  Rule: Lần lưu bài dựa trên một phiên bản đã cũ bị từ chối và không ghi đè lần lưu trước

    Scenario: Lần lưu dựa trên phiên bản cũ bị từ chối
      Given coach đang sửa bài "Chest Press" và bài đó vừa được sửa ở nơi khác
      When coach cập nhật bài "Chest Press" dựa trên phiên bản đang sửa
      Then bài không được lưu vì vừa được sửa ở nơi khác
      And bài "Chest Press" giữ nội dung của lần lưu trước

  @BR-f03-thu-vien-bai-tap-021
  Rule: Sửa bài trong thư viện không làm thay đổi bài đó trong giáo án đã có; giáo án giữ bản chụp lúc thêm bài

    Scenario: Sửa hướng dẫn kỹ thuật thì bài trong thư viện mang nội dung mới
      Given thư viện có bài "Lateral Raise" với hướng dẫn kỹ thuật "Cũ"
      When coach cập nhật bài "Lateral Raise" với hướng dẫn kỹ thuật "Mới"
      Then bài "Lateral Raise" có hướng dẫn kỹ thuật "Mới"

    # Cần F05 (giáo án) để có giáo án đã phát hành
    @manual
    Scenario: Giáo án đã phát hành giữ hướng dẫn cũ
      Given bài "Lateral Raise" có hướng dẫn kỹ thuật "Cũ" đã nằm trong một giáo án đã phát hành
      When coach cập nhật bài "Lateral Raise" với hướng dẫn kỹ thuật "Mới"
      Then giáo án đó vẫn có hướng dẫn kỹ thuật "Cũ" cho bài "Lateral Raise"
