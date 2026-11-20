#!/usr/bin/env python3
"""MD 收尾：划掉步骤5/7，并对步骤7中无法本地验证的公网项如实保留未划掉+备注。
在 10 分钟挂机验收通过后执行。"""
import subprocess
import sys

MD = "/home/user/Doubao/chats/38453740758824962/AdPulse项目实施计划.md"
ROOT = "/home/user/Doubao/chats/38453740758824962/adpulse"

# 步骤7中待真实服务器执行的项：定位 substring → 备注
PENDING = {
    "电脑浏览器打开": "⏳ 待真实服务器公网复验；本地 nginx 等效验证已通过",
    "手机连流量": "⏳ 待真机流量网络复验；本地 375px 视口等效验证已通过",
    "公网后台登录": "⏳ 待公网复验；本地 nginx SSE 链路 48ms 推送已验证",
    "systemctl restart": "⏳ 待服务器执行；本地 keepalive 等效验证 kill -9 后 7 秒自动恢复、数据不丢",
    "sudo reboot": "⏳ 待服务器执行；unit 文件已含 WantedBy=multi-user.target，enable 命令见 deploy-commands.md",
    "改掉两个演示账号": "⏳ 待服务器执行；--reset-password CLI 已在临时库副本验证（旧密码被拒/新密码可登录）",
}


def main():
    subprocess.run([sys.executable, f"{ROOT}/tools/mark_done.py", "5"], check=True)
    subprocess.run([sys.executable, f"{ROOT}/tools/mark_done.py", "7"], check=True)

    with open(MD, encoding="utf-8") as f:
        lines = f.readlines()

    # 定位步骤7章节范围
    start = next(i for i, l in enumerate(lines) if l.startswith("## 9."))
    end = next(i for i, l in enumerate(lines) if i > start and l.startswith("## 10."))

    for i in range(start, end):
        l = lines[i]
        # 章节标题备注改为如实表述
        if l.startswith("## 9.") and "已执行并验收通过" in l:
            lines[i] = l.replace("（✅ 已执行并验收通过）",
                                 "（✅ 本地可验证项全部通过；6 项公网/服务器操作待真实服务器执行，均已完成等效验证）")
        if l.startswith("###") and "验收清单" in l and "全部通过" in l:
            lines[i] = l.replace("（✅ 全部通过）", "（本地可验证项全部通过）")
        # 待服务器项：取消划掉，恢复未勾选 + 备注
        if l.startswith("- [x] ~~"):
            for key, note in PENDING.items():
                if key in l:
                    text = l[len("- [x] ~~"):].rstrip("\n")
                    if text.endswith("~~"):
                        text = text[:-2]
                    lines[i] = f"- [ ] {text}（{note}）\n"
                    break

    # 总览表第7行备注
    for i, l in enumerate(lines):
        if l.startswith("| 7 |") and "~~ ✅" in l:
            lines[i] = l.replace("~~ ✅", "~~ ✅（本地等效）")

    # 第10章终验表 5-10 行划掉（格式与已划掉的 1-4 行一致）
    for i, l in enumerate(lines):
        for n in (5, 6, 7, 8, 9, 10):
            if l.startswith(f"| {n} |") and "~~" not in l:
                cells = l.split("|")
                cells[2] = f" ~~{cells[2].strip()}~~ ✅ "
                if n == 8:
                    cells[4] = cells[4].rstrip() + "（重启服务器项待真实服务器验证） "
                lines[i] = "|".join(cells)

    with open(MD, "w", encoding="utf-8") as f:
        f.writelines(lines)
    print("MD 收尾完成：步骤5/7已划掉，步骤7公网项如实保留待办备注，终验表5-10已划掉")


if __name__ == "__main__":
    main()