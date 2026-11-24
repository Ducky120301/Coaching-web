@manual
Feature: Nghiệm thu P0 trên môi trường test
  # spec: docs/agentic-workflow/specs/2026-11-24-f00-codebase.md
  Là PO của Duobexiuuu Coaching
  Tôi muốn đăng nhập web quản trị trên môi trường test công khai và thấy dữ liệu thật
  Để xác nhận P0 đã chạy đầu–cuối trước khi bắt đầu P1

  @NFR-f00-codebase-008
  Rule: P0 chỉ hoàn thành khi admin khởi tạo đăng nhập được web quản trị trên môi trường test và thấy tên coach lấy từ hệ thống

    Scenario: Admin khởi tạo đăng nhập trên môi trường test và thấy tên coach
      Given môi trường test đã được triển khai từ nhánh main
      When admin khởi tạo đăng nhập web quản trị trên môi trường test
      Then admin thấy tên coach của mình lấy từ hệ thống
