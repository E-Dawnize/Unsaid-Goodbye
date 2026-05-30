namespace Gameplay.SceneFlow
{
    public enum GamePhase
    {
        None = 0,

        // === 第一阶段：表世界客厅 ===
        Phase1_SurfaceLivingRoom_Initial = 1,      // 醒来，靠近猫碗触发
        Phase2_SurfacePark = 2,                    // 表世界公园：叼项圈，安葬
        Phase2_InnerPark = 3,                      // 里世界公园：回头看脚印与狗肉车
        Phase3_SurfaceLivingRoom_Photo = 4,        // 发现照片L2，猫碗返回
        Phase4_SurfaceBedroom = 5,                 // 表世界卧室/床底：航空箱L3，存钱罐H4，安葬橘猫
        Phase4_InnerBedroomUnderBed = 6,           // 里世界床底：阅读日记L4
        Phase3_SurfaceBedroomTransition = 12,      // 表世界卧室：进入床底前过渡
        Phase4_InnerBedroomMirror = 7,             // 里世界卧室：镜子里少了什么
        Phase5_InnerBalcony = 8,                   // 鹦鹉记忆，鸟笼与羽毛
        Phase6_InnerLivingRoom_Mirror = 9,         // 镜子无倒影，揭示真相，结局判定

        // === 后日谈 ===
        Phase7_Epilogue_A = 10,                    // 结局A：留下
        Phase7_Epilogue_B = 11                     // 结局B：离开
    }
}
