# SendGrid Email Configuration for DATN64

This project now supports sending OTP emails via SendGrid as an alternative to SMTP.

## 1. Configure SendGrid in appsettings

In `appsettings.json` or `appsettings.Development.json`, set:

```json
"MailSettings": {
  "Provider": "sendgrid",
  "FromEmail": "no-reply@novatech.vn",
  "FromName": "NovaTech",
  "SendGridApiKey": "YOUR_SENDGRID_API_KEY",
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "haodvttb01628@gmail.com",
  "Password": "haodinh1709"
}
```

Only `Provider`, `FromEmail`, `FromName`, and `SendGridApiKey` are required for SendGrid.
The SMTP settings remain for fallback when `Provider` is not `sendgrid`.

## 2. Setup SendGrid API Key

1. Create or log in to SendGrid.
2. Create an API key with Mail Send permission.
3. Paste the key into `SendGridApiKey`.

## 3. Test OTP sending

1. Start the app with `dotnet run`.
2. Go to `/Account/Register`.
3. Enter any valid email and click `Gửi mã OTP`.
4. If the email is sent successfully, the page will show a success message.

## 4. Fallback to SMTP

If `Provider` is omitted or not `sendgrid`, the app uses SMTP.

For Gmail SMTP, use:

```json
"Host": "smtp.gmail.com",
"Port": 587,
"EnableSsl": true,
"Username": "your-gmail@gmail.com",
"Password": "your-app-password"
```

## 5. Notes

- `ToEmail` can be any valid email address.
- SendGrid is easier if Gmail SMTP authentication is failing.
- Make sure `FromEmail` is authorized by SendGrid if SendGrid account requires sender validation.
