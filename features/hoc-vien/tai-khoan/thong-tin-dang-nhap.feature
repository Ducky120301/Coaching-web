@api
Feature: Coach gửi thông tin đăng nhập và quản lý email tài khoản học viên
  # spec: docs/agentic-workflow/specs/2026-11-25-f01-p1-tai-khoan-hoc-vien.md
  # design: docs/duobexiuuu-coaching-design.html (AdTaoHoSo, AdHoSo)
  Là coach của Duobexiuuu Coaching
  Tôi muốn gửi, gửi lại thông tin đăng nhập, sửa email gõ sai và theo dõi tình trạng tài khoản của học viên
  Để học viên vào được hệ thống mà tôi không cần biết mật khẩu riêng của họ

  Background:
    Given coach "Coach Duobexiuuu" đã đăng nhập

  @BR-f01-p1-tai-khoan-hoc-vien-001
  Rule: Thông tin đăng nhập chỉ gửi được cho tài khoản học viên đang Chờ kích hoạt, ở mọi trạng thái coaching

    Scenario Outline: Gửi được thông tin đăng nhập ở mọi trạng thái coaching
      Given một hồ sơ học viên đủ mục bắt buộc, tài khoản Chờ kích hoạt, coaching "<trang_thai_coaching>"
      When coach gửi thông tin đăng nhập cho học viên đó
      Then học viên đó nhận được email thông tin đăng nhập

      Examples:
        | trang_thai_coaching |
        | Đang coaching       |
        | Tạm dừng            |
        | Đã kết thúc         |

    Scenario: Không gửi thông tin đăng nhập cho tài khoản đã kích hoạt
      Given một học viên có tài khoản Đã kích hoạt
      When coach gửi thông tin đăng nhập cho học viên đó
      Then thông tin đăng nhập không được gửi vì tài khoản đã kích hoạt
      And coach được hướng dẫn gửi link đặt lại mật khẩu thay thế

  @BR-f01-p1-tai-khoan-hoc-vien-002
  Rule: Lần gửi thông tin đăng nhập đầu tiên của một hồ sơ chỉ được khi hồ sơ đã điền đủ các mục bắt buộc

    @web
    Scenario: Gửi lần đầu khi hồ sơ đủ mục bắt buộc
      Given một hồ sơ học viên đủ mục bắt buộc, chưa gửi thông tin đăng nhập lần nào
      When coach gửi thông tin đăng nhập cho học viên đó
      Then học viên đó nhận được email thông tin đăng nhập
      And coach thấy tài khoản đã được gửi thông tin đăng nhập

    Scenario: Hồ sơ còn thiếu mục bắt buộc thì chưa gửi lần đầu được
      Given một hồ sơ học viên còn "3" mục bắt buộc trống, chưa gửi thông tin đăng nhập lần nào
      When coach gửi thông tin đăng nhập cho học viên đó
      Then thông tin đăng nhập không được gửi vì còn "3" mục bắt buộc chưa điền
      And hồ sơ đó vẫn được lưu

    Scenario: Gửi lại không cần hồ sơ đủ mục bắt buộc
      Given một hồ sơ học viên đã được gửi thông tin đăng nhập
      And sau đó coach để trống một mục bắt buộc của hồ sơ đó
      When coach gửi lại thông tin đăng nhập cho học viên đó
      Then học viên đó nhận được email thông tin đăng nhập mới

  @BR-f01-p1-tai-khoan-hoc-vien-003
  Rule: Mỗi lần gửi tạo một mật khẩu tạm mới, và email gửi học viên gồm địa chỉ đăng nhập web học viên, email tài khoản và mật khẩu tạm; địa chỉ đăng nhập không chứa mật khẩu hay mã bí mật nào

    Scenario: Email thông tin đăng nhập có đủ thông tin để học viên vào hệ thống
      Given một hồ sơ học viên đủ mục bắt buộc với email "quan@gmail.com", tài khoản Chờ kích hoạt
      When coach gửi thông tin đăng nhập cho học viên đó
      Then email "quan@gmail.com" nhận được địa chỉ đăng nhập web học viên, email tài khoản "quan@gmail.com", một mật khẩu tạm và hạn dùng "72" giờ
      And địa chỉ đăng nhập trong email không chứa mật khẩu tạm hay mã bí mật nào

  @BR-f01-p1-tai-khoan-hoc-vien-004
  Rule: Mật khẩu tạm hết hiệu lực khi đủ 72 giờ kể từ lúc gửi

    Scenario: Mật khẩu tạm còn dùng được ngay trước mốc 72 giờ
      Given học viên được gửi mật khẩu tạm cách đây "71" giờ "59" phút
      When học viên đăng nhập web học viên bằng mật khẩu tạm đó
      Then học viên được đưa tới bước đặt mật khẩu riêng

    Scenario: Mật khẩu tạm hết hiệu lực ở đúng 72 giờ
      Given học viên được gửi mật khẩu tạm cách đây đúng "72" giờ
      When học viên đăng nhập web học viên bằng mật khẩu tạm đó
      Then đăng nhập bị từ chối

  @BR-f01-p1-tai-khoan-hoc-vien-005
  Rule: Mỗi lần gửi lại, mật khẩu tạm trước đó mất hiệu lực, kể cả khi học viên đang ở bước đặt mật khẩu riêng bằng mật khẩu tạm cũ

    Scenario: Mật khẩu tạm của lần gửi sau cùng dùng được
      Given coach đã gửi thông tin đăng nhập cho học viên hai lần liên tiếp
      When học viên đăng nhập web học viên bằng mật khẩu tạm của lần gửi sau cùng
      Then học viên được đưa tới bước đặt mật khẩu riêng

    Scenario: Mật khẩu tạm của lần gửi trước mất hiệu lực
      Given coach đã gửi thông tin đăng nhập cho học viên hai lần liên tiếp
      When học viên đăng nhập web học viên bằng mật khẩu tạm của lần gửi trước
      Then đăng nhập bị từ chối

    Scenario: Đang đặt mật khẩu riêng bằng mật khẩu tạm cũ thì không lưu được
      Given học viên đã đăng nhập bằng mật khẩu tạm và đang ở bước đặt mật khẩu riêng
      And sau đó coach gửi lại thông tin đăng nhập cho học viên đó
      When học viên lưu một mật khẩu riêng hợp lệ
      Then mật khẩu riêng không được lưu
      And học viên phải đăng nhập lại bằng mật khẩu tạm mới

  @BR-f01-p1-tai-khoan-hoc-vien-006
  Rule: Khi email thông tin đăng nhập không gửi được, tài khoản và lịch sử không thay đổi gì và coach được báo là chưa gửi được

    Scenario: Gửi lại không thành công thì tài khoản giữ nguyên
      Given một học viên đã được gửi thông tin đăng nhập
      And dịch vụ email đang không gửi được thư
      When coach gửi lại thông tin đăng nhập cho học viên đó
      Then coach được báo thông tin đăng nhập chưa gửi được
      And mật khẩu tạm đã gửi trước đó vẫn dùng được
      And lịch sử tài khoản của học viên đó không có thêm dòng nào

    Scenario: Gửi lần đầu không thành công thì hồ sơ vẫn là chưa gửi
      Given một hồ sơ học viên đủ mục bắt buộc, chưa gửi thông tin đăng nhập lần nào
      And dịch vụ email đang không gửi được thư
      When coach gửi thông tin đăng nhập cho học viên đó
      Then coach được báo thông tin đăng nhập chưa gửi được
      And coach thấy tài khoản vẫn chưa được gửi thông tin đăng nhập

  @BR-f01-p1-tai-khoan-hoc-vien-007
  Rule: Email của học viên chỉ sửa được khi tài khoản đang Chờ kích hoạt

    @web
    Scenario: Sửa email khi tài khoản chưa kích hoạt
      Given một hồ sơ học viên với email "quan@gmial.com", tài khoản Chờ kích hoạt
      And chưa có tài khoản nào dùng email "quan@gmail.com"
      When coach sửa email của học viên đó thành "quan@gmail.com"
      Then hồ sơ đó có email "quan@gmail.com"

    Scenario: Không sửa được email của tài khoản đã kích hoạt
      Given một học viên có tài khoản Đã kích hoạt
      When coach sửa email của học viên đó sang một email chưa ai dùng
      Then email của học viên đó không đổi vì tài khoản đã kích hoạt

  @BR-f01-p1-tai-khoan-hoc-vien-008
  Rule: Email mới khi sửa phải chưa thuộc tài khoản nào khác, không phân biệt hoa/thường và bỏ khoảng trắng đầu/cuối

    Scenario Outline: Không sửa sang email đã thuộc người khác
      Given một học viên đã được gửi thông tin đăng nhập, tài khoản Chờ kích hoạt
      And email "quan@gmail.com" đã thuộc về <chu_so_huu>
      When coach sửa email của học viên đó thành "<email_nhap>"
      Then email của học viên đó không đổi vì email đã được dùng
      And mật khẩu tạm đã gửi cho học viên đó vẫn dùng được

      Examples:
        | chu_so_huu          | email_nhap       |
        | một hồ sơ khác      | QUAN@Gmail.com   |
        | tài khoản coach     | quan@gmail.com   |

    Scenario: Không sửa sang email sai định dạng
      Given một hồ sơ học viên tài khoản Chờ kích hoạt
      When coach sửa email của học viên đó thành "quan@"
      Then email của học viên đó không đổi vì email không hợp lệ

  @BR-f01-p1-tai-khoan-hoc-vien-009
  Rule: Sửa email của hồ sơ đã từng được gửi thông tin đăng nhập thì mật khẩu tạm cũ mất hiệu lực và thông tin đăng nhập mới được gửi tới email mới

    Scenario: Sửa email của hồ sơ đã gửi thì thông tin mới tới email mới
      Given một học viên với email "quan@gmial.com" đã được gửi thông tin đăng nhập
      When coach sửa email của học viên đó thành "quan@gmail.com"
      Then email "quan@gmail.com" nhận được email thông tin đăng nhập mới

    Scenario Outline: Mật khẩu tạm cũ mất hiệu lực sau khi sửa email
      Given một học viên với email "quan@gmial.com" đã được gửi thông tin đăng nhập
      And coach đã sửa email của học viên đó thành "quan@gmail.com"
      When học viên đăng nhập web học viên bằng email "<email_dang_nhap>" và mật khẩu tạm cũ
      Then đăng nhập bị từ chối

      Examples:
        | email_dang_nhap |
        | quan@gmial.com  |
        | quan@gmail.com  |

    Scenario: Gửi tới email mới không thành công thì hồ sơ giữ email cũ
      Given một học viên với email "quan@gmial.com" đã được gửi thông tin đăng nhập
      And dịch vụ email đang không gửi được thư
      When coach sửa email của học viên đó thành "quan@gmail.com"
      Then coach được báo thông tin đăng nhập chưa gửi được
      And hồ sơ đó vẫn có email "quan@gmial.com"
      And mật khẩu tạm đã gửi trước đó vẫn dùng được

  @BR-f01-p1-tai-khoan-hoc-vien-010
  Rule: Sửa email của hồ sơ chưa từng được gửi thông tin đăng nhập thì chỉ đổi email, không gửi email nào

    Scenario: Sửa email của hồ sơ chưa gửi thì không gửi email nào
      Given một hồ sơ học viên với email "quan@gmial.com", chưa gửi thông tin đăng nhập lần nào
      When coach sửa email của học viên đó thành "quan@gmail.com"
      Then hồ sơ đó có email "quan@gmail.com"
      And không email nào được gửi cho học viên đó

    Scenario: Lần gửi đầu sau khi sửa email đi tới email mới
      Given một hồ sơ học viên đủ mục bắt buộc, chưa gửi thông tin đăng nhập lần nào
      And coach đã sửa email của hồ sơ đó thành "quan@gmail.com"
      When coach gửi thông tin đăng nhập cho học viên đó
      Then email "quan@gmail.com" nhận được email thông tin đăng nhập

  @BR-f01-p1-tai-khoan-hoc-vien-025
  Rule: Không dữ liệu nào hệ thống trả ra, cho coach hay học viên, chứa mật khẩu tạm, mật khẩu riêng hoặc mã đặt lại mật khẩu

    Scenario: Coach không bao giờ thấy mật khẩu tạm
      Given coach vừa gửi thông tin đăng nhập cho một học viên
      When coach xem hồ sơ của học viên đó
      Then coach thấy trạng thái tài khoản của học viên đó
      And coach không thấy mật khẩu tạm nào của học viên đó

  @BR-f01-p1-tai-khoan-hoc-vien-026
  Rule: Coach thấy được trạng thái tài khoản của học viên gồm chưa gửi, đã gửi kèm thời điểm gửi gần nhất và hạn mật khẩu tạm, mật khẩu tạm đã hết hạn, hoặc đã kích hoạt kèm thời điểm kích hoạt

    Scenario Outline: Coach thấy trạng thái tài khoản của học viên
      Given <tinh_huong>
      When coach xem hồ sơ của học viên đó
      Then coach thấy trạng thái tài khoản "<trang_thai_tai_khoan>"

      Examples:
        | tinh_huong                                                         | trang_thai_tai_khoan                                         |
        | một học viên chưa được gửi thông tin đăng nhập lần nào             | Chưa gửi thông tin đăng nhập                                 |
        | một học viên được gửi thông tin đăng nhập lúc 01/11 09:00, bây giờ là 02/11 09:00 | Đã gửi lúc 01/11 09:00, mật khẩu tạm còn hạn đến 04/11 09:00 |
        | một học viên được gửi thông tin đăng nhập lúc 01/11 09:00, bây giờ là 05/11 09:00 | Mật khẩu tạm đã hết hạn                                      |
        | một học viên kích hoạt tài khoản ngày 13/10/2026                    | Đã kích hoạt ngày 13/10/2026                                 |

  @BR-f01-p1-tai-khoan-hoc-vien-027
  Rule: Mỗi lần coach gửi thông tin lần đầu, gửi lại, sửa email, gửi link đặt lại và mỗi lần học viên kích hoạt được ghi thêm đúng một dòng lịch sử tài khoản kèm thời điểm; lịch sử chỉ thêm, không sửa, không xóa

    Scenario: Lịch sử ghi lại các lần gửi và kích hoạt theo thứ tự thời gian
      Given coach đã gửi thông tin đăng nhập lần đầu, rồi gửi lại cho một học viên
      When học viên đó kích hoạt tài khoản bằng mật khẩu tạm mới nhất
      Then lịch sử tài khoản của học viên đó có "3" dòng theo thứ tự: gửi lần đầu, gửi lại kèm ghi chú mật khẩu tạm cũ bị hủy, học viên đặt mật khẩu riêng

    Scenario: Sửa email được ghi vào lịch sử
      Given một học viên với email "quan@gmial.com" đã được gửi thông tin đăng nhập
      When coach sửa email của học viên đó thành "quan@gmail.com"
      Then lịch sử tài khoản của học viên đó có thêm dòng sửa email từ "quan@gmial.com" sang "quan@gmail.com"
