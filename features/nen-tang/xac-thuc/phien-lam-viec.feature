Feature: Phiên làm việc sau khi đăng nhập
  # spec: docs/agentic-workflow/specs/2026-11-24-f00-codebase.md
  Là coach hoặc học viên đã đăng nhập
  Tôi muốn phiên làm việc được giữ liên tục, kết thúc được khi tôi muốn và đưa tôi về đúng trang đang mở
  Để không phải đăng nhập lại liên tục mà phiên vẫn an toàn

  @BR-f00-codebase-010
  Rule: Phiên đăng nhập được tự gia hạn trong vòng 14 ngày kể từ lần gia hạn gần nhất

    @web
    Scenario: Lượt truy cập hết hạn giữa chừng thì thao tác vẫn thành công
      Given admin đang làm việc và lượt truy cập vừa hết hạn
      When admin xem hồ sơ coach của mình
      Then admin thấy hồ sơ coach mà không phải đăng nhập lại

    @manual
    Scenario: Mở lại trình duyệt sau 3 ngày vẫn còn đăng nhập
      Given học viên đã đăng nhập web học viên và đóng trình duyệt cách đây "3" ngày
      When học viên mở lại web học viên
      Then học viên vẫn ở trạng thái đã đăng nhập

    @manual
    Scenario: Không quay lại quá 14 ngày thì phải đăng nhập lại
      Given học viên đã đăng nhập web học viên và không quay lại trong "15" ngày
      When học viên mở lại web học viên
      Then học viên phải đăng nhập lại

  @BR-f00-codebase-011
  Rule: Mã gia hạn phiên vừa được đổi, nếu bị gửi lại trong vòng 30 giây, vẫn được chấp nhận

    @api
    Scenario: Gửi lại mã gia hạn vừa đổi trong vòng 30 giây vẫn giữ được phiên
      Given một phiên vừa được gia hạn cách đây "5" giây
      When mã gia hạn trước lần đổi đó được gửi lại
      Then người dùng vẫn giữ được phiên mà không phải đăng nhập lại

  @BR-f00-codebase-012
  Rule: Mã gia hạn phiên đã đổi, nếu bị gửi lại sau hơn 30 giây, khiến mọi phiên của tài khoản bị thu hồi

    @api
    Scenario: Gửi lại mã gia hạn cũ sau 45 giây thì mọi phiên bị thu hồi
      Given một tài khoản đang đăng nhập trên hai thiết bị
      And một phiên của tài khoản đó đã được gia hạn cách đây "45" giây
      When mã gia hạn trước lần đổi đó được gửi lại
      Then hệ thống từ chối
      And cả hai thiết bị của tài khoản đó phải đăng nhập lại

  @BR-f00-codebase-013
  Rule: Đăng xuất làm mã gia hạn phiên của thiết bị đó hết hiệu lực ngay

    @web
    Scenario: Đăng xuất đưa người dùng về trạng thái chưa đăng nhập
      Given admin đã đăng nhập web quản trị
      When admin đăng xuất
      Then admin ở trạng thái chưa đăng nhập web quản trị

    @api
    Scenario: Không thể gia hạn phiên đã đăng xuất
      Given một phiên đã đăng xuất
      When mã gia hạn của phiên đó được gửi lại
      Then hệ thống từ chối và người dùng phải đăng nhập lại

  @BR-f00-codebase-014
  Rule: Đăng xuất trong một tab thì các tab khác của cùng web trên trình duyệt đó cũng kết thúc phiên

    @web
    Scenario: Đăng xuất ở một tab thì tab khác của cùng web cũng đăng xuất
      Given admin đang mở web quản trị ở hai tab của cùng trình duyệt
      When admin đăng xuất ở tab thứ nhất
      Then tab thứ hai cũng ở trạng thái chưa đăng nhập

    @web
    Scenario: Đăng xuất web quản trị không ảnh hưởng phiên ở web học viên
      Given trên cùng trình duyệt có một phiên ở web quản trị và một phiên ở web học viên
      When người dùng đăng xuất web quản trị
      Then phiên ở web học viên vẫn còn đăng nhập

  @BR-f00-codebase-015
  Rule: Sau khi đăng nhập, người dùng được đưa về đúng trang cần đăng nhập đã mở nếu trang thuộc cùng web

    @web
    Scenario: Đăng nhập xong quay về đúng trang đã mở
      Given admin chưa đăng nhập và mở một trang con của khu vực coach
      When admin đăng nhập web quản trị thành công
      Then admin được đưa về đúng trang con đó

    @web
    Scenario: Địa chỉ quay về trỏ ra ngoài web thì về trang chủ
      Given admin mở một đường dẫn đăng nhập web quản trị yêu cầu quay về một trang ngoài web quản trị
      When admin đăng nhập web quản trị thành công
      Then admin được đưa về trang chủ của web quản trị
