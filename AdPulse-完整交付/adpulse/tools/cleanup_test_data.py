#!/usr/bin/env python3
"""清理验收测试残留数据（打包交付前执行）。
只删测试产生的新闻/日志/孤儿标签/上传文件，种子演示数据一律保留。
用法: python3 cleanup_test_data.py <db路径> [<db路径> ...]
"""
import sqlite3
import sys

PATTERNS = ["%验收%", "%E2E%", "%终验%"]


def clean(path: str):
    db = sqlite3.connect(path)
    cur = db.cursor()
    where = " OR ".join(["Title LIKE ?"] * len(PATTERNS))
    ids = [r[0] for r in cur.execute(f"SELECT Id FROM News WHERE {where}", PATTERNS)]
    if ids:
        ph = ",".join("?" * len(ids))
        cur.execute(f"DELETE FROM NewsTags WHERE NewsId IN ({ph})", ids)
        cur.execute(f"DELETE FROM ViewLogs WHERE NewsId IN ({ph})", ids)
        cur.execute(f"DELETE FROM News WHERE Id IN ({ph})", ids)
    logwhere = " OR ".join(["Detail LIKE ?"] * len(PATTERNS))
    cur.execute(f"DELETE FROM OperationLogs WHERE {logwhere} OR Action='重新生成AI摘要'", PATTERNS)
    # AI/测试新建的孤儿标签（种子标签 Id<=12 不动）
    cur.execute("DELETE FROM Tags WHERE Id > 12 AND Id NOT IN (SELECT DISTINCT TagId FROM NewsTags)")
    # 自增序列回退到当前最大 Id，保持整洁
    for table in ("News", "Tags", "OperationLogs", "ViewLogs"):
        mx = cur.execute(f"SELECT COALESCE(MAX(Id),0) FROM {table}").fetchone()[0]
        cur.execute("UPDATE sqlite_sequence SET seq=? WHERE name=?", (mx, table))
    db.commit()
    cur.execute("PRAGMA wal_checkpoint(TRUNCATE)")
    n = cur.execute("SELECT COUNT(*) FROM News").fetchone()[0]
    t = cur.execute("SELECT COUNT(*) FROM Tags").fetchone()[0]
    pub = cur.execute("SELECT COUNT(*) FROM News WHERE Status='Published'").fetchone()[0]
    db.close()
    print(f"{path}: 清理测试新闻 {len(ids)} 条；剩余 News {n}（已发布 {pub}）/ Tags {t}")


if __name__ == "__main__":
    for p in sys.argv[1:]:
        clean(p)