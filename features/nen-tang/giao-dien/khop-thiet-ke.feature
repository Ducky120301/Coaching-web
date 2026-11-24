@web
Feature: Giao diện khung khớp thiết kế đã duyệt
  # spec: docs/agentic-workflow/specs/2026-11-24-f00-codebase.md
  # design: docs/duobexiuuu-coaching-design.html
  Là PO của Duobexiuuu Coaching
  Tôi muốn màn đăng nhập và khung hai web giống thiết kế đã duyệt ở cả điện thoại lẫn máy tính
  Để học viên và coach thấy đúng thương hiệu ngay từ phiên bản đầu

  @NFR-f00-codebase-002
  Rule: Màn đăng nhập, khung web quản trị và khung web học viên sai lệch không quá 2% so với thiết kế

    Scenario Outline: Màn hình khớp thiết kế ở độ rộng màn hình đã định
      Given thiết kế đã duyệt của "<man_hinh>"
      When "<man_hinh>" được hiển thị ở độ rộng "<do_rong>" px
      Then phần khác biệt so với thiết kế không quá "2" phần trăm diện tích

      Examples:
        | man_hinh                   | do_rong |
        | màn đăng nhập              | 390     |
        | màn đăng nhập              | 879     |
        | màn đăng nhập              | 880     |
        | màn đăng nhập              | 1280    |
        | khung web quản trị         | 390     |
        | khung web quản trị         | 879     |
        | khung web quản trị         | 880     |
        | khung web quản trị         | 1280    |
        | khung web học viên         | 390     |
        | khung web học viên         | 879     |
        | khung web học viên         | 880     |
        | khung web học viên         | 1280    |
