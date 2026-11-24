@api
Feature: Phân quyền truy cập theo vai trò
  # spec: docs/agentic-workflow/specs/2026-11-24-f00-codebase.md
  Là coach hoặc học viên của Duobexiuuu Coaching
  Tôi muốn mỗi vai trò chỉ dùng được đúng chức năng của mình
  Để dữ liệu quản trị và dữ liệu học viên không bị truy cập sai người

  @BR-f00-codebase-001
  Rule: Chức năng quản trị chỉ phục vụ tài khoản vai trò Admin đang đăng nhập

    Scenario: Admin đã đăng nhập xem được hồ sơ coach của mình
      Given admin đã đăng nhập
      When admin xem hồ sơ coach của mình
      Then admin thấy đúng email và tên coach của mình

    Scenario: Từ chối chức năng quản trị khi chưa đăng nhập
      Given người dùng chưa đăng nhập
      When người dùng yêu cầu xem hồ sơ coach
      Then hệ thống từ chối vì người dùng chưa xác thực

    Scenario: Từ chối chức năng quản trị với tài khoản học viên
      Given học viên đã đăng nhập
      When học viên yêu cầu xem hồ sơ coach
      Then hệ thống từ chối vì học viên không đủ quyền

  @BR-f00-codebase-002
  Rule: Chức năng học viên chỉ phục vụ tài khoản vai trò Học viên đang đăng nhập

    Scenario: Học viên đã đăng nhập xem được thông tin tài khoản của chính mình
      Given học viên đã đăng nhập
      When học viên xem thông tin tài khoản của mình
      Then học viên thấy đúng email và họ tên của mình

    Scenario: Từ chối chức năng học viên khi chưa đăng nhập
      Given người dùng chưa đăng nhập
      When người dùng yêu cầu xem thông tin tài khoản học viên
      Then hệ thống từ chối vì người dùng chưa xác thực

    Scenario: Từ chối chức năng học viên với tài khoản admin
      Given admin đã đăng nhập
      When admin yêu cầu xem thông tin tài khoản học viên
      Then hệ thống từ chối vì admin không đủ quyền

  @BR-f00-codebase-003
  Rule: Mỗi lượt truy cập được cấp chỉ có hiệu lực 15 phút kể từ lúc cấp

    Scenario: Lượt truy cập còn hiệu lực ở phút thứ 14
      Given admin được cấp một lượt truy cập cách đây "14" phút
      When admin dùng lượt truy cập đó để xem hồ sơ coach
      Then admin thấy hồ sơ coach của mình

    Scenario: Lượt truy cập hết hiệu lực ở phút thứ 16
      Given admin được cấp một lượt truy cập cách đây "16" phút và chưa gia hạn
      When admin dùng lượt truy cập đó để xem hồ sơ coach
      Then hệ thống từ chối vì người dùng chưa xác thực
