#!/usr/bin/env python3
"""生成种子演示封面图（SVG）：30 张新闻封面 + 3 张首页轮播横幅。"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "backend", "AdPulse.Api", "wwwroot", "covers")
os.makedirs(OUT, exist_ok=True)

PALETTES = [
    ("品牌营销", "#FF5A1F", "#3A1206"),
    ("数字广告", "#38BDF8", "#082A44"),
    ("创意案例", "#F59E0B", "#3D2506"),
    ("媒介投放", "#34D399", "#06342A"),
    ("行业动态", "#A5B4CF", "#161D2E"),
]

COVER_TPL = """<svg xmlns="http://www.w3.org/2000/svg" width="800" height="450" viewBox="0 0 800 450">
<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
<stop offset="0" stop-color="#0B0E14"/><stop offset="1" stop-color="{dark}"/></linearGradient>
<radialGradient id="r" cx="0.85" cy="0.15" r="1.1">
<stop offset="0" stop-color="{accent}" stop-opacity="0.55"/><stop offset="0.55" stop-color="{accent}" stop-opacity="0.08"/><stop offset="1" stop-color="{accent}" stop-opacity="0"/>
</radialGradient></defs>
<rect width="800" height="450" fill="url(#g)"/><rect width="800" height="450" fill="url(#r)"/>
<text x="620" y="400" font-family="Arial, sans-serif" font-size="220" font-weight="800" fill="{accent}" opacity="0.16" text-anchor="middle">{num:02d}</text>
<rect x="48" y="52" width="10" height="10" fill="{accent}"/>
<text x="72" y="62" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="22" fill="#E8EAF0" opacity="0.9">AdPulse · 广告脉搏</text>
<text x="48" y="250" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="52" font-weight="700" fill="#F5F7FA">{cat}</text>
<text x="48" y="300" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="24" fill="#9AA3B8">行业情报 · 演示封面</text>
<rect x="48" y="330" width="120" height="4" fill="{accent}"/>
</svg>"""

BANNER_TPL = """<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="640" viewBox="0 0 1600 640">
<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
<stop offset="0" stop-color="#0B0E14"/><stop offset="1" stop-color="{dark}"/></linearGradient>
<radialGradient id="r" cx="0.8" cy="0.2" r="1.2">
<stop offset="0" stop-color="{accent}" stop-opacity="0.6"/><stop offset="0.6" stop-color="{accent}" stop-opacity="0.08"/><stop offset="1" stop-color="{accent}" stop-opacity="0"/>
</radialGradient></defs>
<rect width="1600" height="640" fill="url(#g)"/><rect width="1600" height="640" fill="url(#r)"/>
<rect x="90" y="120" width="14" height="14" fill="{accent}"/>
<text x="122" y="133" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="28" fill="#E8EAF0" opacity="0.9">AdPulse 头条 · {cat}</text>
<text x="90" y="330" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="64" font-weight="800" fill="#F5F7FA">{line1}</text>
<text x="90" y="420" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="64" font-weight="800" fill="#F5F7FA">{line2}</text>
<rect x="90" y="470" width="180" height="6" fill="{accent}"/>
<text x="90" y="540" font-family="'Noto Sans CJK SC','PingFang SC','Microsoft YaHei',sans-serif" font-size="26" fill="#9AA3B8">演示数据 · 点击查看全文</text>
</svg>"""

for i in range(1, 31):
    cat, accent, dark = PALETTES[(i - 1) % 5]
    with open(os.path.join(OUT, f"cover-{i:02d}.svg"), "w", encoding="utf-8") as f:
        f.write(COVER_TPL.format(cat=cat, accent=accent, dark=dark, num=i))

banners = [
    ("品牌营销", "#FF5A1F", "#3A1206", "星驰汽车官宣全球代言人", "首支品牌 TVC 全网上线"),
    ("数字广告", "#38BDF8", "#082A44", "数字广告投放规模增长 18%", "短视频媒介占比首次过半"),
    ("创意案例", "#F59E0B", "#3D2506", "戛纳创意节中国军团", "斩获 12 项大奖"),
]
for i, (cat, accent, dark, l1, l2) in enumerate(banners, 1):
    with open(os.path.join(OUT, f"banner-{i}.svg"), "w", encoding="utf-8") as f:
        f.write(BANNER_TPL.format(cat=cat, accent=accent, dark=dark, line1=l1, line2=l2))

print("covers generated:", len(os.listdir(OUT)))