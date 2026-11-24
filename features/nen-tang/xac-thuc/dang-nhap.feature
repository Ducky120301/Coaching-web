Feature: Đăng nhập vào web quản trị và web học viên
  # spec: docs/agentic-workflow/specs/2026-11-24-f00-codebase.md
  Là coach hoặc học viên đã có tài khoản
  Tôi muốn đăng nhập bằng email và mật khẩu vào đúng web dành cho vai trò của mình
  Để làm việc trên hệ thống mà không lộ thông tin tài khoản cho người khác

  @BR-f00-codebase-004
  Rule: Web quản trị chỉ cho đăng nhập bằng tài khoản vai trò Admin

    @web
    Scenario: Admin đăng nhập web quản trị thành công
      Given một tài khoản admin
      When admin đăng nhập web quản trị bằng email và mật khẩu đúng
      Then admin vào được khu vực coach

    @web
    Scenario: Học viên không đăng nhập được web quản trị
      Given một tài khoản học viên
      When học viên đăng nhập web quản trị bằng email và mật khẩu đúng
      Then hệ thống từ chối với thông báo "Tài khoản này không có quyền vào khu vực coach."
      And học viên vẫn ở trạng thái chưa đăng nhập web quản trị

  @BR-f00-codebase-005
  Rule: Web học viên chỉ cho đăng nhập bằng tài khoản vai trò Học viên

    @web
    Scenario: Học viên đăng nhập web học viên thành công
      Given một tài khoản học viên
      When học viên đăng nhập web học viên bằng email và mật khẩu đúng
      Then học viên vào được khu vực học viên

    @web
    Scenario: Admin không đăng nhập được web học viên
      Given một tài khoản admin
      When admin đăng nhập web học viên bằng email và mật khẩu đúng
      Then hệ thống từ chối với thông báo "Tài khoản này không có quyền vào khu vực học viên."
      And admin vẫn ở trạng thái chưa đăng nhập web học viên

  @BR-f00-codebase-006
  Rule: Đăng nhập thất bại luôn trả cùng một thông báo, không tiết lộ email có tài khoản hay tài khoản có đang bị khóa

    @api
    Scenario Outline: Cùng một thông báo cho mọi lần đăng nhập sai thông tin
      Given <tinh_huong>
      When người dùng đăng nhập bằng thông tin đó
      Then hệ thống từ chối với thông báo "Email hoặc mật khẩu không đúng, hoặc bạn đã thử quá nhiều lần. Vui lòng thử lại sau vài phút."

      Examples:
        | tinh_huong                                   |
        | email đã có tài khoản nhưng mật khẩu sai     |
        | email chưa có tài khoản nào                  |
        | tài khoản đang bị khóa đăng nhập, mật khẩu nhập đúng |

  @BR-f00-codebase-007
  Rule: Tài khoản bị khóa đăng nhập 5 phút khi sai mật khẩu 5 lần liên tiếp

    @api
    Scenario: Đăng nhập được khi đúng mật khẩu trước lần sai thứ 5
      Given một tài khoản đã đăng nhập sai mật khẩu "4" lần liên tiếp
      When người dùng đăng nhập vào tài khoản đó bằng mật khẩu đúng
      Then người dùng đăng nhập thành công

    @api
    Scenario: Sau lần sai thứ 5 tài khoản bị khóa trong 5 phút
      Given một tài khoản vừa đăng nhập sai mật khẩu lần thứ "5" liên tiếp cách đây "3" phút
      When người dùng đăng nhập vào tài khoản đó bằng mật khẩu đúng
      Then đăng nhập bị từ chối

    @api
    Scenario: Hết 5 phút khóa thì đăng nhập lại được
      Given một tài khoản bị khóa đăng nhập cách đây "6" phút
      When người dùng đăng nhập vào tài khoản đó bằng mật khẩu đúng
      Then người dùng đăng nhập thành công

  @BR-f00-codebase-008
  Rule: Mỗi người dùng, theo địa chỉ IP thật của họ, được gửi tối đa 10 yêu cầu đăng nhập hoặc gia hạn phiên trong 1 phút

    @api
    Scenario: Yêu cầu thứ 10 trong cùng một phút vẫn được xử lý
      Given một người dùng đã gửi "9" yêu cầu đăng nhập trong phút hiện tại
      When người dùng đó gửi thêm một yêu cầu đăng nhập
      Then yêu cầu được xử lý bình thường

    @api
    Scenario: Yêu cầu thứ 11 trong cùng một phút bị từ chối kèm thời gian chờ
      Given một người dùng đã gửi "10" yêu cầu đăng nhập trong phút hiện tại
      When người dùng đó gửi thêm một yêu cầu đăng nhập
      Then hệ thống từ chối bằng thông báo tiếng Việt
      And người dùng được cho biết thời gian phải chờ

    @integration
    Scenario: Người dùng khác không bị ảnh hưởng khi một người chạm giới hạn
      Given người dùng A đã chạm giới hạn yêu cầu đăng nhập trong phút hiện tại
      And người dùng B ở một địa chỉ khác, cùng đi qua proxy của nhà cung cấp hosting
      When người dùng B gửi một yêu cầu đăng nhập
      Then yêu cầu của người dùng B được xử lý bình thường
