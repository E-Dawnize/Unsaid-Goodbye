namespace Gameplay.SceneFlow
{
    public enum GamePhase
    {
        None = 0,

        // === 第一阶段：表世界客厅 ===
        Phase1_SurfaceLivingRoom_Initial,      // 醒来，靠近猫碗触发
        Phase1_SurfaceLivingRoom_Investigate,  // 调查阶段：药瓶/手机/相框

        // === 第二阶段：里世界·公园 ===
        Phase2_InnerPark,                      // 遇见流浪狗，叼项圈L1，安葬

        // === 第三阶段：表世界·照片 ===
        Phase3_SurfaceLivingRoom_Photo,        // 发现照片L2，光源解谜

        // === 第四阶段：里世界·卧室 ===
        Phase4_InnerBedroom,                   // 航空箱L3，日记L4，安葬橘猫

        // === 第五阶段：里世界·阳台 ===
        Phase5_InnerBalcony,                   // 风铃解谜，鹦鹉消散

        // === 第六阶段：里世界·客厅（真相）===
        Phase6_InnerLivingRoom_Mirror,         // 镜子无倒影，揭示真相，结局判定

        // === 后日谈 ===
        Phase7_Epilogue_A,                     // 结局A：留下
        Phase7_Epilogue_B                      // 结局B：离开
    }
}