$params = @{
  Name = 'Dodkin'
  BinaryPathName = 'C:\Tools\Dodkin\DodkinSvc.exe'
  Credential = New-Object System.Management.Automation.PSCredential ('NT AUTHORITY\LocalService', (New-Object System.Security.SecureString))
  DependsOn = @('MSMQ', 'MSSQLSERVER')
  DisplayName = 'Dodkin Service'
  StartupType = 'Automatic'
  Description = 'Future message delivery service.'
}
New-Service @params