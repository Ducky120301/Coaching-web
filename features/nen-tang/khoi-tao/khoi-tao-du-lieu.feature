@integration
Feature: Khởi tạo dữ liệu ban đầu của hệ thống
  # spec: docs/agentic-workflow/specs/2026-11-24-f00-codebase.md
  Là PO vận hành hệ thống
  Tôi muốn hệ thống tự tạo tài khoản coach (và một học viên demo khi ở môi trường test) một cách an toàn
  Để nghiệm thu được ngay mà không cần thao tác tay vào cơ sở dữ liệu

  @BR-f00-codebase-016
  Rule: Khởi tạo hệ thống luôn cho ra đúng một tài khoản admin và một hồ sơ coach gắn với tài khoản đó

    Scenario: Khởi động lần đầu tạo admin đăng nhập được kèm hồ sơ coach
      Given hệ thống chưa có dữ liệu và cấu hình khởi tạo admin hợp lệ
      When hệ thống khởi động
      Then admin khởi tạo đăng nhập được và thấy hồ sơ coach của mình

    Scenario: Khởi động lại không tạo thêm admin hay hồ sơ coach
      Given hệ thống đã khởi động một lần với cấu hình khởi tạo admin
      When hệ thống khởi động lại
      Then hệ thống vẫn chỉ có một tài khoản admin và một hồ sơ coach

  @BR-f00-codebase-017
  Rule: Hệ thống không khởi động khi thiếu email hoặc mật khẩu của tài khoản admin khởi tạo

    Scenario: Thiếu mật khẩu admin khởi tạo thì hệ thống không khởi động
      Given cấu hình khởi tạo admin không có mật khẩu
      When hệ thống khởi động
      Then hệ thống không khởi động

  @BR-f00-codebase-018
  Rule: Hệ thống không khởi động khi mật khẩu admin khởi tạo ngắn hơn 8 ký tự

    Scenario: Mật khẩu admin khởi tạo 6 ký tự thì hệ thống không khởi động
      Given cấu hình khởi tạo admin có mật khẩu dài "6" ký tự
      When hệ thống khởi động
      Then hệ thống không khởi động

  @BR-f00-codebase-019
  Rule: Mật khẩu của admin đã tồn tại không bị thay đổi bởi cấu hình khởi tạo

    Scenario: Đổi mật khẩu trong cấu hình khởi tạo không đổi mật khẩu admin đang có
      Given admin khởi tạo đã tồn tại với mật khẩu ban đầu
      And mật khẩu trong cấu hình khởi tạo đã được đổi sang giá trị khác
      When hệ thống khởi động lại
      Then admin vẫn đăng nhập được bằng mật khẩu ban đầu

  @BR-f00-codebase-020
  Rule: Học viên demo chỉ được tạo khi môi trường bật cấu hình học viên demo

    Scenario: Môi trường bật học viên demo thì có học viên demo đăng nhập được
      Given môi trường bật cấu hình học viên demo
      When hệ thống khởi động
      Then có đúng một học viên demo và học viên đó đăng nhập được

    Scenario: Môi trường không bật học viên demo thì không có học viên demo
      Given môi trường không bật cấu hình học viên demo
      When hệ thống khởi động
      Then hệ thống không có học viên demo nào
