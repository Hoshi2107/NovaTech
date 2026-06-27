$smtp = New-Object System.Net.Mail.SmtpClient('smtp.gmail.com',587)
$smtp.EnableSsl = $true
$smtp.Credentials = New-Object System.Net.NetworkCredential('haodvttb01628@gmail.com','haodinh1709')
$smtp.DeliveryMethod = [System.Net.Mail.SmtpDeliveryMethod]::Network
$msg = New-Object System.Net.Mail.MailMessage('haodvttb01628@gmail.com','haodvttb01628@gmail.com','Test SMTP','Test body')
try { $smtp.Send($msg); Write-Output 'SMTP_OK' } catch { Write-Output 'SMTP_FAIL'; Write-Output $_.Exception.GetType().FullName; Write-Output $_.Exception.Message; if ($_.Exception.InnerException) { Write-Output $_.Exception.InnerException.Message } }
