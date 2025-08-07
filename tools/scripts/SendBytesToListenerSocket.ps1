$c = New-Object System.Net.Sockets.TcpClient("127.0.0.1",6000)
$s = $c.GetStream()
$b = [Text.Encoding]::UTF8.GetBytes("hello")
$s.Write($b,0,$b.Length)
$buf = New-Object byte[] 1024
$n = $s.Read($buf,0,$buf.Length)
[Text.Encoding]::UTF8.GetString($buf,0,$n)
$c.Close()