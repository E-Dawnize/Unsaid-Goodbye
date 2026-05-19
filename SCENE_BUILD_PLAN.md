# SCENE_BUILD_PLAN

本文件是场景搭建施工表：记录每个场景需要的背景、动物、物品、交互定义、对白 ID、音频和待补项。

## 维护规则

- 不大规模重命名 `Assets/Arts` 中的中文源文件；运行时资源需要稳定引用时，再导出到英文路径。
- 每次新增场景、交互物、音频接入后，同步更新本文档的现状和待办。
- `dialogue_id` 不改名，继续使用同学主线现有 DialogueSequence ID。

## 当前工作目标

- 主线客厅：使用 `Assets/Scenes/OutterWorld.unity`。
- 备份场景：`Assets/Scenes/OutterWorld_1.unity` 是复制件，暂不维护。
- Phase2 推进：`DogLookBack` 和 `BuryCollar` 需要作为进入 Phase3 前的必要流程。
- 结局背景：暂时没有专用背景图，先用现有资源或占位方式跑通。
- 解谜玩法：本阶段不接入，先完成所有场景搭建、美术/音乐接入、剧情推进。

## 当前场景现状

| 场景 | 文件状态 | 已识别交互 |
| --- | --- | --- |
| `Assets/Scenes/Start.unity` | 已存在 | 暂无/未识别 |
| `Assets/Scenes/OutterWorld.unity` | 已存在 | `Def_CatBowl`, `Def_BalconyEntrance`, `Def_WallFrame`, `Def_SofaPhone`, `Def_PillBottle` |
| `Assets/Scenes/OutterWorld_1.unity` | 已存在但暂不使用 | `Def_CatBowl`, `Def_BalconyEntrance`, `Def_WallFrame`, `Def_SofaPhone`, `Def_PillBottle` |
| `Assets/Scenes/Balcony.unity` | 已存在 | `Def_BalconyCorner`, `Def_BalconyJump` |
| `Assets/Scenes/InnerWorld_Park.unity` | 已存在 | 暂无/未识别 |
| `Assets/Scenes/InnerWorld_Bedroom.unity` | 待新建 | 暂无/未识别 |
| `Assets/Scenes/InnerWorld_Balcony.unity` | 待新建 | 暂无/未识别 |
| `Assets/Scenes/InnerWorld_LivingRoom.unity` | 待新建 | 暂无/未识别 |
| `Assets/Scenes/Epilogue_Stay.unity` | 待新建 | 暂无/未识别 |
| `Assets/Scenes/Epilogue_Leave.unity` | 待新建 | 暂无/未识别 |

## 阶段搭建总表

| 阶段 | 目标场景 | 场景状态 | 核心对白 | 核心交互 |
| --- | --- | --- | --- | --- |
| Start | `Assets/Scenes/Start.unity` | 已搭建封面 UI | 无 | `StartHotspot -> StartGameCommand`, `ExitHotspot -> Application.Quit` |
| Phase1 表世界客厅 | `Scenes/OutterWorld` | 已有场景与主要交互 | `dialogue_phase1_wakeup`, `dialogue_cat_bowl_inspect`, `dialogue_h0_pillbottle`, `dialogue_h1_phone`, `dialogue_h2_frame` | `Def_CatBowl`, `Def_PillBottle`, `Def_SofaPhone`, `Def_WallFrame`, `Def_BalconyEntrance` |
| Phase1 阳台 | `Scenes/Balcony` | 已有基础场景与 2 个交互 | `dialogue_h3_balcony_corner`, `dialogue_balcony_jump` | `Def_BalconyCorner`, `Def_BalconyJump` |
| Phase2 里世界公园 | `Scenes/InnerWorld_Park` | 已挂载 3 个核心交互，需 Unity 内测试位置 | `dialogue_phase2_intro`, `dialogue_l1_dog_encounter`, `dialogue_dog_lookback`, `dialogue_bury_collar` | `Def_DogCollar`, `Def_DogLookBack`, `Def_BuryCollar` |
| Phase3 返回客厅/照片 | `Scenes/OutterWorld` | 复用客厅，需新增 Phase3 显隐状态 | `dialogue_phase3_return_home`, `dialogue_catbowl_return`, `dialogue_l2_sofa_photo` | `Def_CatBowlReturn`, `Def_SofaPhoto` |
| Phase4 里世界卧室 | `Scenes/Old Home Bedroom` | 已搭建最小触发版，需 Unity 内微调位置/显隐 | `dialogue_phase4_bedroom_intro`, `dialogue_l3_crate_label`, `dialogue_h4_savings`, `dialogue_l4_wall_diary`, `dialogue_bury_orange_cat` | `Def_CrateLabel`, `Def_BedCornerSavings`, `Def_WallDiary`, `Def_BuryOrangeCat` |
| Phase5 里世界阳台/鹦鹉 | `Scenes/Balcony 2` | 已搭建最小触发版，需 Unity 内微调位置/显隐 | `dialogue_phase5_balcony_intro`, `dialogue_cage_door`, `dialogue_cage_water`, `dialogue_l5_parrot_feather` | `Def_CageDoor`, `Def_CageWater`, `Def_ParrotFeather` |
| Phase6 里世界客厅/真相 | `Scenes/OutterWorld_Living Room 3` | 已搭建最小触发版，需 Unity 内微调镜子/主人位置 | `dialogue_phase6_mirror_intro`, `dialogue_mirror_touch`, `dialogue_owner_reveal`, `dialogue_ending_choice` | `Def_LivingRoomMirror`, `Def_SofaOwner`, `Def_EndingChoice` |
| Phase7A 后日谈：留下 | `Scenes/Epilogue_Stay` | 目标场景待新建 | `dialogue_epilogue_stay` | `Def_EpilogueStay` |
| Phase7B 后日谈：离开 | `Scenes/Epilogue_Leave` | 目标场景待新建 | `dialogue_epilogue_leave` | `Def_EpilogueLeave` |

## 逐场景施工清单

### Start

- 目标场景：`Assets/Scenes/Start.unity`
- 当前状态：已搭建封面 UI
- 备注：Continue / Setup 仅保留在封面图里，暂不响应。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | `Assets/Resources/UI/Start/CoverUI.png` | 已找到 |
|  | `Assets/Arts/CoverUI(1).psd` | 已找到 |
| 动物/角色 | 无 | - |
| 物品/触发点 | Start 透明热区 | 待场景中确认/摆放 |
|  | Exit 透明热区 | 待场景中确认/摆放 |
| 对白 | 无 | - |
| 交互定义 | StartHotspot -> StartGameCommand | 待场景中确认/摆放 |
|  | ExitHotspot -> Application.Quit | 待场景中确认/摆放 |
| 音频 | `Assets/Arts/BGM/BGM/interface.mp3` | 已找到 |

### Phase1 表世界客厅

- 目标场景：`Scenes/OutterWorld`
- 当前状态：已有场景与主要交互
- 备注：`OutterWorld.unity` 和 `OutterWorld_1.unity` 需确认主线使用哪个。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | `Assets/Arts/客厅/Living room items.png` | 已找到 |
|  | `Assets/Arts/客厅/living room.jpg` | 已找到 |
|  | `Assets/Arts/客厅/moon living room.jpg` | 已找到 |
|  | `Assets/Arts/客厅/moon living room.psb` | 已找到 |
|  | `Assets/Arts/客厅拆件/Living room items.png` | 已找到 |
|  | `Assets/Arts/客厅拆件/living room.psb` | 已找到 |
| 动物/角色 | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-LeftEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-RIghtEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eye.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eyePupil.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-face.PNG` | 已找到 |
| 物品/触发点 | 猫碗 | 待场景中确认/摆放 |
|  | H0 抗抑郁药瓶 | 待场景中确认/摆放 |
|  | H1 碎屏手机 | 待场景中确认/摆放 |
|  | H2 空相框 | 待场景中确认/摆放 |
|  | 阳台入口 | 待场景中确认/摆放 |
| 对白 | `dialogue_phase1_wakeup` | 已有 DialogueSequence |
|  | `dialogue_cat_bowl_inspect` | 已有 DialogueSequence |
|  | `dialogue_h0_pillbottle` | 已有 DialogueSequence |
|  | `dialogue_h1_phone` | 已有 DialogueSequence |
|  | `dialogue_h2_frame` | 已有 DialogueSequence |
| 交互定义 | `Def_CatBowl` | 已有 InteractionDef，需挂到场景物体 |
|  | `Def_PillBottle` | 已有 InteractionDef，需挂到场景物体 |
|  | `Def_SofaPhone` | 已有 InteractionDef，需挂到场景物体 |
|  | `Def_WallFrame` | 已有 InteractionDef，需挂到场景物体 |
|  | `Def_BalconyEntrance` | 已有 InteractionDef，需挂到场景物体 |
| 音频 | `Assets/Arts/BGM/BGM/living room-surface.mp3` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/调查手机.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/调查相框.wav` | 已找到 |

### Phase1 阳台

- 目标场景：`Scenes/Balcony`
- 当前状态：已有基础场景与 2 个交互
- 备注：跳窗后应进入 Phase2 里世界公园。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | `Assets/Arts/阳台/moon terrace.jpg` | 已找到 |
|  | `Assets/Arts/阳台/moon terrace.psb` | 已找到 |
|  | `Assets/Arts/阳台/moonterrace.png` | 已找到 |
|  | `Assets/Arts/阳台/terrace.jpg` | 已找到 |
|  | `Assets/Arts/阳台/terrace.psb` | 已找到 |
|  | `Assets/Arts/阳台/terracepng.png` | 已找到 |
| 动物/角色 | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-LeftEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-RIghtEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eye.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eyePupil.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-face.PNG` | 已找到 |
| 物品/触发点 | H3 阳台角落 | 待场景中确认/摆放 |
|  | 跳窗触发点 | 待场景中确认/摆放 |
| 对白 | `dialogue_h3_balcony_corner` | 已有 DialogueSequence |
|  | `dialogue_balcony_jump` | 已有 DialogueSequence |
| 交互定义 | `Def_BalconyCorner` | 已有 InteractionDef，需挂到场景物体 |
|  | `Def_BalconyJump` | 已有 InteractionDef，需挂到场景物体 |
| 音频 | `Assets/Arts/BGM/BGM_all/进入阳台.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/回头看（H3）.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/检查食槽（H3）.wav` | 已找到 |

### Phase2 里世界公园

- 目标场景：`Scenes/InnerWorld_Park`
- 当前状态：已有场景，交互需补齐/核对
- 备注：StoryPhase 已要求 `Def_DogCollar`?`Def_DogLookBack`?`Def_BuryCollar` 全部完成后进入 Phase3。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | `Assets/Arts/公园/moon park.jpg` | 已找到 |
|  | `Assets/Arts/公园/moon park.psb` | 已找到 |
|  | `Assets/Arts/公园/park.jpg` | 已找到 |
|  | `Assets/Arts/公园/park.psb` | 已找到 |
|  | `Assets/Arts/公园/公园p n g.png` | 已找到 |
| 动物/角色 | `Assets/Arts/动物/狗/Dog-1-light.PNG` | 已找到 |
|  | `Assets/Arts/动物/狗/Dog-1.PNG` | 已找到 |
|  | `Assets/Arts/动物/狗/Dog-2-light.PNG` | 已找到 |
|  | `Assets/Arts/动物/狗/Dog-2.PNG` | 已找到 |
|  | `Assets/Arts/动物/狗/L1项圈狗牌138/IMG_9351(1).PNG` | 已找到 |
|  | `Assets/Arts/动物/狗/光线参考，可能需要两张混用.PNG` | 已找到 |
| 物品/触发点 | L1 项圈狗牌 | 待场景中确认/摆放 |
|  | 流浪狗 | 待场景中确认/摆放 |
|  | 回头看触发点 | 待场景中确认/摆放 |
|  | 安葬项圈触发点 | 待场景中确认/摆放 |
| 对白 | `dialogue_phase2_intro` | 已有 DialogueSequence |
|  | `dialogue_l1_dog_encounter` | 已有 DialogueSequence |
|  | `dialogue_dog_lookback` | 已有 DialogueSequence |
|  | `dialogue_bury_collar` | 已有 DialogueSequence |
| 交互定义 | `Def_DogCollar` | 已有 InteractionDef，需挂到场景物体 |
|  | `Def_DogLookBack` | 已有 InteractionDef，需挂到场景物体 |
|  | `Def_BuryCollar` | 已有 InteractionDef，需挂到场景物体 |
| 音频 | `Assets/Arts/BGM/BGM/park-inner world.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/进入公园wav.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/叼项圈（L1）.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/安葬项圈.wav` | 已找到 |

### Phase3 返回客厅/照片

- 目标场景：`Scenes/OutterWorld`
- 当前状态：复用客厅，需新增 Phase3 显隐状态
- 备注：需区分 Phase1 和 Phase3 客厅的可交互物显隐。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | `Assets/Arts/客厅/Living room items.png` | 已找到 |
|  | `Assets/Arts/客厅/living room.jpg` | 已找到 |
|  | `Assets/Arts/客厅/moon living room.jpg` | 已找到 |
|  | `Assets/Arts/客厅/moon living room.psb` | 已找到 |
|  | `Assets/Arts/客厅拆件/Living room items.png` | 已找到 |
|  | `Assets/Arts/客厅拆件/living room.psb` | 已找到 |
| 动物/角色 | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-LeftEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-RIghtEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eye.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eyePupil.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-face.PNG` | 已找到 |
| 物品/触发点 | 猫碗返回状态 | 待场景中确认/摆放 |
|  | L2 沙发照片 | 待场景中确认/摆放 |
|  | 照片光源解谜入口 | 待场景中确认/摆放 |
| 对白 | `dialogue_phase3_return_home` | 已有 DialogueSequence |
|  | `dialogue_catbowl_return` | 已有 DialogueSequence |
|  | `dialogue_l2_sofa_photo` | 已有 DialogueSequence |
| 交互定义 | `Def_CatBowlReturn` | 已有 InteractionDef，需挂到场景物体 |
|  | `Def_SofaPhoto` | 已有 InteractionDef，需挂到场景物体 |
| 音频 | `Assets/Arts/BGM/BGM/living room-surface.mp3` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/道具拾取放大音效.wav` | 已找到 |

### Phase4 里世界卧室

- 目标场景：`Scenes/Old Home Bedroom`
- 当前状态：已搭建最小触发版；`Old Home Bedroom.unity` 中复制残留客厅 prefab 已隐藏，新增床底背景、橘猫、航空箱标签、日记与 4 个触发区。
- 备注：先跑通触发对白版，再做“航空箱开门/橘猫出现/安葬消散”的显隐或动画。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | `Assets/Arts/卧室/Bedroom.jpg` | 已找到 |
|  | `Assets/Arts/卧室/moon Bedroom.jpg` | 已找到 |
|  | `Assets/Arts/床下/卧室床下.png` | 已找到 |
|  | `Assets/Arts/床下/moon under the bed .jpg` | 已找到 |
|  | `Assets/Arts/床下/moon under the bed .psb` | 已找到 |
|  | `Assets/Arts/床下/under the bed.jpg` | 已找到 |
|  | `Assets/Arts/床下/under the bed.psb` | 已找到 |
| 动物/角色 | `Assets/Arts/动物/橘猫/GingerCat-1.PNG` | 已找到 |
|  | `Assets/Arts/动物/橘猫/GingerCat-2.PNG` | 已找到 |
|  | `Assets/Arts/动物/橘猫/效果参考.PNG` | 已找到 |
|  | `Assets/Arts/动物/橘猫/效果参考1.PNG` | 已找到 |
|  | `Assets/Arts/动物/橘猫/橘猫-1.png` | 已找到 |
|  | `Assets/Arts/动物补充及参考/动物补充及参考/橘猫/橘猫-1.png` | 已找到 |
| 物品/触发点 | L3 航空箱标签 | 已挂 `CrateLabel_Interactable`，使用 `Assets/Arts/物品/L3航空箱标签/label.PNG`，需 Unity 内微调 |
|  | H4 床底存钱罐 | 已挂 `BedCornerSavings_Interactable` 隐形触发区；暂未找到明确存钱罐图，待补/确认 |
|  | L4 墙上日记 | 已挂 `WallDiary_Interactable`，使用 `Assets/Arts/物品/L4日记/diary.psd`，需 Unity 内微调 |
|  | 橘猫安葬点 | 已挂 `BuryOrangeCat_Interactable`，与 `OrangeCat_Phase4_Target` 同位置，需 Unity 内微调 |
| 对白 | `dialogue_phase4_bedroom_intro` | 已有 DialogueSequence |
|  | `dialogue_l3_crate_label` | 已有 DialogueSequence |
|  | `dialogue_h4_savings` | 已有 DialogueSequence |
|  | `dialogue_l4_wall_diary` | 已有 DialogueSequence |
|  | `dialogue_bury_orange_cat` | 已有 DialogueSequence |
| 交互定义 | `Def_CrateLabel` | 已挂到场景，加入 Phase4 RequiredBeats |
|  | `Def_BedCornerSavings` | 已挂到场景，加入 Phase4 RequiredBeats |
|  | `Def_WallDiary` | 已挂到场景，加入 Phase4 RequiredBeats |
|  | `Def_BuryOrangeCat` | 已挂到场景，加入 Phase4 RequiredBeats |
| 音频 | `Assets/Arts/BGM/BGM_all/直接开箱（L3）.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/检查存钱罐.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/橘猫出现.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/安葬橘猫.wav` | 已找到 |

### Phase5 里世界阳台/鹦鹉

- 目标场景：`Scenes/Balcony 2`
- 当前状态：已搭建最小触发版；`Balcony 2.unity` 中旧阳台交互已替换为鸟笼门、食槽/水、羽毛 3 个触发点。
- 备注：真相揭露暂接在 `dialogue_cage_water` 的红色剧情文字里；后续可再做自动黑屏演出。风铃解谜资源在 UI 解谜补充和 `物品/风铃` 中。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | `Assets/Arts/阳台/moon terrace.jpg` | 已找到 |
|  | `Assets/Arts/阳台/moon terrace.psb` | 已找到 |
|  | `Assets/Arts/阳台/moonterrace.png` | 已找到 |
|  | `Assets/Arts/阳台/terrace.jpg` | 已找到 |
|  | `Assets/Arts/阳台/terrace.psb` | 已找到 |
|  | `Assets/Arts/阳台/terracepng.png` | 已找到 |
| 动物/角色 | `Assets/Arts/动物/鸟/Bird-1.PNG` | 已找到 |
| 物品/触发点 | 鸟笼门 | 已挂 `CageDoor_Interactable`，需 Unity 内微调 |
|  | 发绿的水/食槽 | 已挂 `CageWater_Interactable`，需 Unity 内微调 |
|  | L5 鹦鹉羽毛 | 已挂 `ParrotFeather_Interactable`，使用 `Assets/Arts/物品/L5鹦鹉羽毛/feather.PNG`，需 Unity 内微调 |
|  | 风铃解谜 | 待场景中确认/摆放 |
| 对白 | `dialogue_phase5_balcony_intro` | 已有 DialogueSequence |
|  | `dialogue_cage_door` | 已有 DialogueSequence |
|  | `dialogue_cage_water` | 已有 DialogueSequence |
|  | `dialogue_l5_parrot_feather` | 已有 DialogueSequence |
| 交互定义 | `Def_CageDoor` | 已挂到场景，加入 Phase5 RequiredBeats |
|  | `Def_CageWater` | 已挂到场景，加入 Phase5 RequiredBeats |
|  | `Def_ParrotFeather` | 已挂到场景，加入 Phase5 RequiredBeats |
| 音频 | `Assets/Arts/BGM/BGM_all/进入阳台里世界.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/推笼门.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/安葬鹦鹉.wav` | 已找到 |

### Phase6 里世界客厅/真相

- 目标场景：`Scenes/OutterWorld_Living Room 3`
- 当前状态：已搭建最小触发版；新增 `MirrorTouch_Interactable`、`OwnerReveal_Interactable`、`EndingChoice_Interactable`。
- 备注：需测试 `dialogue_ending_choice` 能否跳到 stay / leave；镜子视觉目前借用空相框资源占位，主人坐沙发表现待补美术/摆放。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | `Assets/Arts/客厅/Living room items.png` | 已找到 |
|  | `Assets/Arts/客厅/living room.jpg` | 已找到 |
|  | `Assets/Arts/客厅/moon living room.jpg` | 已找到 |
|  | `Assets/Arts/客厅/moon living room.psb` | 已找到 |
|  | `Assets/Arts/客厅拆件/Living room items.png` | 已找到 |
|  | `Assets/Arts/客厅拆件/living room.psb` | 已找到 |
| 动物/角色 | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-LeftEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-RIghtEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eye.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eyePupil.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-face.PNG` | 已找到 |
| 物品/触发点 | 镜子 | 已挂 `MirrorTouch_Interactable`，需 Unity 内微调 |
|  | 沙发主人/真相触发 | 已挂 `OwnerReveal_Interactable`，需 Unity 内微调 |
|  | 结局选择触发点 | 已挂 `EndingChoice_Interactable`，需 Unity 内微调 |
| 对白 | `dialogue_phase6_mirror_intro` | 已有 DialogueSequence |
|  | `dialogue_mirror_touch` | 已有 DialogueSequence |
|  | `dialogue_owner_reveal` | 已有 DialogueSequence |
|  | `dialogue_ending_choice` | 已有 DialogueSequence |
| 交互定义 | `Def_LivingRoomMirror` | 已挂到场景，加入 Phase6 RequiredBeats |
|  | `Def_SofaOwner` | 已挂到场景，加入 Phase6 RequiredBeats |
|  | `Def_EndingChoice` | 已挂到场景，加入 Phase6 RequiredBeats |
| 音频 | `Assets/Arts/BGM/BGM_all/触摸镜子.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/真相揭露（黑屏）.wav` | 已找到 |
|  | `Assets/Arts/BGM/BGM_all/黑屏转场.wav` | 已找到 |

### Phase7A 后日谈：留下

- 目标场景：`Scenes/Epilogue_Stay`
- 当前状态：目标场景待新建
- 备注：结局专用背景图未明确识别，先标待确认。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | 待确认：结局专用背景图 | 待场景中确认/摆放 |
| 动物/角色 | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-LeftEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-RIghtEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eye.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eyePupil.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-face.PNG` | 已找到 |
| 物品/触发点 | 留下结局画面 | 待场景中确认/摆放 |
|  | 主人抚摸空气 | 待场景中确认/摆放 |
|  | 药瓶被涂黑 | 待场景中确认/摆放 |
| 对白 | `dialogue_epilogue_stay` | 已有 DialogueSequence |
| 交互定义 | `Def_EpilogueStay` | 已有 InteractionDef，需挂到场景物体 |
| 音频 | `Assets/Arts/BGM/BGM/Ending1.mp3` | 已找到 |

### Phase7B 后日谈：离开

- 目标场景：`Scenes/Epilogue_Leave`
- 当前状态：目标场景待新建
- 备注：结局专用背景图未明确识别，先标待确认。

| 类型 | 内容 | 资源/状态 |
| --- | --- | --- |
| 背景 | 待确认：结局专用背景图 | 待场景中确认/摆放 |
| 动物/角色 | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-LeftEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-RIghtEar.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eye.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-eyePupil.PNG` | 已找到 |
|  | `Assets/Arts/黑猫/黑猫/黑猫-头/BlackCat-head-face.PNG` | 已找到 |
| 物品/触发点 | 离开结局画面 | 待场景中确认/摆放 |
|  | 停药扔药瓶 | 待场景中确认/摆放 |
|  | 日记再见小猫 | 待场景中确认/摆放 |
|  | 阳光风铃 | 待场景中确认/摆放 |
| 对白 | `dialogue_epilogue_leave` | 已有 DialogueSequence |
| 交互定义 | `Def_EpilogueLeave` | 已有 InteractionDef，需挂到场景物体 |
| 音频 | `Assets/Arts/BGM/BGM/Ending2.wav` | 已找到 |

## 优先施工顺序建议

1. 以 `OutterWorld.unity` 作为唯一主线客厅；`OutterWorld_1.unity` 暂不维护。
2. 补齐 `InnerWorld_Park` 的狗/项圈/回头看/安葬交互，确保 `DogLookBack` 和 `BuryCollar` 完成后再进入 Phase3。
3. 在 `Old Home Bedroom` 中测试 L3/L4/H4/橘猫安葬四个交互，并微调床底背景、日记、航空箱标签、橘猫位置。
4. 在 `Balcony 2` 中测试鸟笼门、食槽/水、羽毛三个交互，并微调触发范围。
5. 在 `OutterWorld_Living Room 3` 中测试镜子、主人真相和结局选择，并微调触发范围。
6. 最后补 `Epilogue_Stay` / `Epilogue_Leave`；结局图暂缺时先用占位资源跑通。

## 已确认决策与待补项

- 已确认：`OutterWorld.unity` 是主线客厅，`OutterWorld_1.unity` 暂时不用管。
- 已确认：Phase2 需要完成 `DogLookBack` 和 `BuryCollar` 后再进入 Phase3。
- 已确认：结局 A/B 暂时没有专用背景图。
- 已确认：解谜玩法先不接入；当前重点是搭好所有场景、纳入美术资源和音乐资源、构建剧情推进。
- 待补：音效后续接入代码时，建议复制/导出到英文运行时路径。

## 更新记录

- 2026-05-18：首次根据现有 Scene、StoryPhase、InteractionDef、DialogueSequence 和 `Assets/Arts` 生成场景搭建表。
- 2026-05-18：确认 `OutterWorld.unity` 为主线客厅，`OutterWorld_1.unity` 暂不维护；Phase2 需要完成回头看和安葬项圈；结局图暂缺；解谜玩法暂不接入。
- 2026-05-18：补齐 Phase2 公园推进条件：`Def_DogCollar`?`Def_DogLookBack`?`Def_BuryCollar` 均加入 RequiredBeats，并在 `InnerWorld_Park.unity` 挂载 3 个交互触发区。
- 2026-05-18?????? `InnerWorld_Park.unity` ?????????????????????? StoryPhase?GameFlow ???????????????
- 2026-05-18????? StoryPhase ? Addressables `GamePhaseConfig` ????? Phase2 ?????? `Phase3_SurfaceLivingRoom_Photo` ??????
- 2026-05-18?Phase3 ???? `OutterWorld_Living Room 2`??? `Def_SofaPhoto` ? `Def_CatBowlReturn` ??? `Old Home Bedroom`?`OutterWorld_Living Room 1` ??????????
- 2026-05-18：Phase4 旧家卧室改为 `Scenes/Old Home Bedroom`；已新增床底背景、橘猫、航空箱标签、日记和四个交互触发区，并将 `Def_CrateLabel` / `Def_BedCornerSavings` / `Def_WallDiary` / `Def_BuryOrangeCat` 全部加入 `InnerBedroom.asset` 的 RequiredBeats。
- 2026-05-18：Phase5 里世界阳台改为 `Scenes/Balcony 2`；已加入 Addressables，并将 `Def_CageDoor` / `Def_CageWater` / `Def_ParrotFeather` 全部加入 `InnerBalcony.asset` 的 RequiredBeats。
- 2026-05-19：Phase6 镜子/自我真相改为 `Scenes/OutterWorld_Living Room 3`；已加入 Addressables，并在场景内新增镜子、主人真相、结局选择三个交互触发点。
