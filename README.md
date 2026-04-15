# 🌐 SCM-System (Supply Chain Management Web App)

> **Lưu ý:** Đây là đồ án môn học Công nghệ phần mềm tại Trường Đại học Tôn Đức Thắng. Dự án được phát triển theo hướng Web-based System hiện đại.

[![Framework: ASP.NET Core](https://img.shields.io/badge/Framework-ASP.NET_Core_8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Database: SQL Server](https://img.shields.io/badge/Database-SQL_Server-red.svg)]()
[![ORM: EF Core](https://img.shields.io/badge/ORM-Entity_Framework_Core-3FA037.svg)]()

## 📖 Giới thiệu (Introduction)
**SCM-System** là hệ thống quản trị chuỗi cung ứng toàn diện, được số hóa trên nền tảng Web. Hệ thống giải quyết các bài toán cốt lõi của doanh nghiệp bao gồm: **Quản lý Mua hàng (Procurement)**, **Kiểm soát Tồn kho (Inventory Control)**, và **Vận hành Đơn hàng (Order Fulfillment)**.

Việc chuyển đổi sang nền tảng **ASP.NET Core** giúp hệ thống dễ dàng triển khai (deploy) lên Cloud, hỗ trợ đa nền tảng và cung cấp các RESTful APIs tiêu chuẩn cho các thiết bị di động (ứng dụng của Shipper) trong tương lai.

## 👥 Nhóm phát triển (Team Members)
* **Thành viên 1:** Trần Gia Phát - 52400148 - Backend Developer / System Architect
* **Thành viên 2:** Ngô Khánh Bình - 52400005 - Frontend Developer / Tester
* **Thành viên 3:** Hoàng Xuân Tuấn - 52400166 - Frontend Developer / Tester
* **Giảng viên hướng dẫn:** Ths. Võ Thị Kim Anh

## 🚀 Tính năng Kỹ thuật Nổi bật (Technical Highlights)

Dự án không chỉ là các thao tác CRUD cơ bản mà tập trung xử lý các nghiệp vụ kho vận phức tạp:

* 🔐 **Authentication & Authorization:** Hệ thống phân quyền chặt chẽ (Role-based Access Control) cho Admin, Nhân viên kho, Nhân viên mua hàng và Shipper.
* 🛡️ **Xử lý Tương tranh (Concurrency Control):** Áp dụng Transaction và cơ chế Locking ở tầng Database thông qua Entity Framework Core để chống tình trạng Over-selling (Bán lố hàng tồn kho) khi có lượng truy cập lớn.
* 📍 **Truy xuất nguồn gốc (Traceability):** Quản lý vòng đời sản phẩm tới từng cá thể vật lý thông qua `SerialNumber` (Từ lúc nhập kho -> Cất kệ -> Đóng gói -> Giao thành công).
* 🤝 **Xác thực Giao nhận Số (Digital Handshake):** Cung cấp API để ứng dụng Shipper cập nhật tọa độ, tải lên hình ảnh minh chứng (`HandshakeProof`) và chốt trạng thái đơn hàng theo thời gian thực.

## 🏗️ Kiến trúc Hệ thống (System Architecture)
Dự án được cấu trúc theo mô hình chuẩn của một Web Application hiện đại:
* **Controllers:** Tiếp nhận HTTP Request, điều hướng và trả về HTTP Response (JSON/Views).
* **Services (BLL):** Chứa toàn bộ Business Logic (Kiểm tra tồn kho, tính toán công nợ...).
* **Data Access (DAL):** Sử dụng Repository Pattern kết hợp Entity Framework Core để tương tác với SQL Server.
* **DTOs (Data Transfer Objects):** Mapping dữ liệu giữa Entity và View/Client để bảo mật cấu trúc Database.

## 📊 Tài liệu Thiết kế (Documentation)
* [Entity Relationship Diagram (ERD)](docs/images/erd.png) - Thiết kế Cơ sở dữ liệu đạt Dạng chuẩn 3 (3NF).
* [Activity Diagrams](docs/images/activity-diagrams) - Quy trình Nhập - Xuất - Vận chuyển.

## 🛠️ Hướng dẫn cài đặt (Setup Instructions)
1. Clone repository: 
   ```bash
   git clone https://github.com/phatgia/SCM-System.git
   ```
2. Mở file `appsettings.json` và cấu hình lại chuỗi kết nối cơ sở dữ liệu (Connection String).
3. Mở Package Manager Console và chạy lệnh Update Database:
   ```bash
   Update-Database
   ```
4. Build và Run project (F5).
