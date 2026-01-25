namespace WallpaperEngine.Data {
    /// 定义壁纸引擎支持的壁纸类型
    public enum WallpaperTypes {
        /// 场景类型壁纸（通常为3D场景或复杂特效）
        Scene = 0,
        /// 网页类型壁纸（基于HTML/WebGL等技术）
        Web = 1,
        /// 视频类型壁纸（MP4、WebM等视频文件）
        Video = 2,
        /// 应用程序类型壁纸（可执行程序或脚本）
        Application = 3,
        /// 动态壁纸（实时渲染的动态效果）
        Dynamic = 4,
        /// 未知或未分类类型
        Unknown = 99
    }
}
