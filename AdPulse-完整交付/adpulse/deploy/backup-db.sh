#!/usr/bin/env bash
# SQLite 在线热备份（WAL 安全，等效 sqlite3 adpulse.db ".backup ..."）。
# 不需要停服；用 Python 标准库 sqlite3 的 backup API，避免直接 cp 拷到半截 WAL。
# 用法：./backup-db.sh [源db路径] [备份目录]
# 恢复：停服 → 用备份文件覆盖 /var/adpulse/data/adpulse.db（连同删除 -wal/-shm 残留）→ 启动。
set -euo pipefail
SRC="${1:-/var/adpulse/data/adpulse.db}"
DEST_DIR="${2:-/var/adpulse/backups}"
mkdir -p "$DEST_DIR"
STAMP=$(date +%Y%m%d-%H%M%S)
DEST="$DEST_DIR/adpulse-$STAMP.db"

python3 - "$SRC" "$DEST" <<'PY'
import sqlite3, sys
src = sqlite3.connect(sys.argv[1])
dst = sqlite3.connect(sys.argv[2])
with dst:
    src.backup(dst)
dst.close(); src.close()
print("备份完成:", sys.argv[2])
PY

python3 - "$DEST" <<'PY'
import sqlite3, sys
db = sqlite3.connect(sys.argv[1])
n = db.execute("select count(*) from News").fetchone()[0]
u = db.execute("select count(*) from Users").fetchone()[0]
print(f"备份校验通过：News {n} 条 / Users {u} 个")
PY

# 可选 cron 每日 3 点备份（保留 14 天）：
# 0 3 * * * /var/adpulse/backup-db.sh >> /var/adpulse/logs/backup.log 2>&1
# 0 4 * * * find /var/adpulse/backups -name 'adpulse-*.db' -mtime +14 -delete