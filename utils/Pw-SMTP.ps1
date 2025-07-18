$smtpServer = "smtp.azurecomm.net"
$smtpPort = 587
$smtpUsername = "teste"
$smtpPassword = ""
$from = "DoNotReply@6a1e88eb-6d06-4f80-8d4c-1b90c1beb9a0.azurecomm.net"
$to = "igor.coura@rufinoempreiteira.com.br"
$subject = "Test Email STMP"
$body = "Body Test Email STMP"

$securePassword = ConvertTo-SecureString $smtpPassword -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential($smtpUsername, $securePassword)


Send-MailMessage -From $from -To $to -Subject $subject -Body $body -SmtpServer $smtpServer -Port $smtpPort -UseSsl -Credential $credential