using Admin.NET.Core;
using Furion;

namespace Admin.NET.Application
{
    /// <summary>
    /// 当前用户
    /// </summary>
    public static class CurrentUserInfo
    {
        /// <summary>
        /// 用户id
        /// </summary>
        public static long UserId => long.Parse(App.User.FindFirst(ClaimConst.CLAINM_USERID)?.Value);

        /// <summary>
        /// 账号
        /// </summary>
        public static string Account => App.User.FindFirst(ClaimConst.CLAINM_ACCOUNT)?.Value;

        /// <summary>
        /// 昵称
        /// </summary>
        public static string Name => App.User.FindFirst(ClaimConst.CLAINM_NAME)?.Value;

        /// <summary>
        /// 是否超级管理员
        /// </summary>
        public static bool IsSuperAdmin => App.User.FindFirst(ClaimConst.CLAINM_SUPERADMIN)?.Value == ((int)AdminType.SuperAdmin).ToString();
    }
}