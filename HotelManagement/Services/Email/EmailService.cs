using HotelManagement.Configuration;
using HotelManagement.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HotelManagement.Services.Email
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendPaymentReceiptAsync(
            string recipientEmail,
            string recipientName,
            Booking booking,
            Invoice invoice,
            IEnumerable<BookingService> bookingServices)
        {
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail)
                || string.IsNullOrWhiteSpace(_settings.AppPassword)
                || _settings.SenderEmail.Contains("your.email"))
            {
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(recipientName, recipientEmail));
            message.Subject = $"Biên lai thanh toán - {booking.BookingCode}";

            var serviceRows = bookingServices.Any()
                ? string.Join("", bookingServices.Select(s =>
                    $"<tr><td>{s.Service?.Name ?? "Dịch vụ"}</td><td>{s.Quantity}</td><td>{s.TotalPrice:N0} VND</td></tr>"))
                : "<tr><td colspan=\"3\">Không có dịch vụ kèm theo</td></tr>";

            var body = $"""
                <html>
                <body style="font-family: Arial, sans-serif; color: #333;">
                    <h2 style="color: #0d6efd;">Biên lai thanh toán</h2>
                    <p>Xin chào <strong>{recipientName}</strong>,</p>
                    <p>Cảm ơn bạn đã thanh toán đặt phòng tại khách sạn của chúng tôi.</p>

                    <h3>Thông tin đặt phòng</h3>
                    <table style="border-collapse: collapse; width: 100%;">
                        <tr><td><strong>Mã đặt phòng:</strong></td><td>{booking.BookingCode}</td></tr>
                        <tr><td><strong>Mã hóa đơn:</strong></td><td>{invoice.InvoiceCode}</td></tr>
                        <tr><td><strong>Phòng:</strong></td><td>{booking.Room?.RoomNumber} - {booking.Room?.RoomType?.Name}</td></tr>
                        <tr><td><strong>Nhận phòng:</strong></td><td>{booking.CheckInDate:dd/MM/yyyy}</td></tr>
                        <tr><td><strong>Trả phòng:</strong></td><td>{booking.CheckOutDate:dd/MM/yyyy}</td></tr>
                        <tr><td><strong>Số khách:</strong></td><td>{booking.Adults} người lớn, {booking.Children} trẻ em</td></tr>
                    </table>

                    <h3>Chi tiết thanh toán</h3>
                    <table style="border-collapse: collapse; width: 100%; border: 1px solid #ddd;">
                        <tr style="background: #f8f9fa;">
                            <th style="padding: 8px; text-align: left;">Hạng mục</th>
                            <th style="padding: 8px; text-align: right;">Số tiền</th>
                        </tr>
                        <tr><td style="padding: 8px;">Tiền phòng</td><td style="padding: 8px; text-align: right;">{invoice.RoomAmount:N0} VND</td></tr>
                        <tr><td style="padding: 8px;">Dịch vụ</td><td style="padding: 8px; text-align: right;">{invoice.ServiceAmount:N0} VND</td></tr>
                        <tr style="font-weight: bold;">
                            <td style="padding: 8px;">Tổng cộng</td>
                            <td style="padding: 8px; text-align: right; color: #0d6efd;">{invoice.TotalAmount:N0} VND</td>
                        </tr>
                    </table>

                    <h3>Dịch vụ đi kèm</h3>
                    <table style="border-collapse: collapse; width: 100%; border: 1px solid #ddd;">
                        <tr style="background: #f8f9fa;">
                            <th style="padding: 8px;">Dịch vụ</th>
                            <th style="padding: 8px;">SL</th>
                            <th style="padding: 8px;">Thành tiền</th>
                        </tr>
                        {serviceRows}
                    </table>

                    <h3>Chính sách hủy phòng</h3>
                    <ul>
                        <li><strong>Hoàn 100%:</strong> Hủy trước ngày nhận phòng hơn 7 ngày.</li>
                        <li><strong>Hoàn 100% (ưu tiên):</strong> Hủy trong vòng 24 giờ kể từ lúc thanh toán.</li>
                        <li><strong>Hoàn 50%:</strong> Hủy từ 4 đến 7 ngày trước ngày nhận phòng.</li>
                        <li><strong>Không hoàn tiền:</strong> Hủy trong vòng 3 ngày trước ngày nhận phòng.</li>
                    </ul>

                    <p style="color: #666; font-size: 12px;">Email này được gửi tự động. Vui lòng không trả lời.</p>
                </body>
                </html>
                """;

            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
