cd C:\work\tgbots\BotManager

dotnet build --runtime ubuntu.18.04-x64

cd C:\Program Files (x86)\7-Zip

7z a C:\work\BotManager.zip C:\work\tgbots\BotManager\bin\Debug\netcoreapp3.1\ubuntu.18.04-x64\.

Plink root@176.119.156.220 -no-antispoof -pw IGduEj7w0n "systemctl stop bot_manager"; "cd /root"; "rm -r /root/BotManager";

pscp.exe -pw IGduEj7w0n "C:\work\BotManager.zip" "root@176.119.156.220:/root/BotManager.zip"

pscp.exe -pw IGduEj7w0n "C:\work\tgbots\bot_manager.service" "root@176.119.156.220:/etc/systemd/system/bot_manager.service"

Plink root@176.119.156.220 -no-antispoof -pw IGduEj7w0n "cd /root"; mkdir BotManager"; 

Plink root@176.119.156.220 -no-antispoof -pw IGduEj7w0n "mv /root/BotManager.zip /root/BotManager/BotManager.zip"; 

Plink root@176.119.156.220 -no-antispoof -pw IGduEj7w0n "cd /root/BotManager"; "unzip BotManager.zip"; "rm BotManager.zip"; 

Plink root@176.119.156.220 -no-antispoof -pw IGduEj7w0n "chmod u+x /root/BotManager/BotManager"; 

Plink root@176.119.156.220 -no-antispoof -pw IGduEj7w0n "systemctl daemon-reload"; "systemctl enable bot_manager"; "systemctl start bot_manager";

cd C:\work

del BotManager.zip

cd C:\work\tgbots