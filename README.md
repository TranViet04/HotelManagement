# HotelManagement
Dưới đây là **tiến trình chia việc cho 3 người theo 3 role**. Mục tiêu là cả 3 làm độc lập tối đa, cuối cùng ghép lại thành một luồng hoàn chỉnh:

```text
Customer đặt phòng
↓
Receptionist xác nhận, check-in, thêm dịch vụ, check-out, thanh toán
↓
Admin quản lý dữ liệu nền và xem báo cáo
```

# 1. Trạng thái project trước khi chia việc

Trước khi 3 người bắt đầu làm riêng, branch `develop` phải có sẵn:

```text
Đăng nhập / đăng ký / đăng xuất
Phân quyền Admin / Receptionist / Customer
Database 9 bảng V1
Seed dữ liệu mẫu
Layout theo quyền
Menu placeholder không lỗi 404
```

Database V1:

```text
Users
RoomTypes
Rooms
Bookings
Services
BookingServices
Invoices
Payments
ActivityLogs
```

Layout V1:

```text
_AdminMenu.cshtml
_ReceptionistMenu.cshtml
_CustomerMenu.cshtml
```

---

# 2. Cấu trúc branch GitHub

Tạo các branch sau:

```text
main
develop
feature/admin-module
feature/receptionist-module
feature/customer-module
```

Quy tắc:

```text
main: chỉ chứa bản ổn định
develop: nơi tổng hợp code
feature/admin-module: người 1 làm
feature/receptionist-module: người 2 làm
feature/customer-module: người 3 làm
```

Không ai push trực tiếp vào `main`.

Mỗi người làm xong một mốc nhỏ thì tạo Pull Request vào `develop`.

---

# 3. Phân công tổng quát

| Người   | Role phụ trách  | Mục tiêu chính                                                |
| ------- | --------------- | ------------------------------------------------------------- |
| Người 1 | Admin           | Quản lý dữ liệu nền, nhân viên, báo cáo                       |
| Người 2 | Receptionist    | Vận hành đặt phòng, check-in/out, hóa đơn, thanh toán         |
| Người 3 | Customer/Public | Trang chủ, xem phòng, tìm phòng, đặt phòng, lịch sử đặt phòng |

---

# 4. Người 1 — Admin Module

## 4.1. Phạm vi

Người 1 phụ trách toàn bộ phần quản trị hệ thống.

```text
Admin Dashboard
Quản lý loại phòng
Quản lý phòng
Quản lý dịch vụ
Quản lý nhân viên
Quản lý khách hàng
Theo dõi đặt phòng
Theo dõi hóa đơn
Báo cáo doanh thu
Nhật ký hoạt động
```

## 4.2. File được sửa chính

```text
Controllers/AdminController.cs
Views/Admin/**
Views/Shared/RoleMenus/_AdminMenu.cshtml
Services/Admin/**
ViewModels/Admin/**
```

Nếu chưa có thư mục thì tạo:

```text
Services/Admin
ViewModels/Admin
```

## 4.3. Không tự ý sửa

```text
ReceptionistController.cs
BookingsController.cs
RoomsController.cs
Views/Receptionist/**
Views/Bookings/**
Views/Rooms/**
HotelDbContext.cs
Models/**
Migrations/**
```

---

## 4.4. Tiến trình làm Admin

### Admin-M1: Dashboard Admin

Làm trước:

```text
Hiển thị tổng số phòng
Hiển thị số phòng trống
Hiển thị số phòng đang sử dụng
Hiển thị số booking hôm nay
Hiển thị doanh thu hôm nay
```

File:

```text
Controllers/AdminController.cs
Views/Admin/Dashboard.cshtml
Services/Admin/AdminDashboardService.cs
ViewModels/Admin/AdminDashboardViewModel.cs
```

Điều kiện hoàn thành:

```text
Admin đăng nhập vào được Dashboard
Customer không vào được
Receptionist không vào được
Dashboard không lỗi khi database chưa có booking
```

---

### Admin-M2: Quản lý loại phòng

Chức năng:

```text
Danh sách loại phòng
Thêm loại phòng
Sửa loại phòng
Ngưng sử dụng loại phòng
```

Dữ liệu:

```text
Name
Description
Price
Capacity
BedType
ThumbnailUrl
Status
```

File:

```text
Views/Admin/RoomTypes.cshtml
Views/Admin/CreateRoomType.cshtml
Views/Admin/EditRoomType.cshtml
Services/Admin/RoomTypeManagementService.cs
ViewModels/Admin/RoomTypeViewModel.cs
```

Điều kiện hoàn thành:

```text
Thêm được loại phòng
Sửa được loại phòng
Không xóa cứng, chỉ đổi Status = Inactive
Danh sách loại phòng hiện đúng dữ liệu
```

---

### Admin-M3: Quản lý phòng

Chức năng:

```text
Danh sách phòng
Thêm phòng
Sửa phòng
Cập nhật trạng thái phòng
```

Dữ liệu:

```text
RoomNumber
RoomTypeId
Floor
Status
Note
```

Điều kiện hoàn thành:

```text
Không cho trùng RoomNumber
Không cho tạo phòng nếu RoomType không tồn tại
Có thể chuyển phòng sang Maintenance hoặc Inactive
```

---

### Admin-M4: Quản lý dịch vụ

Chức năng:

```text
Danh sách dịch vụ
Thêm dịch vụ
Sửa dịch vụ
Ẩn dịch vụ
```

Dữ liệu:

```text
Name
Category
Unit
Price
Status
```

Điều kiện hoàn thành:

```text
Không cho giá âm
Không xóa cứng dịch vụ đã từng được dùng
Dịch vụ Inactive không hiện cho lễ tân chọn
```

---

### Admin-M5: Quản lý nhân viên và khách hàng

Chức năng:

```text
Xem danh sách nhân viên
Tạo tài khoản lễ tân
Khóa / mở khóa tài khoản nhân viên
Xem danh sách khách hàng
```

Quy tắc:

```text
Admin chỉ tạo được Receptionist
Không cho tạo thêm Admin từ UI
Customer tự đăng ký
```

Điều kiện hoàn thành:

```text
Tài khoản nhân viên tạo mới đăng nhập được
Password phải hash
Tài khoản bị Locked không đăng nhập được
```

---

### Admin-M6: Báo cáo và ActivityLogs

Chức năng:

```text
Doanh thu theo ngày
Doanh thu theo tháng
Số booking theo trạng thái
Danh sách ActivityLogs
```

Điều kiện hoàn thành:

```text
Báo cáo lấy từ Invoices/Payments
Không lỗi khi chưa có dữ liệu
ActivityLogs hiển thị được thao tác quan trọng
```

---

# 5. Người 2 — Receptionist Module

## 5.1. Phạm vi

Người 2 phụ trách vận hành khách sạn hằng ngày.

```text
Receptionist Dashboard
Xem booking
Tạo booking tại quầy
Xác nhận booking
Check-in
Thêm dịch vụ phát sinh
Check-out
Tạo hóa đơn
Ghi nhận thanh toán
```

## 5.2. File được sửa chính

```text
Controllers/ReceptionistController.cs
Views/Receptionist/**
Views/Shared/RoleMenus/_ReceptionistMenu.cshtml
Services/Receptionist/**
ViewModels/Receptionist/**
```

Nếu chưa có thư mục thì tạo:

```text
Services/Receptionist
ViewModels/Receptionist
```

## 5.3. Không tự ý sửa

```text
AdminController.cs
RoomsController.cs
BookingsController.cs
Views/Admin/**
Views/Rooms/**
Views/Bookings/**
HotelDbContext.cs
Models/**
Migrations/**
```

---

## 5.4. Tiến trình làm Receptionist

### Receptionist-M1: Dashboard lễ tân

Hiển thị:

```text
Booking hôm nay
Khách sắp check-in
Khách sắp check-out
Phòng đang sử dụng
Phòng đang cần dọn
```

File:

```text
Controllers/ReceptionistController.cs
Views/Receptionist/Dashboard.cshtml
Services/Receptionist/ReceptionistDashboardService.cs
ViewModels/Receptionist/ReceptionistDashboardViewModel.cs
```

Điều kiện hoàn thành:

```text
Receptionist đăng nhập vào được Dashboard
Customer không vào được
Dashboard không lỗi khi chưa có booking
```

---

### Receptionist-M2: Danh sách và chi tiết booking

Chức năng:

```text
Xem danh sách booking
Lọc theo trạng thái
Tìm theo BookingCode
Tìm theo tên khách
Xem chi tiết booking
```

Hiển thị:

```text
BookingCode
CustomerName
RoomNumber
CheckInDate
CheckOutDate
Status
TotalAmount
```

Điều kiện hoàn thành:

```text
Không sửa nhầm booking của người khác bằng URL
Booking detail hiển thị đủ khách, phòng, tiền phòng, dịch vụ
```

---

### Receptionist-M3: Tạo booking tại quầy

Chức năng:

```text
Chọn khách hàng có sẵn hoặc nhập khách mới
Chọn ngày nhận/trả phòng
Chọn phòng trống
Tạo booking
```

Quy tắc:

```text
CheckOutDate > CheckInDate
Không cho đặt ngày quá khứ
Không cho chọn phòng Maintenance/Inactive
Không cho đặt trùng lịch
```

Điều kiện hoàn thành:

```text
Tạo booking tại quầy thành công
BookingSource có thể để mặc định hoặc ghi chú là Reception
Booking.Status = Confirmed hoặc Pending, thống nhất nhóm chọn một kiểu
```

Khuyến nghị V1:

```text
Booking tại quầy → Confirmed
Booking online của Customer → Pending
```

---

### Receptionist-M4: Xác nhận booking

Chức năng:

```text
Xác nhận booking Pending
Hủy booking nếu hợp lệ
```

Quy tắc:

```text
Chỉ Pending mới được Confirm
CheckedIn/CheckedOut không được hủy
```

Sau xác nhận:

```text
Booking.Status = Confirmed
ConfirmedAt = DateTime.Now
Ghi ActivityLogs
```

---

### Receptionist-M5: Check-in

Điều kiện:

```text
Booking.Status = Confirmed
Room.Status = Available
Booking chưa bị hủy
```

Sau check-in:

```text
Booking.Status = CheckedIn
Booking.CheckedInAt = DateTime.Now
Room.Status = Occupied
Ghi ActivityLogs
```

Điều kiện hoàn thành:

```text
Không check-in được booking Pending
Không check-in được booking Cancelled
Không check-in được phòng Maintenance
```

---

### Receptionist-M6: Thêm dịch vụ phát sinh

Chức năng:

```text
Chọn booking đang CheckedIn
Chọn dịch vụ
Nhập số lượng
Tính TotalPrice
Lưu BookingServices
Cập nhật Booking.TotalServiceAmount
Cập nhật Booking.TotalAmount
```

Điều kiện:

```text
Chỉ booking CheckedIn mới được thêm dịch vụ
Không cho Quantity <= 0
Không cho thêm Service Inactive
```

---

### Receptionist-M7: Check-out và tạo hóa đơn

Điều kiện:

```text
Booking.Status = CheckedIn
Booking chưa có Invoice
```

Sau check-out:

```text
Tính RoomAmount
Tính ServiceAmount
Tạo Invoice
Booking.Status = CheckedOut
Booking.CheckedOutAt = DateTime.Now
Room.Status = Cleaning hoặc Available
Ghi ActivityLogs
```

Khuyến nghị V1:

```text
Sau check-out → Room.Status = Cleaning
Admin hoặc lễ tân đổi lại Available sau
```

---

### Receptionist-M8: Thanh toán

Chức năng:

```text
Xem hóa đơn
Chọn phương thức Cash hoặc BankTransfer
Nhập số tiền thanh toán
Tạo Payment
Cập nhật Invoice.PaidAmount
Cập nhật Invoice.RemainingAmount
Cập nhật Invoice.Status
```

Quy tắc:

```text
Không thanh toán hóa đơn Cancelled
Không cho Amount <= 0
Không cho thanh toán vượt RemainingAmount
Nếu RemainingAmount = 0 → Invoice.Status = Paid
Nếu PaidAmount > 0 và còn nợ → PartiallyPaid
```

---

# 6. Người 3 — Customer/Public Module

## 6.1. Phạm vi

Người 3 phụ trách giao diện công khai và khách hàng.

```text
Trang chủ
Danh sách phòng
Tìm phòng trống
Chi tiết phòng
Đặt phòng online
Lịch sử đặt phòng
Hủy booking
Thông tin cá nhân
```

## 6.2. File được sửa chính

```text
Controllers/HomeController.cs
Controllers/RoomsController.cs
Controllers/BookingsController.cs
Controllers/CustomerController.cs
Views/Home/**
Views/Rooms/**
Views/Bookings/**
Views/Customer/**
Views/Shared/RoleMenus/_CustomerMenu.cshtml
Services/Customer/**
ViewModels/Customer/**
```

Nếu chưa có thư mục thì tạo:

```text
Services/Customer
ViewModels/Customer
```

## 6.3. Không tự ý sửa

```text
AdminController.cs
ReceptionistController.cs
Views/Admin/**
Views/Receptionist/**
HotelDbContext.cs
Models/**
Migrations/**
```

---

## 6.4. Tiến trình làm Customer

### Customer-M1: Trang chủ public

Trang chủ gồm:

```text
Banner giới thiệu khách sạn
Form tìm phòng
Danh sách loại phòng nổi bật
Dịch vụ nổi bật
Thông tin liên hệ
```

File:

```text
Controllers/HomeController.cs
Views/Home/Index.cshtml
Services/Customer/PublicHomeService.cs
ViewModels/Customer/HomeViewModel.cs
```

Điều kiện hoàn thành:

```text
Guest xem được trang chủ
Customer xem được trang chủ
Không cần đăng nhập vẫn xem được
```

---

### Customer-M2: Danh sách phòng

Chức năng:

```text
Xem danh sách loại phòng/phòng
Xem giá
Xem sức chứa
Xem mô tả
Xem ảnh đại diện nếu có
```

Hiển thị:

```text
Name
Price
Capacity
BedType
ThumbnailUrl
Description
```

Điều kiện hoàn thành:

```text
Chỉ hiển thị RoomTypes Active
Không hiển thị phòng Inactive/Maintenance như phòng có thể đặt
```

---

### Customer-M3: Tìm phòng trống

Form:

```text
CheckInDate
CheckOutDate
Adults
Children
```

Quy tắc:

```text
CheckOutDate > CheckInDate
CheckInDate >= hôm nay
Không hiện phòng Maintenance
Không hiện phòng Inactive
Không hiện phòng đã trùng lịch
Sức chứa phòng >= Adults + Children nếu dùng Capacity đơn giản
```

Logic trùng lịch:

```text
Phòng bị bận nếu có booking cùng RoomId
và Status thuộc Pending, Confirmed, CheckedIn
và khoảng ngày bị giao nhau
```

Điều kiện hoàn thành:

```text
Tìm phòng với ngày hợp lệ ra kết quả
Ngày sai báo lỗi
Phòng đã có booking không hiện lại
```

---

### Customer-M4: Chi tiết phòng

Hiển thị:

```text
Tên loại phòng
Giá
Sức chứa
Loại giường
Mô tả
Ảnh đại diện
Danh sách phòng còn trống nếu có ngày tìm kiếm
Nút đặt phòng
```

Điều kiện hoàn thành:

```text
Guest xem được chi tiết
Nút đặt phòng yêu cầu đăng nhập nếu chưa login
```

---

### Customer-M5: Đặt phòng online

Chức năng:

```text
Customer chọn phòng
Nhập ngày nhận/trả
Nhập số người
Nhập yêu cầu đặc biệt
Tạo booking
```

Quy tắc:

```text
Chỉ Customer mới đặt online
Không cho Admin/Receptionist đặt qua flow Customer nếu không cần
Không cho đặt trùng lịch
Không cho đặt phòng Maintenance/Inactive
```

Sau đặt phòng:

```text
Booking.Status = Pending
CreatedByUserId = CustomerId
TotalRoomAmount = số đêm * giá phòng
TotalAmount = TotalRoomAmount
```

Điều kiện hoàn thành:

```text
Đặt phòng thành công
Booking xuất hiện trong MyBookings
Receptionist thấy booking trong danh sách để xác nhận
```

---

### Customer-M6: Lịch sử đặt phòng

Khách xem:

```text
BookingCode
RoomNumber
RoomTypeName
CheckInDate
CheckOutDate
TotalAmount
Status
```

Quy tắc bảo mật:

```text
Customer chỉ xem booking của chính mình
Không xem booking người khác bằng cách đổi id trên URL
```

Điều kiện hoàn thành:

```text
Customer A không xem được booking của Customer B
```

---

### Customer-M7: Hủy booking

Điều kiện:

```text
Booking thuộc về customer đang đăng nhập
Status = Pending hoặc Confirmed
Chưa CheckedIn
```

Sau hủy:

```text
Booking.Status = Cancelled
CancelReason lưu nếu có
CancelledAt = DateTime.Now
Ghi ActivityLogs nếu đã có service log
```

Điều kiện hoàn thành:

```text
Hủy Pending được
Hủy Confirmed được
Không hủy CheckedIn/CheckedOut được
Không hủy booking của người khác được
```

---

# 7. Tiến trình tích hợp theo tuần

## Tuần 1 — Hoàn thiện nền và module đọc dữ liệu

### Việc chung

```text
Push project hiện tại lên GitHub
Tạo develop
Tạo 3 feature branch
Tạo database V1 hoàn chỉnh
Tạo layout role hoàn chỉnh
Tạo placeholder action cho menu
```

### Người 1 — Admin

```text
Admin-M1 Dashboard
Admin-M2 Danh sách RoomTypes
Admin-M3 Danh sách Rooms
Admin-M4 Danh sách Services
```

### Người 2 — Receptionist

```text
Receptionist-M1 Dashboard
Receptionist-M2 Danh sách booking
Receptionist-M2 Chi tiết booking
```

### Người 3 — Customer

```text
Customer-M1 Trang chủ
Customer-M2 Danh sách phòng
Customer-M3 Form tìm phòng
```

Mốc cuối tuần 1:

```text
Cả 3 role đăng nhập được
Mỗi role có dashboard riêng
Các danh sách chính hiển thị dữ liệu
Chưa cần xử lý nghiệp vụ phức tạp
```

---

## Tuần 2 — Làm nghiệp vụ chính

### Người 1 — Admin

```text
Admin-M2 CRUD RoomTypes
Admin-M3 CRUD Rooms
Admin-M4 CRUD Services
Admin-M5 Quản lý nhân viên
```

### Người 2 — Receptionist

```text
Receptionist-M3 Tạo booking tại quầy
Receptionist-M4 Xác nhận booking
Receptionist-M5 Check-in
Receptionist-M6 Thêm dịch vụ phát sinh
```

### Người 3 — Customer

```text
Customer-M3 Tìm phòng trống đúng logic
Customer-M4 Chi tiết phòng
Customer-M5 Đặt phòng online
Customer-M6 Lịch sử đặt phòng
```

Mốc cuối tuần 2:

```text
Customer đặt phòng online được
Receptionist xác nhận và check-in được
Admin quản lý phòng/loại phòng/dịch vụ được
```

---

## Tuần 3 — Hoàn thiện checkout, thanh toán, báo cáo

### Người 1 — Admin

```text
Admin-M6 Báo cáo doanh thu
Admin-M6 ActivityLogs
Kiểm tra quyền truy cập Admin
```

### Người 2 — Receptionist

```text
Receptionist-M7 Check-out và tạo hóa đơn
Receptionist-M8 Thanh toán
Kiểm tra quy tắc trạng thái booking/invoice/payment
```

### Người 3 — Customer

```text
Customer-M6 Chi tiết lịch sử đặt phòng
Customer-M7 Hủy booking
Hoàn thiện UI public
```

Mốc cuối tuần 3:

```text
Demo được full flow:
Customer đặt phòng
Receptionist xác nhận
Receptionist check-in
Receptionist thêm dịch vụ
Receptionist check-out
Receptionist thanh toán
Admin xem báo cáo
```

---

# 8. Thứ tự merge Pull Request

Không merge theo cảm tính. Merge theo thứ tự sau:

```text
1. Base database + layout role
2. Admin danh sách RoomTypes/Rooms/Services
3. Customer danh sách phòng
4. Customer tìm phòng trống
5. Customer đặt phòng online
6. Receptionist danh sách booking
7. Receptionist xác nhận booking
8. Receptionist check-in
9. Receptionist thêm dịch vụ
10. Receptionist check-out + invoice
11. Receptionist payment
12. Admin report + activity logs
```

Lý do:

```text
Customer cần RoomTypes/Rooms của Admin
Receptionist cần Booking của Customer
Admin report cần Invoice/Payment của Receptionist
```

---

# 9. Checklist trước khi tạo Pull Request

Mỗi người trước khi tạo PR phải kiểm tra:

```text
Build succeeded
Chạy project được
Đăng nhập đúng role được
Không lỗi layout
Không lỗi menu
Không sửa file ngoài phạm vi nếu chưa báo
Không tạo migration riêng nếu chưa thống nhất
Không phá đăng nhập/đăng xuất
```

Với chức năng có ghi dữ liệu:

```text
Có validate form
Có kiểm tra quyền
Không trust dữ liệu từ hidden input nếu dữ liệu quan trọng
Có xử lý trường hợp không tìm thấy Id
Có thông báo lỗi thân thiện
```

---

# 10. Checklist sau khi merge vào develop

Sau mỗi lần merge, trưởng nhóm chạy lại:

```text
Build succeeded
Update-Database không lỗi
Admin login được
Receptionist login được
Customer login được
Menu từng role đúng
Chức năng vừa merge chạy được
Chức năng cũ không bị hỏng
```

Nếu merge xong lỗi, ưu tiên sửa ngay trên `develop` hoặc revert PR đó. Không merge tiếp tính năng khác khi `develop` đang lỗi.

---

# 11. Quy tắc khóa file

Các file không tự ý sửa:

```text
Program.cs
appsettings.json
HotelDbContext.cs
Models/**
Migrations/**
SeedData.cs
Views/Shared/_Layout.cshtml
```

Các file mỗi người được sửa:

## Admin

```text
Controllers/AdminController.cs
Views/Admin/**
Views/Shared/RoleMenus/_AdminMenu.cshtml
Services/Admin/**
ViewModels/Admin/**
```

## Receptionist

```text
Controllers/ReceptionistController.cs
Views/Receptionist/**
Views/Shared/RoleMenus/_ReceptionistMenu.cshtml
Services/Receptionist/**
ViewModels/Receptionist/**
```

## Customer

```text
Controllers/HomeController.cs
Controllers/RoomsController.cs
Controllers/BookingsController.cs
Controllers/CustomerController.cs
Views/Home/**
Views/Rooms/**
Views/Bookings/**
Views/Customer/**
Views/Shared/RoleMenus/_CustomerMenu.cshtml
Services/Customer/**
ViewModels/Customer/**
```

---

# 12. Phân công chốt

## Người 1 — Admin

```text
Quản lý dữ liệu nền:
- RoomTypes
- Rooms
- Services
- Employees
- Customers

Theo dõi:
- Bookings
- Invoices
- Reports
- ActivityLogs
```

## Người 2 — Receptionist

```text
Vận hành:
- Booking management
- Walk-in booking
- Confirm booking
- Check-in
- Booking services
- Check-out
- Invoice
- Payment
```

## Người 3 — Customer/Public

```text
Website khách hàng:
- Home
- Room list
- Room search
- Room detail
- Online booking
- My bookings
- Cancel booking
- Profile
```

Cách chia này cho phép 3 người làm song song, ít đụng file, và khi ghép lại vẫn ra đúng một hệ thống PMS MVC hoàn chỉnh.
