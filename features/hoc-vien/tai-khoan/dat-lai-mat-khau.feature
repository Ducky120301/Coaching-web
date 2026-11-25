@api
Feature: Đặt lại mật khẩu học viên qua link
  # spec: docs/agentic-workflow/specs/2026-11-25-f01-p1-tai-khoan-hoc-vien.md
  # design: docs/duobexiuuu-coaching-design.html (Main, AdHoSo)
  Là học viên đã kích hoạt tài khoản
  Tôi muốn đặt lại mật khẩu bằng link do coach gửi hoặc tự xin qua Quên mật khẩu
  Để vào lại được web học viên khi quên mật khẩu, mà người khác không dò ra được email nào có tài khoản

  @BR-f01-p1-tai-khoan-hoc-vien-017
  Rule: Coach chỉ gửi được link đặt lại mật khẩu cho tài khoản Đã kích hoạt, ở mọi trạng thái coaching

    Scenario Outline: Coach gửi link đặt lại ở mọi trạng thái coaching
      Given coach "Coach Duobexiuuu" đã đăng nhập
      And một học viên có tài khoản Đã kích hoạt, coaching "<trang_thai_coaching>"
      When coach gửi link đặt lại mật khẩu cho học viên đó
      Then học viên đó nhận được email có link đặt lại mật khẩu

      Examples:
        | trang_thai_coaching |
        | Tạm dừng            |
        | Đã kết thúc         |

    Scenario: Không gửi link đặt lại cho tài khoản chưa kích hoạt
      Given coach "Coach Duobexiuuu" đã đăng nhập
      And một hồ sơ học viên tài khoản Chờ kích hoạt
      When coach gửi link đặt lại mật khẩu cho học viên đó
      Then link đặt lại không được gửi vì tài khoản chưa kích hoạt
      And coach được hướng dẫn gửi thông tin đăng nhập thay thế

  @BR-f01-p1-tai-khoan-hoc-vien-018
  Rule: Link đặt lại mật khẩu hết hiệu lực khi đủ 30 phút kể từ lúc tạo

    Scenario: Link còn hạn thì đặt lại được
      Given học viên nhận link đặt lại mật khẩu cách đây "20" phút
      When học viên đặt lại mật khẩu "Coach2026" bằng link đó
      Then học viên đăng nhập được bằng mật khẩu "Coach2026"

    Scenario: Link hết hiệu lực ở đúng 30 phút
      Given học viên nhận link đặt lại mật khẩu cách đây đúng "30" phút
      When học viên đặt lại mật khẩu "Coach2026" bằng link đó
      Then mật khẩu không được đặt lại vì link đã hết hiệu lực

  @BR-f01-p1-tai-khoan-hoc-vien-019
  Rule: Link đặt lại mật khẩu chỉ đặt lại thành công được một lần

    Scenario: Link đã đặt lại thành công không dùng lại được
      Given học viên đã đặt lại mật khẩu thành công bằng một link còn hạn
      When học viên đặt lại mật khẩu "Coach2027" bằng link đó
      Then mật khẩu không được đặt lại vì link đã hết hiệu lực

    Scenario: Lần đặt lại không hợp lệ không làm mất link
      Given học viên đã dùng một link còn hạn để đặt mật khẩu "abc123" và bị từ chối
      When học viên đặt lại mật khẩu "Coach2026" bằng link đó
      Then học viên đăng nhập được bằng mật khẩu "Coach2026"

  @BR-f01-p1-tai-khoan-hoc-vien-020
  Rule: Khi tài khoản có link đặt lại mới, mọi link đặt lại trước đó của tài khoản mất hiệu lực

    Scenario: Link mới nhất dùng được
      Given học viên đã nhận link đặt lại mật khẩu hai lần
      When học viên đặt lại mật khẩu "Coach2026" bằng link lần hai
      Then học viên đăng nhập được bằng mật khẩu "Coach2026"

    Scenario: Link cũ mất hiệu lực khi đã có link mới
      Given học viên đã nhận link đặt lại mật khẩu hai lần
      When học viên đặt lại mật khẩu "Coach2026" bằng link lần một
      Then mật khẩu không được đặt lại vì link đã hết hiệu lực

  @BR-f01-p1-tai-khoan-hoc-vien-021
  Rule: Đặt lại mật khẩu xong, mọi phiên đăng nhập đang có của học viên kết thúc và chỉ mật khẩu mới đăng nhập được

    Scenario: Đặt lại mật khẩu kết thúc phiên trên thiết bị khác
      Given học viên đang đăng nhập web học viên trên điện thoại
      When học viên đặt lại mật khẩu bằng link trên máy tính
      Then phiên đăng nhập trên điện thoại kết thúc, học viên phải đăng nhập lại

    Scenario: Mật khẩu cũ không còn đăng nhập được
      Given học viên đã đặt lại mật khẩu từ "Coach2025" sang "Coach2026"
      When học viên đăng nhập web học viên bằng mật khẩu "Coach2025"
      Then đăng nhập bị từ chối

  @BR-f01-p1-tai-khoan-hoc-vien-022
  Rule: Quên mật khẩu chỉ gửi link đặt lại cho tài khoản học viên Đã kích hoạt

    @web
    Scenario: Học viên đã kích hoạt nhận được link đặt lại
      Given học viên có tài khoản Đã kích hoạt với email "quan@gmail.com"
      When một người xin đặt lại mật khẩu cho email "QUAN@gmail.com "
      Then email "quan@gmail.com" nhận được email có link đặt lại mật khẩu

    Scenario Outline: Không gửi link cho email không phải học viên đã kích hoạt
      Given email "<email>" là <tinh_huong>
      When một người xin đặt lại mật khẩu cho email "<email>"
      Then không email nào được gửi tới "<email>"

      Examples:
        | email              | tinh_huong                           |
        | la@gmail.com       | email chưa có tài khoản nào          |
        | moi@gmail.com      | email của học viên còn Chờ kích hoạt |
        | coach@gmail.com    | email của tài khoản coach            |

  @BR-f01-p1-tai-khoan-hoc-vien-023
  Rule: Kết quả trả về của Quên mật khẩu giống hệt nhau trong mọi trường hợp

    Scenario Outline: Mọi trường hợp nhận cùng một thông báo
      Given email "<email>" là <tinh_huong>
      When một người xin đặt lại mật khẩu cho email "<email>"
      Then người đó nhận thông báo chung của Quên mật khẩu, không phân biệt được email có tài khoản hay không

      Examples:
        | email              | tinh_huong                           |
        | quan@gmail.com     | email của học viên Đã kích hoạt      |
        | la@gmail.com       | email chưa có tài khoản nào          |
        | moi@gmail.com      | email của học viên còn Chờ kích hoạt |
        | coach@gmail.com    | email của tài khoản coach            |

  @BR-f01-p1-tai-khoan-hoc-vien-024
  Rule: Mỗi tài khoản nhận tối đa một email đặt lại từ Quên mật khẩu trong mỗi 60 giây

    Scenario: Xin lại sau hơn 60 giây thì nhận email mới
      Given học viên Đã kích hoạt đã xin đặt lại mật khẩu cách đây "2" phút
      When học viên xin đặt lại mật khẩu lần nữa
      Then học viên nhận được email đặt lại thứ hai

    Scenario: Xin nhiều lần trong 60 giây chỉ nhận một email
      Given học viên Đã kích hoạt đã xin đặt lại mật khẩu "4" lần trong "30" giây vừa qua
      When học viên xin đặt lại mật khẩu lần thứ "5"
      Then học viên chỉ nhận được "1" email đặt lại
      And lần xin đó vẫn nhận thông báo chung của Quên mật khẩu
