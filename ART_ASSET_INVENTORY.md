# ART_ASSET_INVENTORY

本文件记录 `Assets/Arts` 中的美术、音频、UI 原始资源，用于后续场景搭建和资源接入。

## 维护规则

- `Assets/Arts` 作为中文源文件仓库，不主动大规模重命名中文资源。
- 代码或运行时需要引用的资源，另存/导出到英文路径，例如 `Assets/Resources/UI/...`。
- 后续新增资源时，优先追加到本文档对应分类；不要覆盖已有记录。
- 脚本处理中文/特殊字符路径时使用 UTF-8、`pathlib.Path` 和 PowerShell `-LiteralPath`。

## 总览

- 扫描目录：`Assets/Arts`
- 当前资源总数：`185` 个，不含 `.meta`
- 顶层分组数：`14`

### 文件类型统计

| 类型 | 数量 |
| --- | ---: |
| `.png` | 117 |
| `.wav` | 27 |
| `.psd` | 12 |
| `.jpg` | 11 |
| `.psb` | 8 |
| `.mov` | 4 |
| `.mp3` | 3 |
| `.mp4` | 2 |
| `.ttf` | 1 |

### 顶层目录统计

| 分组 | 数量 | 大小 | 主要类型 |
| --- | ---: | ---: | --- |
| `BGM` | 30 | 63.59 MB | `.wav`×27, `.mp3`×3 |
| `CoverUI(1).psd` | 1 | 2.65 MB | `.psd`×1 |
| `H0` | 6 | 2.40 MB | `.png`×6 |
| `公园` | 5 | 31.41 MB | `.jpg`×2, `.psb`×2, `.png`×1 |
| `动物` | 16 | 21.43 MB | `.png`×15, `.mp4`×1 |
| `动物补充及参考` | 10 | 8.13 MB | `.png`×9, `.mp4`×1 |
| `卧室` | 2 | 2.96 MB | `.jpg`×2 |
| `客厅` | 4 | 22.75 MB | `.jpg`×2, `.png`×1, `.psb`×1 |
| `客厅拆件` | 2 | 17.23 MB | `.png`×1, `.psb`×1 |
| `床下` | 5 | 35.33 MB | `.jpg`×2, `.psb`×2, `.png`×1 |
| `物品` | 35 | 114.68 MB | `.png`×29, `.psd`×5, `.jpg`×1 |
| `至0515ui汇总` | 46 | 103.87 MB | `.png`×35, `.psd`×6, `.mov`×4, `.ttf`×1 |
| `阳台` | 6 | 37.76 MB | `.jpg`×2, `.psb`×2, `.png`×2 |
| `黑猫` | 17 | 0.66 MB | `.png`×17 |

## 使用建议

- 场景搭建优先看：`客厅`、`卧室`、`阳台`、`公园`、`床下`。
- 交互物体优先看：`物品` 下的 H0/H1/H2/L1/L2/L3/L4/L5、日历、风铃。
- UI 接入优先看：`至0515ui汇总`，其中对话框和字体已经有运行时导出版本。
- 音频接入优先看：`BGM/BGM` 和 `BGM/BGM_all`，后续可按 BGM、环境音、交互音效、结局音频拆分。
- `动物补充及参考` 更像参考/备份区，接入正式场景前建议确认是否使用 `动物` 目录中的版本。

## 分类清单

### `BGM`

- 说明：音乐与环境音资源。
- 资源数：`30`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/BGM/BGM/Ending1.mp3` | `.mp3` | 音频 | 1.61 MB |
| `Assets/Arts/BGM/BGM/Ending2.wav` | `.wav` | 音频 | 11.91 MB |
| `Assets/Arts/BGM/BGM/interface.mp3` | `.mp3` | 音频 | 4.22 MB |
| `Assets/Arts/BGM/BGM/living room-surface.mp3` | `.mp3` | 音频 | 4.11 MB |
| `Assets/Arts/BGM/BGM/park-inner world.wav` | `.wav` | 音频 | 10.09 MB |
| `Assets/Arts/BGM/BGM_all/叼项圈（L1）.wav` | `.wav` | 音频 | 349.1 KB |
| `Assets/Arts/BGM/BGM_all/回头看（H3）.wav` | `.wav` | 音频 | 308.0 KB |
| `Assets/Arts/BGM/BGM_all/安葬橘猫.wav` | `.wav` | 音频 | 1.13 MB |
| `Assets/Arts/BGM/BGM_all/安葬项圈.wav` | `.wav` | 音频 | 3.37 MB |
| `Assets/Arts/BGM/BGM_all/安葬鹦鹉.wav` | `.wav` | 音频 | 366.8 KB |
| `Assets/Arts/BGM/BGM_all/对话框音效.wav` | `.wav` | 音频 | 87.8 KB |
| `Assets/Arts/BGM/BGM_all/推笼门.wav` | `.wav` | 音频 | 255.3 KB |
| `Assets/Arts/BGM/BGM_all/检查存钱罐.wav` | `.wav` | 音频 | 264.0 KB |
| `Assets/Arts/BGM/BGM_all/检查存钱罐（H4）.wav` | `.wav` | 音频 | 462.2 KB |
| `Assets/Arts/BGM/BGM_all/检查食槽（H3）.wav` | `.wav` | 音频 | 28.2 KB |
| `Assets/Arts/BGM/BGM_all/橘猫出现.wav` | `.wav` | 音频 | 270.0 KB |
| `Assets/Arts/BGM/BGM_all/物品调查_触碰音效 (1).wav` | `.wav` | 音频 | 101.5 KB |
| `Assets/Arts/BGM/BGM_all/独白开始.wav` | `.wav` | 音频 | 5.12 MB |
| `Assets/Arts/BGM/BGM_all/猫粮.wav` | `.wav` | 音频 | 2.65 MB |
| `Assets/Arts/BGM/BGM_all/直接开箱（L3）.wav` | `.wav` | 音频 | 152.4 KB |
| `Assets/Arts/BGM/BGM_all/真相揭露（黑屏）.wav` | `.wav` | 音频 | 11.43 MB |
| `Assets/Arts/BGM/BGM_all/移动脚步声.wav` | `.wav` | 音频 | 11.8 KB |
| `Assets/Arts/BGM/BGM_all/触摸镜子.wav` | `.wav` | 音频 | 271.0 KB |
| `Assets/Arts/BGM/BGM_all/调查手机.wav` | `.wav` | 音频 | 285.9 KB |
| `Assets/Arts/BGM/BGM_all/调查相框.wav` | `.wav` | 音频 | 125.7 KB |
| `Assets/Arts/BGM/BGM_all/进入公园wav.wav` | `.wav` | 音频 | 1.17 MB |
| `Assets/Arts/BGM/BGM_all/进入阳台.wav` | `.wav` | 音频 | 1.52 MB |
| `Assets/Arts/BGM/BGM_all/进入阳台里世界.wav` | `.wav` | 音频 | 772.5 KB |
| `Assets/Arts/BGM/BGM_all/道具拾取放大音效.wav` | `.wav` | 音频 | 149.6 KB |
| `Assets/Arts/BGM/BGM_all/黑屏转场.wav` | `.wav` | 音频 | 1.08 MB |

### `CoverUI(1).psd`

- 说明：Start 封面源文件；运行时已导出到 `Assets/Resources/UI/Start/CoverUI.png`。
- 资源数：`1`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/CoverUI(1).psd` | `.psd` | 源文件 | 2.65 MB |

### `H0`

- 说明：H0 药瓶资源；与 `物品/H0：抗抑郁药瓶` 存在重复。
- 资源数：`6`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/H0/H0-1.PNG` | `.png` | 1000×1000 | 309.3 KB |
| `Assets/Arts/H0/H0-2.PNG` | `.png` | 1000×1000 | 316.3 KB |
| `Assets/Arts/H0/H0-3.PNG` | `.png` | 1000×1000 | 386.3 KB |
| `Assets/Arts/H0/H0-4.PNG` | `.png` | 1000×1000 | 503.9 KB |
| `Assets/Arts/H0/H0-open-1.PNG` | `.png` | 1000×1000 | 611.9 KB |
| `Assets/Arts/H0/H0-open-2.PNG` | `.png` | 1000×1000 | 331.8 KB |

### `公园`

- 说明：公园场景背景与 PSB 源文件。
- 资源数：`5`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/公园/moon park.jpg` | `.jpg` | 1920×1080 | 3.59 MB |
| `Assets/Arts/公园/moon park.psb` | `.psb` | 源文件 | 12.58 MB |
| `Assets/Arts/公园/park.jpg` | `.jpg` | 1920×1080 | 2.13 MB |
| `Assets/Arts/公园/park.psb` | `.psb` | 源文件 | 12.51 MB |
| `Assets/Arts/公园/公园p n g.png` | `.png` | 1920×1080 | 604.3 KB |

### `动物`

- 说明：正式动物素材：狗、橘猫、黑猫、鸟。
- 资源数：`16`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/动物/橘猫/GingerCat-1.PNG` | `.png` | 1139×825 | 484.7 KB |
| `Assets/Arts/动物/橘猫/GingerCat-2.PNG` | `.png` | 1139×825 | 471.7 KB |
| `Assets/Arts/动物/橘猫/效果参考.PNG` | `.png` | 2178×1737 | 5.52 MB |
| `Assets/Arts/动物/橘猫/效果参考1.PNG` | `.png` | 2732×1534 | 5.53 MB |
| `Assets/Arts/动物/橘猫/橘猫-1.png` | `.png` | 2407×1741 | 2.44 MB |
| `Assets/Arts/动物/狗/Dog-1-light.PNG` | `.png` | 1139×825 | 515.5 KB |
| `Assets/Arts/动物/狗/Dog-1.PNG` | `.png` | 1139×825 | 506.6 KB |
| `Assets/Arts/动物/狗/Dog-2-light.PNG` | `.png` | 1139×825 | 458.3 KB |
| `Assets/Arts/动物/狗/Dog-2.PNG` | `.png` | 1139×825 | 445.6 KB |
| `Assets/Arts/动物/狗/L1项圈狗牌138/IMG_9351(1).PNG` | `.png` | 1000×1000 | 353.0 KB |
| `Assets/Arts/动物/狗/光线参考，可能需要两张混用.PNG` | `.png` | 1920×1080 | 3.83 MB |
| `Assets/Arts/动物/鸟/Bird-1.PNG` | `.png` | 1139×825 | 176.0 KB |
| `Assets/Arts/动物/黑猫/IMG_9289.MP4` | `.mp4` | 视频/参考 | 102.0 KB |
| `Assets/Arts/动物/黑猫/IMG_9292.PNG` | `.png` | 1139×825 | 325.8 KB |
| `Assets/Arts/动物/黑猫/正面-1.png` | `.png` | 1000×720 | 178.1 KB |
| `Assets/Arts/动物/黑猫/正面-2.png` | `.png` | 1000×720 | 181.4 KB |

### `动物补充及参考`

- 说明：动物补充参考素材，可能包含重复或待调整版本。
- 资源数：`10`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/动物补充及参考/动物补充及参考/橘猫/橘猫-1.png` | `.png` | 2407×1741 | 2.44 MB |
| `Assets/Arts/动物补充及参考/动物补充及参考/橘猫/橘猫-体型配色区分尝试.png` | `.png` | 2155×1520 | 2.87 MB |
| `Assets/Arts/动物补充及参考/动物补充及参考/橘猫/橘猫-蜷缩待调.png` | `.png` | 384×278 | 132.9 KB |
| `Assets/Arts/动物补充及参考/动物补充及参考/狗/生成姿势待调-1.png` | `.png` | 384×278 | 107.8 KB |
| `Assets/Arts/动物补充及参考/动物补充及参考/狗/生成姿势待调-2.png` | `.png` | 384×278 | 97.8 KB |
| `Assets/Arts/动物补充及参考/动物补充及参考/狗/绘制调整.png` | `.png` | 2407×1741 | 1.72 MB |
| `Assets/Arts/动物补充及参考/动物补充及参考/黑猫/IMG_9289.MP4` | `.mp4` | 视频/参考 | 102.0 KB |
| `Assets/Arts/动物补充及参考/动物补充及参考/黑猫/IMG_9292.PNG` | `.png` | 1139×825 | 325.8 KB |
| `Assets/Arts/动物补充及参考/动物补充及参考/黑猫/正面-1.png` | `.png` | 1000×720 | 178.1 KB |
| `Assets/Arts/动物补充及参考/动物补充及参考/黑猫/正面-2.png` | `.png` | 1000×720 | 181.4 KB |

### `卧室`

- 说明：卧室场景背景。
- 资源数：`2`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/卧室/Bedroom.jpg` | `.jpg` | 1920×1080 | 1.37 MB |
| `Assets/Arts/卧室/moon Bedroom.jpg` | `.jpg` | 1920×1080 | 1.59 MB |

### `客厅`

- 说明：客厅场景背景与物件合图。
- 资源数：`4`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/客厅/Living room items.png` | `.png` | 2134×1238 | 2.25 MB |
| `Assets/Arts/客厅/living room.jpg` | `.jpg` | 1920×1080 | 2.09 MB |
| `Assets/Arts/客厅/moon living room.jpg` | `.jpg` | 1920×1080 | 3.30 MB |
| `Assets/Arts/客厅/moon living room.psb` | `.psb` | 源文件 | 15.11 MB |

### `客厅拆件`

- 说明：客厅拆件和源文件。
- 资源数：`2`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/客厅拆件/Living room items.png` | `.png` | 2134×1238 | 2.25 MB |
| `Assets/Arts/客厅拆件/living room.psb` | `.psb` | 源文件 | 14.98 MB |

### `床下`

- 说明：床下场景背景与源文件。
- 资源数：`5`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/床下/moon under the bed .jpg` | `.jpg` | 1920×1080 | 3.53 MB |
| `Assets/Arts/床下/moon under the bed .psb` | `.psb` | 源文件 | 12.84 MB |
| `Assets/Arts/床下/under the bed.jpg` | `.jpg` | 1920×1080 | 2.08 MB |
| `Assets/Arts/床下/under the bed.psb` | `.psb` | 源文件 | 14.18 MB |
| `Assets/Arts/床下/卧室床下.png` | `.png` | 2180×1220 | 2.70 MB |

### `物品`

- 说明：交互物体、解谜物、记忆物等。
- 资源数：`35`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/物品/3d72e0aef8138dcc529807b28392e552.png` | `.png` | 1499×1773 | 507.2 KB |
| `Assets/Arts/物品/a354c419fef89d52a55413185cda675f.png` | `.png` | 1499×1773 | 942.1 KB |
| `Assets/Arts/物品/dccc4b5dab4df76d73b4041908765392.png` | `.png` | 1499×1773 | 1.08 MB |
| `Assets/Arts/物品/e0357373d5668bbc7f71be18ac4c29ba.png` | `.png` | 1499×1773 | 1.92 MB |
| `Assets/Arts/物品/H0：抗抑郁药瓶/H0-1.PNG` | `.png` | 1000×1000 | 309.3 KB |
| `Assets/Arts/物品/H0：抗抑郁药瓶/H0-2.PNG` | `.png` | 1000×1000 | 316.3 KB |
| `Assets/Arts/物品/H0：抗抑郁药瓶/H0-3.PNG` | `.png` | 1000×1000 | 386.3 KB |
| `Assets/Arts/物品/H0：抗抑郁药瓶/H0-4.PNG` | `.png` | 1000×1000 | 503.9 KB |
| `Assets/Arts/物品/H0：抗抑郁药瓶/H0-open-1.PNG` | `.png` | 1000×1000 | 611.9 KB |
| `Assets/Arts/物品/H0：抗抑郁药瓶/H0-open-2.PNG` | `.png` | 1000×1000 | 331.8 KB |
| `Assets/Arts/物品/H1碎屏手机/phone1-off.PNG` | `.png` | 1809×2316 | 2.88 MB |
| `Assets/Arts/物品/H1碎屏手机/phone1-on.PNG` | `.png` | 1809×2316 | 3.44 MB |
| `Assets/Arts/物品/H1碎屏手机/phone1.PNG` | `.png` | 2048×2048 | 1.82 MB |
| `Assets/Arts/物品/H1碎屏手机/phone2-off.PNG` | `.png` | 1809×2316 | 2.83 MB |
| `Assets/Arts/物品/H1碎屏手机/phone2-on.PNG` | `.png` | 1809×2316 | 3.39 MB |
| `Assets/Arts/物品/H2空相框/IMG_9409.PNG` | `.png` | 2048×2048 | 3.67 MB |
| `Assets/Arts/物品/L1项圈狗牌138/necklace.PNG` | `.png` | 1000×1000 | 353.0 KB |
| `Assets/Arts/物品/L2沙发照片/photo-front.PNG` | `.png` | 2730×1535 | 2.43 MB |
| `Assets/Arts/物品/L2沙发照片/Photo.psd` | `.psd` | 源文件 | 16.22 MB |
| `Assets/Arts/物品/L2沙发照片/内容预览.PNG` | `.png` | 2200×1200 | 3.66 MB |
| `Assets/Arts/物品/L3航空箱标签/label.PNG` | `.png` | 1500×1500 | 1.62 MB |
| `Assets/Arts/物品/L4日记/diary.psd` | `.psd` | 源文件 | 44.94 MB |
| `Assets/Arts/物品/L5鹦鹉羽毛/feather.PNG` | `.png` | 2048×2048 | 1.12 MB |
| `Assets/Arts/物品/日历/3900(2).psd` | `.psd` | 源文件 | 7.50 MB |
| `Assets/Arts/物品/日历/calender-1.PNG` | `.png` | 2107×1534 | 2.07 MB |
| `Assets/Arts/物品/日历/内容预览.jpg` | `.jpg` | 1483×1080 | 126.3 KB |
| `Assets/Arts/物品/风铃/新版/windChime.PNG` | `.png` | 1500×1500 | 916.8 KB |
| `Assets/Arts/物品/风铃/新版/windchime.psd` | `.psd` | 源文件 | 3.93 MB |
| `Assets/Arts/物品/风铃/旧版/1.PNG` | `.png` | 1000×1500 | 192.5 KB |
| `Assets/Arts/物品/风铃/旧版/2.PNG` | `.png` | 1000×1500 | 221.3 KB |
| `Assets/Arts/物品/风铃/旧版/3.PNG` | `.png` | 1000×1500 | 201.8 KB |
| `Assets/Arts/物品/风铃/旧版/4.PNG` | `.png` | 1000×1500 | 236.9 KB |
| `Assets/Arts/物品/风铃/旧版/5.PNG` | `.png` | 1000×1500 | 271.0 KB |
| `Assets/Arts/物品/风铃/旧版/windchimes.PNG` | `.png` | 1000×1500 | 781.1 KB |
| `Assets/Arts/物品/风铃/旧版/Wingdc.psd` | `.psd` | 源文件 | 3.12 MB |

### `至0515ui汇总`

- 说明：UI、对话框、按钮、选项框、背包弹窗、解谜补充、字体。
- 资源数：`46`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/bury2.png` | `.png` | 206×114 | 8.6 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/collage2.png` | `.png` | 206×114 | 8.8 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/Dimming2.png` | `.png` | 206×114 | 9.0 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/eat2.png` | `.png` | 206×114 | 7.8 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/inspect2.png` | `.png` | 177×114 | 8.2 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/investigate2.png` | `.png` | 177×114 | 8.0 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/open2.png` | `.png` | 177×114 | 7.7 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/pickit2.png` | `.png` | 177×114 | 7.8 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/pushdown2.png` | `.png` | 206×114 | 9.0 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/touch2.png` | `.png` | 177×114 | 8.0 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/淡金色-正常尺寸/turnHead2.png` | `.png` | 177×114 | 7.7 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/bury1.png` | `.png` | 424×235 | 18.2 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/collage1.png` | `.png` | 424×235 | 18.7 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/Dimming.png` | `.png` | 424×235 | 19.2 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/eat1.png` | `.png` | 424×235 | 16.7 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/inspect1.png` | `.png` | 367×236 | 17.3 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/investigate1.png` | `.png` | 367×236 | 16.9 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/open1.png` | `.png` | 367×236 | 16.1 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/pickit1.png` | `.png` | 367×236 | 16.5 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/pushDown1.png` | `.png` | 424×235 | 19.2 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/read1.PNG` | `.png` | 367×236 | 24.0 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/touch1.png` | `.png` | 367×236 | 17.1 KB |
| `Assets/Arts/至0515ui汇总/交互按钮/深褐色-尺寸过大，需x0.45左右/turnHead1.png` | `.png` | 367×236 | 16.3 KB |
| `Assets/Arts/至0515ui汇总/对话框/Dialog(2).psd` | `.psd` | 源文件 | 971.9 KB |
| `Assets/Arts/至0515ui汇总/对话框/鸿雷小纸条青春体(2).ttf` | `.ttf` | 字体 | 5.95 MB |
| `Assets/Arts/至0515ui汇总/提示系统/hiddenClue.PNG` | `.png` | 250×160 | 15.6 KB |
| `Assets/Arts/至0515ui汇总/提示系统/interctive.PNG` | `.png` | 150×150 | 13.5 KB |
| `Assets/Arts/至0515ui汇总/提示系统/memory1.PNG` | `.png` | 250×160 | 19.1 KB |
| `Assets/Arts/至0515ui汇总/提示系统/memory2.PNG` | `.png` | 250×160 | 24.0 KB |
| `Assets/Arts/至0515ui汇总/背包与弹窗/B a g.psd` | `.psd` | 源文件 | 11.39 MB |
| `Assets/Arts/至0515ui汇总/背包与弹窗/system.psd` | `.psd` | 源文件 | 11.57 MB |
| `Assets/Arts/至0515ui汇总/解谜系统补充/diary/DiaryNew.psd` | `.psd` | 源文件 | 21.97 MB |
| `Assets/Arts/至0515ui汇总/解谜系统补充/photo/Photo-light.psd` | `.psd` | 源文件 | 16.63 MB |
| `Assets/Arts/至0515ui汇总/解谜系统补充/wind/Windchime(1).psd` | `.psd` | 源文件 | 5.84 MB |
| `Assets/Arts/至0515ui汇总/解谜系统补充/wind/音波.mov` | `.mov` | 视频/参考 | 6.84 MB |
| `Assets/Arts/至0515ui汇总/解谜系统补充/wind/音波1+.mov` | `.mov` | 视频/参考 | 6.84 MB |
| `Assets/Arts/至0515ui汇总/解谜系统补充/wind/音波1.mov` | `.mov` | 视频/参考 | 6.84 MB |
| `Assets/Arts/至0515ui汇总/解谜系统补充/wind/音波5.mov` | `.mov` | 视频/参考 | 6.84 MB |
| `Assets/Arts/至0515ui汇总/选项框/envolop1/envolop1-depart.PNG` | `.png` | 1353×995 | 245.6 KB |
| `Assets/Arts/至0515ui汇总/选项框/envolop1/envolop1-leave.PNG` | `.png` | 1353×995 | 232.6 KB |
| `Assets/Arts/至0515ui汇总/选项框/envolop1/envolop1-notselected.PNG` | `.png` | 1353×995 | 164.1 KB |
| `Assets/Arts/至0515ui汇总/选项框/envolop1/envolop1-selescted.PNG` | `.png` | 1353×995 | 295.4 KB |
| `Assets/Arts/至0515ui汇总/选项框/envolop2/envolop1-depart.PNG` | `.png` | 1353×995 | 243.5 KB |
| `Assets/Arts/至0515ui汇总/选项框/envolop2/envolop1-leave.PNG` | `.png` | 1353×995 | 230.2 KB |
| `Assets/Arts/至0515ui汇总/选项框/envolop2/envolop1-notselected.PNG` | `.png` | 1353×995 | 164.4 KB |
| `Assets/Arts/至0515ui汇总/选项框/envolop2/envolop1-selected.PNG` | `.png` | 1353×995 | 295.3 KB |

### `阳台`

- 说明：阳台场景背景与源文件。
- 资源数：`6`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/阳台/moon terrace.jpg` | `.jpg` | 1920×1080 | 3.16 MB |
| `Assets/Arts/阳台/moon terrace.psb` | `.psb` | 源文件 | 13.56 MB |
| `Assets/Arts/阳台/moonterrace.png` | `.png` | 2281×1322 | 1.95 MB |
| `Assets/Arts/阳台/terrace.jpg` | `.jpg` | 1920×1080 | 2.11 MB |
| `Assets/Arts/阳台/terrace.psb` | `.psb` | 源文件 | 14.88 MB |
| `Assets/Arts/阳台/terracepng.png` | `.png` | 2365×1349 | 2.09 MB |

### `黑猫`

- 说明：黑猫身体部件拆图，适合动画和拼装。
- 资源数：`17`

| 路径 | 类型 | 尺寸/备注 | 大小 |
| --- | --- | --- | ---: |
| `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eye.PNG` | `.png` | 1139×825 | 29.2 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eyePupil.PNG` | `.png` | 1139×825 | 20.6 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-face.PNG` | `.png` | 1139×825 | 47.5 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-LeftEar.PNG` | `.png` | 1139×825 | 37.3 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-RIghtEar.PNG` | `.png` | 1139×825 | 30.4 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-尾/BlackCat-Tail-1.PNG` | `.png` | 1139×825 | 36.5 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-尾/BlackCat-Tail-2.PNG` | `.png` | 1139×825 | 29.7 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-尾/BlackCat-Tail-3.PNG` | `.png` | 1139×825 | 26.1 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-身体/BlackCat-Body-back.PNG` | `.png` | 1139×825 | 60.2 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-身体/BlackCat-Body-Front.PNG` | `.png` | 1139×825 | 67.8 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-身体/BlackCat-Body-LeftFrontLeg-1.PNG` | `.png` | 1139×825 | 43.3 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-身体/BlackCat-Body-LeftFrontLeg-2.PNG` | `.png` | 1139×825 | 29.8 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-身体/BlackCat-Body-LeftHindLeg.PNG` | `.png` | 1139×825 | 51.8 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-身体/BlackCat-Body-RightFrontLeg-1.PNG` | `.png` | 1139×825 | 29.1 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-身体/BlackCat-Body-RightFrontLeg-2.PNG` | `.png` | 1139×825 | 31.8 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-身体/BlackCat-Body-RightHindLeg.PNG` | `.png` | 1139×825 | 44.0 KB |
| `Assets/Arts/黑猫/黑猫/黑猫-身体/BlackCat-Body-Waist.PNG` | `.png` | 1139×825 | 62.2 KB |

## 后续更新记录

- 2026-05-18：首次根据 `Assets/Arts` 自动扫描生成清单。
- 后续新增/替换美术或音乐资源时，在这里追加日期、路径、用途、是否已接入运行时。
