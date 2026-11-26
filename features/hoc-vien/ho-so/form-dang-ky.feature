@api
Feature: Form hồ sơ học viên theo câu hỏi Google Form
  # spec: docs/agentic-workflow/specs/2026-11-26-f02-import-excel.md
  Là coach của Duobexiuuu Coaching
  Tôi muốn hồ sơ học viên hỏi đúng các câu như Google Form đăng ký
  Để dữ liệu nhập tay và dữ liệu import từ Excel khớp nhau, không phải đoán

  Background:
    Given coach "Coach Duobexiuuu" đã đăng nhập

  @BR-f02-import-excel-001
  Rule: Hồ sơ học viên không có mục "Lịch rảnh" và "Dụng cụ"

    Scenario: Hồ sơ mới không có Lịch rảnh và Dụng cụ
      When coach tạo hồ sơ học viên đủ mọi mục bắt buộc
      Then hồ sơ không có mục "Lịch rảnh" và "Dụng cụ"

  @BR-f02-import-excel-003
  Rule: "Số năm tập luyện" nhận chữ tự do dài tối đa 200 ký tự và giữ nguyên văn

    Scenario Outline: Số năm tập luyện được ghi nguyên văn
      When coach lưu hồ sơ học viên có "Số năm tập luyện" là "<so_nam_tap>"
      Then hồ sơ ghi nhận "Số năm tập luyện" là "<so_nam_tap>"

      Examples:
        | so_nam_tap         |
        | 1                  |
        | Khoảng 2 năm rưỡi  |

  @BR-f02-import-excel-004
  Rule: "Mỡ cơ thể và cách đo" là một mục chữ tự do dài tối đa 200 ký tự

    Scenario Outline: Mỡ cơ thể và cách đo được ghi nguyên văn
      When coach lưu hồ sơ học viên có "Mỡ cơ thể và cách đo" là "<mo_co_the>"
      Then hồ sơ ghi nhận "Mỡ cơ thể và cách đo" là "<mo_co_the>"

      Examples:
        | mo_co_the      |
        | 34             |
        | 18% đo InBody  |

  @BR-f02-import-excel-005
  Rule: "Cân nặng và chiều cao trước khi tập tạ" là một mục chữ tự do dài tối đa 2000 ký tự

    Scenario: Cân nặng và chiều cao trước khi tập tạ được ghi nguyên văn
      When coach lưu hồ sơ học viên có "Cân nặng và chiều cao trước khi tập tạ" là "60kg, 1m70"
      Then hồ sơ ghi nhận "Cân nặng và chiều cao trước khi tập tạ" là "60kg, 1m70"

  @BR-f02-import-excel-006
  Rule: Hồ sơ có một "Link ảnh toàn thân" là địa chỉ web http hoặc https, dài tối đa 2048 ký tự

    Scenario: Link ảnh toàn thân là địa chỉ web được ghi nhận
      When coach lưu hồ sơ học viên có "Link ảnh toàn thân" là "https://drive.google.com/open?id=abc"
      Then hồ sơ ghi nhận "Link ảnh toàn thân" là "https://drive.google.com/open?id=abc"

    Scenario Outline: Link ảnh toàn thân không phải địa chỉ web bị từ chối
      When coach lưu hồ sơ học viên có "Link ảnh toàn thân" là "<link>"
      Then hồ sơ không được lưu vì link ảnh không hợp lệ

      Examples:
        | link               |
        | drive.google.com/abc |
        | ftp://x.vn/a.jpg   |

  @BR-f02-import-excel-007
  Rule: Mục bắt buộc của hồ sơ gồm "Số năm tập luyện", "Cân nặng và chiều cao trước khi tập tạ" và "Link ảnh toàn thân"; "Mỡ cơ thể và cách đo" không bắt buộc

    Scenario: Thiếu Link ảnh toàn thân được báo là mục còn thiếu
      When coach lưu hồ sơ học viên đủ mọi mục bắt buộc trừ "Link ảnh toàn thân"
      Then hồ sơ cho biết còn thiếu đúng 1 mục: "Link ảnh toàn thân"

    Scenario: Để trống Mỡ cơ thể và cách đo không bị tính là thiếu
      When coach lưu hồ sơ học viên đủ mọi mục bắt buộc và để trống "Mỡ cơ thể và cách đo"
      Then hồ sơ cho biết không còn mục bắt buộc nào thiếu
