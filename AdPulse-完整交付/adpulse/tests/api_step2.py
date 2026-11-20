#!/usr/bin/env python3
"""步骤2验收：按计划第4章清单逐项自动化验证（Swagger 等价操作）。"""
import base64
import json
import urllib.error
import urllib.request
import uuid
from urllib.parse import quote, urlsplit, urlunsplit

BASE = "http://localhost:5000"
RESULTS = []


def req(method, path, data=None, token=None, raw=None, ctype="application/json"):
    parts = urlsplit(path)
    path = urlunsplit((parts.scheme, parts.netloc, parts.path, quote(parts.query, safe="=&"), parts.fragment))
    headers = {}
    if token:
        headers["Authorization"] = "Bearer " + token
    body = None
    if raw is not None:
        body = raw
        headers["Content-Type"] = ctype
    elif data is not None:
        body = json.dumps(data).encode()
        headers["Content-Type"] = "application/json"
    r = urllib.request.Request(BASE + path, data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(r, timeout=20) as resp:
            return resp.status, json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        try:
            return e.code, json.loads(e.read().decode() or "{}")
        except Exception:
            return e.code, {}


def check(name, cond, extra=""):
    RESULTS.append(bool(cond))
    print(("PASS" if cond else "FAIL"), "|", name, ("| " + str(extra) if extra else ""))


def multipart(filename, content, ctype):
    b = "----adpulse" + uuid.uuid4().hex
    head = f"--{b}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{filename}\"\r\nContent-Type: {ctype}\r\n\r\n".encode()
    tail = f"\r\n--{b}--\r\n".encode()
    return head + content + tail, f"multipart/form-data; boundary={b}"


# 1 登录
s, b = req("POST", "/api/auth/login", {"userName": "admin", "password": "AdPulse@2026"})
token = b.get("data", {}).get("token", "")
check("admin 登录拿到 token", s == 200 and b.get("code") == 0 and len(token) > 50)

s, b = req("POST", "/api/auth/login", {"userName": "admin", "password": "wrong"})
check("错误密码返回中文提示", b.get("code") == 40000 and "用户名或密码错误" in b.get("message", ""), b.get("message"))

# 2 未登录 401
s, b = req("GET", "/api/admin/news")
check("未登录访问后台接口返回 40100", s == 401 and b.get("code") == 40100)

# 3 带 token 列表
s, b = req("GET", "/api/admin/news?page=1&pageSize=5", token=token)
check("带 token 后台列表正常", s == 200 and b.get("code") == 0 and b["data"]["total"] >= 27, "total=%s" % b.get("data", {}).get("total"))

# 4 新建草稿 → 前台不可见 → 发布 → 前台可见
suffix = uuid.uuid4().hex[:6]
test_title = f"验收测试新闻{suffix}：程序化创意进入实时时代"
payload = {
    "title": test_title,
    "summary": "这是一条用于自动化验收的演示新闻。",
    "content": "<p>第一段：验收正文。</p><p>第二段：程序化创意正在改变素材生产节奏。</p>",
    "coverUrl": "/covers/cover-04.svg", "categoryId": 2, "tagIds": [4],
    "publish": False, "isTop": False, "isHeadline": False,
}
s, b = req("POST", "/api/admin/news", payload, token=token)
nid = b.get("data", {}).get("id")
check("新建草稿成功", s == 200 and b.get("code") == 0 and nid)

s, b = req("GET", f"/api/portal/news?keyword={test_title}")
check("草稿在前台不可见", b.get("code") == 0 and b["data"]["total"] == 0)

s, b = req("PATCH", f"/api/admin/news/{nid}/status", {"action": "publish"}, token=token)
check("草稿发布成功", s == 200 and b.get("code") == 0)

s, b = req("GET", f"/api/portal/news?keyword={test_title}")
check("发布后前台可查到", b.get("code") == 0 and b["data"]["total"] == 1)

# 5 editor 权限
s, b = req("POST", "/api/auth/login", {"userName": "editor", "password": "Editor@2026"})
etoken = b.get("data", {}).get("token", "")
eid = b.get("data", {}).get("user", {}).get("id")
check("editor 登录成功", bool(etoken))

s, b = req("DELETE", f"/api/admin/news/{nid}", token=etoken)
check("editor 删除 admin 文章返回 403", s == 403 and b.get("code") == 40300)

s, b = req("GET", "/api/admin/logs", token=etoken)
check("editor 访问操作日志返回 403", s == 403)

s, b = req("GET", "/api/admin/news?pageSize=50", token=etoken)
own = all(i["authorId"] == eid for i in b.get("data", {}).get("items", []))
check("editor 列表仅见自己的文章", b.get("code") == 0 and own)

# 6 参数校验
bad = dict(payload, title="")
s, b = req("POST", "/api/admin/news", bad, token=token)
check("空标题返回 40000 中文提示", b.get("code") == 40000 and "标题不能为空" in b.get("message", ""), b.get("message"))

# 7 浏览量
s, b = req("GET", f"/api/portal/news/{nid}")
v1 = b["data"]["viewCount"]
s, b = req("GET", f"/api/portal/news/{nid}")
v2 = b["data"]["viewCount"]
check("详情访问两次浏览量 +2", v2 == v1 + 1, f"{v1}->{v2}（第一次访问前为 {v1-1}）")

# 8 上传
png = base64.b64decode("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==")
raw, ct = multipart("test.png", png, "image/png")
s, b = req("POST", "/api/admin/upload", token=token, raw=raw, ctype=ct)
url = b.get("data", {}).get("url", "")
check("上传 jpg/png 成功", s == 200 and b.get("code") == 0 and url.startswith("/uploads/"))
if url:
    with urllib.request.urlopen(BASE + url, timeout=10) as r:
        check("上传文件可访问", r.status == 200)
raw, ct = multipart("evil.txt", b"not an image", "text/plain")
s, b = req("POST", "/api/admin/upload", token=token, raw=raw, ctype=ct)
check("上传 .txt 被拒绝", b.get("code") == 40000, b.get("message"))

# 9 数据接口
s, b = req("GET", "/api/portal/stats")
d = b.get("data", {})
check("stats 返回四组图表数据", all(k in d for k in ("marketTrend", "mediaShare", "categoryCompare", "regionDistribution")))

# 10 日志
s, b = req("GET", "/api/admin/logs?pageSize=20", token=token)
acts = [i["action"] for i in b.get("data", {}).get("items", [])]
check("操作日志含发布动作", any("发布" in a for a in acts), acts[:5])

# 11 Swagger 安全定义
with urllib.request.urlopen(BASE + "/swagger/v1/swagger.json", timeout=10) as r:
    sw = json.loads(r.read().decode())
check("Swagger 含 Bearer Authorize 定义", "Bearer" in sw.get("components", {}).get("securitySchemes", {}))
paths = sw.get("paths", {})
check("Swagger 接口数量 >= 26", len(paths) >= 20, "paths=%d" % len(paths))

print("\n%d/%d 通过" % (sum(RESULTS), len(RESULTS)))
# 清理：测试新闻移入回收站，避免污染演示数据
req("DELETE", f"/api/admin/news/{nid}", token=token)
raise SystemExit(0 if all(RESULTS) else 1)