#!/usr/bin/env python3
"""把计划 MD 中已验收步骤的内容划掉（保留原文，加删除线），不删除任何内容。
用法: python3 mark_done.py <步骤号1-7>
"""
import re
import sys

MD = "/home/user/Doubao/chats/38453740758824962/AdPulse项目实施计划.md"
SECTION_HEADERS = {
    1: "## 3. 步骤 1", 2: "## 4. 步骤 2", 3: "## 5. 步骤 3", 4: "## 6. 步骤 4",
    5: "## 7. 步骤 5", 6: "## 8. 步骤 6", 7: "## 9. 步骤 7",
}
NEXT_HEADERS = {
    1: "## 4.", 2: "## 5.", 3: "## 6.", 4: "## 7.", 5: "## 8.", 6: "## 9.", 7: "## 10.",
}


def strike_line(line: str) -> str:
    s = line.rstrip("\n")
    if not s.strip():
        return line
    if s.startswith("- [ ] "):
        return "- [x] ~~" + s[6:] + "~~\n"
    if s.startswith("- [x] ") and "~~" not in s:
        return "- [x] ~~" + s[6:] + "~~\n"
    if s.startswith("#"):
        m = re.match(r"^(#+\s*)(.*)$", s)
        text = m.group(2)
        if "~~" in text:
            return line
        note = ""
        if "步骤" in text and re.match(r"^步骤\s*\d", text):
            note = "（✅ 已执行并验收通过）"
        elif "验收清单" in text:
            note = "（✅ 全部通过）"
        elif "指令块" in text:
            note = "（已执行）"
        return m.group(1) + "~~" + text + "~~" + note + "\n"
    if s.startswith(("|", "```", "    ", "\t")) or s.startswith("- "):
        return line
    if "~~" in s:
        return line
    return "~~" + s + "~~\n"


def main(step: int):
    with open(MD, encoding="utf-8") as f:
        lines = f.readlines()

    start = next(i for i, l in enumerate(lines) if l.startswith(SECTION_HEADERS[step]))
    end = next(i for i, l in enumerate(lines) if i > start and l.startswith(NEXT_HEADERS[step]))

    in_fence = False
    for i in range(start, end):
        l = lines[i]
        if l.startswith("```"):
            in_fence = not in_fence
            if l.startswith("```text"):
                lines[i] = l  # 指令块代码围栏保留原样
            continue
        if in_fence:
            continue
        lines[i] = strike_line(l)

    # 步骤总览表中对应行划掉
    for i, l in enumerate(lines):
        if re.match(rf"^\|\s*{step}\s*\|", l) and "~~" not in l:
            cells = l.split("|")
            cells[2] = " ~~" + cells[2].strip() + "~~ ✅ "
            lines[i] = "|".join(cells)

    with open(MD, "w", encoding="utf-8") as f:
        f.writelines(lines)
    print(f"步骤 {step} 已在 MD 中划掉（{end - start} 行范围）")


if __name__ == "__main__":
    main(int(sys.argv[1]))