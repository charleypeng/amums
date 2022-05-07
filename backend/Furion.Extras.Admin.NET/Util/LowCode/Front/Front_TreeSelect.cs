﻿using Furion.DatabaseAccessor;
using Furion.Extras.Admin.NET.Util.LowCode.Front.Att;
using Furion.Extras.Admin.NET.Util.LowCode.Front.Interface;
using Furion.Extras.Admin.NET.Util.LowCode.Front.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Furion.Extras.Admin.NET.Util.LowCode.Front
{
    /// <summary>
    /// 树选择器
    /// </summary>
    [FrontType("treeSelect")]
    [FrontTypeBindDatabase(DbProvider.SqlServer, typeof(string), "nvarchar(2000)")]
    [FrontTypeBindDatabase(DbProvider.SqlServer, typeof(string), "nvarchar(2000)", Suffix = "FullName")]
    public class Front_TreeSelect : Front_Base<Front_TreeSelect_Options>, IFrontDynamic
    {
    }

    public class Front_TreeSelect_Options : IFrontDynamicOptions
    {
        /// <summary>
        /// 动态数据
        /// </summary>
        public string DynamicKey { get; set; }

        /// <summary>
        /// 是否是动态数据
        /// </summary>
        public bool Dynamic { get; set; }

        /// <summary>
        /// 选项配置
        /// </summary>
        public Front_Tree_Option[] Options { get; set; }

        /// <summary>
        /// 禁用
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// 隐藏
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// 多选
        /// </summary>
        public bool Multiple { get; set; }

        /// <summary>
        /// 可清除
        /// </summary>
        public bool Clearable { get; set; }

        /// <summary>
        /// 可搜索
        /// </summary>
        public bool ShowSearch { get; set; }

        /// <summary>
        /// 可勾选
        /// </summary>
        public bool TreeCheckable { get; set; }

        /// <summary>
        /// 占位内容
        /// </summary>
        public string Placeholder { get; set; }
    }
}
