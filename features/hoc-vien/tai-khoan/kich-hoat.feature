@api
Feature: Học viên kích hoạt tài khoản và đặt mật khẩu riêng
  # spec: docs/agentic-workflow/specs/2026-11-25-f01-p1-tai-khoan-hoc-vien.md
  # design: docs/duobexiuuu-coaching-design.html (Main)
  Là học viên vừa nhận thông tin đăng nhập từ coach
  Tôi muốn dùng mật khẩu tạm để đặt mật khẩu riêng của mình
  Để tự quản lý việc đăng nhập và bắt đầu dùng web học viên

  @BR-f01-p1-tai-khoan-hoc-vien-011
  Rule: Đăng nhập bằng mật khẩu tạm còn hiệu lực chỉ cho phép học viên đặt mật khẩu riêng, chưa xem hay ghi được dữ liệu nào khác

    @web
    Scenario: Đăng nhập bằng mật khẩu tạm chỉ dẫn tới việc đặt mật khẩu riêng
      Given một học viên vừa được gửi mật khẩu tạm
      When học viên đăng nhập web học viên bằng mật khẩu tạm đó
      Then học viên được đưa tới bước đặt mật khẩu riêng

    Scenario: Chưa đặt mật khẩu riêng thì chưa xem được dữ liệu
      Given học viên đã đăng nhập bằng mật khẩu tạm và chưa đặt mật khẩu riêng
      When học viên xem hồ sơ của mình
      Then học viên bị từ chối vì cần đặt mật khẩu riêng trước

    Scenario: Mật khẩu tạm không vào được web quản trị
      Given một học viên vừa được gửi mật khẩu tạm
      When học viên đăng nhập web quản trị bằng mật khẩu tạm đó
      Then hệ thống từ chối với thông báo "Email hoặc mật khẩu không đúng, hoặc bạn đã thử quá nhiều lần. Vui lòng thử lại sau vài phút."

  @BR-f01-p1-tai-khoan-hoc-vien-012
  Rule: Đăng nhập bằng mật khẩu tạm không còn hiệu lực bị từ chối với đúng thông báo chung của mọi lần đăng nhập thất bại, không nói lý do

    Scenario Outline: Mật khẩu tạm không còn hiệu lực nhận thông báo đăng nhập thất bại chung
      Given <tinh_huong>
      When học viên đăng nhập web học viên bằng mật khẩu tạm đó
      Then hệ thống từ chối với thông báo "Email hoặc mật khẩu không đúng, hoặc bạn đã thử quá nhiều lần. Vui lòng thử lại sau vài phút."

      Examples:
        | tinh_huong                                                      |
        | một học viên được gửi mật khẩu tạm cách đây "73" giờ             |
        | một học viên có mật khẩu tạm của lần gửi trước, coach đã gửi lại |

  @BR-f01-p1-tai-khoan-hoc-vien-013
  Rule: Mật khẩu riêng, dù đặt lúc kích hoạt hay đặt lại qua link, dài từ 8 đến 128 ký tự

    Scenario: Mật khẩu riêng đúng 8 ký tự được chấp nhận
      Given học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực
      When học viên lưu mật khẩu riêng "abcdefgh"
      Then mật khẩu riêng được lưu

    Scenario: Mật khẩu riêng dưới 8 ký tự bị từ chối
      Given học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực
      When học viên lưu mật khẩu riêng "abcdefg"
      Then mật khẩu riêng không được lưu vì cần ít nhất "8" ký tự

    Scenario: Mật khẩu riêng quá 128 ký tự bị từ chối
      Given học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực
      When học viên lưu một mật khẩu riêng dài "129" ký tự
      Then mật khẩu riêng không được lưu vì dài quá "128" ký tự

  @BR-f01-p1-tai-khoan-hoc-vien-014
  Rule: Mật khẩu riêng, dù đặt lúc kích hoạt hay đặt lại qua link, phải được nhập giống nhau ở hai lần

    Scenario: Hai lần nhập giống nhau thì lưu được
      Given học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực
      When học viên lưu mật khẩu riêng "Coach2026", nhập lại "Coach2026"
      Then mật khẩu riêng được lưu

    Scenario: Hai lần nhập khác nhau thì không lưu
      Given học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực
      When học viên lưu mật khẩu riêng "Coach2026", nhập lại "Coach2027"
      Then mật khẩu riêng không được lưu vì hai lần nhập không khớp
      And tài khoản của học viên vẫn Chờ kích hoạt

  @BR-f01-p1-tai-khoan-hoc-vien-015
  Rule: Mật khẩu riêng được giữ đúng từng ký tự như học viên nhập, kể cả chữ có dấu và khoảng trắng đầu/cuối

    Scenario Outline: Đăng nhập bằng đúng mật khẩu đã đặt
      Given học viên đã kích hoạt tài khoản với mật khẩu riêng <mat_khau>
      When học viên đăng nhập web học viên bằng mật khẩu <mat_khau>
      Then học viên vào được khu vực học viên

      Examples:
        | mat_khau      |
        | "Mậtkhẩu1"    |
        | " Coach2026 " |

    Scenario: Bỏ khoảng trắng đầu/cuối thì không khớp mật khẩu
      Given học viên đã kích hoạt tài khoản với mật khẩu riêng " Coach2026 "
      When học viên đăng nhập web học viên bằng mật khẩu "Coach2026"
      Then đăng nhập bị từ chối

  @BR-f01-p1-tai-khoan-hoc-vien-016
  Rule: Đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực thì tài khoản chuyển sang Đã kích hoạt kèm thời điểm kích hoạt, mật khẩu tạm mất hiệu lực ngay và học viên dùng được dữ liệu của mình

    @web
    Scenario: Đặt mật khẩu riêng thì tài khoản được kích hoạt
      Given hôm nay là "13/10/2026"
      And học viên đang đặt mật khẩu riêng bằng mật khẩu tạm còn hiệu lực
      When học viên lưu mật khẩu riêng "Coach2026"
      Then học viên vào được khu vực học viên
      And coach thấy tài khoản của học viên đó Đã kích hoạt ngày "13/10/2026"

    Scenario: Mật khẩu tạm hết hiệu lực ngay khi kích hoạt
      Given học viên đã kích hoạt tài khoản bằng mật khẩu tạm còn hiệu lực
      When học viên đăng nhập web học viên bằng mật khẩu tạm đó
      Then hệ thống từ chối với thông báo "Email hoặc mật khẩu không đúng, hoặc bạn đã thử quá nhiều lần. Vui lòng thử lại sau vài phút."
