#!/usr/bin/env bash
# systemd Restart=always + RestartSec=5 的本地等效验证脚本。
# 用途：在没有 systemd 的环境（如容器）里验证"进程挂了 5 秒内自动拉起"。
# 真实服务器请直接用 adpulse-api.service，不需要本脚本。
# 用法：ADPULSE_JWT_KEY='你的长随机密钥' ./keepalive.sh
set -u
: "${ADPULSE_JWT_KEY:?请先设置环境变量 ADPULSE_JWT_KEY}"
cd /var/adpulse/api
while true; do
  echo "[keepalive] $(date -Is) 启动 AdPulse.Api"
  ASPNETCORE_ENVIRONMENT=Production \
  ASPNETCORE_URLS=http://127.0.0.1:5000 \
  ConnectionStrings__Default="Data Source=/var/adpulse/data/adpulse.db" \
  Jwt__Key="$ADPULSE_JWT_KEY" \
  UploadPath=/var/adpulse/uploads \
  Serilog__Path=/var/adpulse/logs/adpulse-.log \
  Ai__BaseUrl="${ADPULSE_AI_BASEURL:-}" \
  Ai__ApiKey="${ADPULSE_AI_KEY:-}" \
  Ai__Model="${ADPULSE_AI_MODEL:-}" \
  dotnet AdPulse.Api.dll
  code=$?
  echo "[keepalive] $(date -Is) 进程退出(code=$code)，5 秒后自动重启（等效 Restart=always + RestartSec=5）"
  sleep 5
done