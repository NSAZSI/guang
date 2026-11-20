# AdPulse 服务器部署命令清单（Ubuntu 22.04/24.04，裸机不用 Docker）

> 每条命令都附"执行后应该看到什么"。按顺序执行；其他发行版差异见文末。
> 前提：你有一台能 SSH 登录、有 sudo 权限的 Linux 服务器，云安全组已放行 80。

## 0. 开发机：生成部署产物

```bash
# 后端（框架依赖模式，服务器只需装运行时不需装 SDK）
cd adpulse/backend/AdPulse.Api
dotnet publish -c Release -o ./publish
# 应看到：AdPulse.Api -> .../publish/ 且无 Error

# 前端（.env.production 已设 VITE_API_BASE=/api，同源相对路径，生产无 CORS）
cd ../../frontend
npm ci && npm run build
# 应看到：dist/ 目录生成，✓ built in xxs
```

## 1. 开发机：上传到服务器

```bash
scp -r backend/AdPulse.Api/publish 用户@服务器IP:/tmp/adpulse-api
scp -r frontend/dist               用户@服务器IP:/tmp/adpulse-www
scp deploy/nginx-adpulse.conf deploy/adpulse-api.service deploy/backup-db.sh 用户@服务器IP:/tmp/
# 应看到：逐文件 100% 进度条
# 数据库可选：把开发机 App_Data/adpulse.db 也 scp 到 /tmp/（含演示数据）；
# 不传也行——首次启动会自动建库+播种演示数据。
```

## 2. 服务器：安装运行时与 Nginx

```bash
# .NET 10 ASP.NET Core 运行时（Ubuntu 22.04/24.04 官方源已收录）
sudo apt-get update && sudo apt-get install -y aspnetcore-runtime-10.0 nginx
dotnet --list-runtimes
# 应看到：Microsoft.AspNetCore.App 10.0.x 一行
# 若源里找不到包：先加微软包源 https://learn.microsoft.com/dotnet/core/install/linux-ubuntu
```

## 3. 服务器：目录与权限

```bash
sudo mkdir -p /var/adpulse/{api,www,data,uploads,logs,backups}
sudo cp -r /tmp/adpulse-api/* /var/adpulse/api/
sudo cp -r /tmp/adpulse-www/* /var/adpulse/www/
sudo cp /tmp/backup-db.sh /var/adpulse/ && sudo chmod +x /var/adpulse/backup-db.sh
# 可选：sudo cp /tmp/adpulse.db /var/adpulse/data/
sudo chown -R www-data:www-data /var/adpulse/data /var/adpulse/uploads /var/adpulse/logs /var/adpulse/backups
# 应看到：无输出即成功；ls /var/adpulse 能看到六个目录
```

## 4. 服务器：systemd 服务

```bash
sudo cp /tmp/adpulse-api.service /etc/systemd/system/adpulse-api.service
sudo nano /etc/systemd/system/adpulse-api.service
# 必改两行：Jwt__Key=换成服务器专用长随机密钥（≥32字符）；
#          Ai__ApiKey=有 AI 密钥就填（Base Url/Model 一并填），没有就留空（自动规则降级）
sudo systemctl daemon-reload
sudo systemctl enable --now adpulse-api
sudo systemctl status adpulse-api
# 应看到：active (running)；curl http://127.0.0.1:5000/api/health 返回 {"code":0,...,"status":"up"}
```

## 5. 服务器：Nginx

```bash
sudo cp /tmp/nginx-adpulse.conf /etc/nginx/sites-available/adpulse.conf
sudo ln -s /etc/nginx/sites-available/adpulse.conf /etc/nginx/sites-enabled/
sudo mv /etc/nginx/sites-enabled/default /etc/nginx/sites-enabled/default.disabled
sudo nginx -t && sudo systemctl reload nginx
# 应看到：syntax is ok / test is successful
curl -I http://127.0.0.1/
# 应看到：HTTP/1.1 200 OK（此时浏览器打开 http://服务器IP 就是完整首页）
```

## 6. 防火墙 / 安全组

```bash
sudo ufw allow 80/tcp    # 若启用了 ufw
# 云厂商控制台安全组同样放行 80（服务器内 curl 正常但公网打不开，九成是这里）
```

## 7. 改掉演示账号默认密码（必做）

```bash
cd /var/adpulse/api
sudo -u www-data ConnectionStrings__Default="Data Source=/var/adpulse/data/adpulse.db" \
  dotnet AdPulse.Api.dll --reset-password admin '你的新强密码'
sudo -u www-data ConnectionStrings__Default="Data Source=/var/adpulse/data/adpulse.db" \
  dotnet AdPulse.Api.dll --reset-password editor '另一个新强密码'
# 应看到：已重置 admin / editor 的密码（PBKDF2 哈希入库，明文不落盘）
```

## 8. 备份与恢复

```bash
sudo -u www-data /var/adpulse/backup-db.sh
# 应看到：备份完成: /var/adpulse/backups/adpulse-时间戳.db + 备份校验通过：News N 条
# 恢复：sudo systemctl stop adpulse-api
#       sudo cp /var/adpulse/backups/某备份.db /var/adpulse/data/adpulse.db
#       sudo rm -f /var/adpulse/data/adpulse.db-wal /var/adpulse/data/adpulse.db-shm
#       sudo chown www-data:www-data /var/adpulse/data/adpulse.db && sudo systemctl start adpulse-api
```

## 9. 可选：HTTPS（有域名后）

```bash
sudo apt-get install -y certbot python3-certbot-nginx
sudo certbot --nginx -d 你的域名
# 应看到：Congratulations! 证书自动续期已配置；nginx 配置被 certbot 自动加上 443 段
```

## 其他发行版差异

- Debian 12：包名相同，微软源换 debian/12 路径；nginx 站点目录同为 sites-available/enabled。
- CentOS/RHEL/Rocky：用 `dnf install aspnetcore-runtime-10.0 nginx`；nginx 配置放 `/etc/conf.d/adpulse.conf`（主配置默认 include conf.d）；防火墙用 `firewall-cmd --add-service=http --permanent && firewall-cmd --reload`；SELinux 需 `setsebool -P httpd_can_network_connect 1` 否则反代 502。