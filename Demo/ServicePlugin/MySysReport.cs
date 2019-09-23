using Kingdee.BOS.Contracts.Report;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicePlugin
{
    /// <summary>
    /// 自定义简单账表：服务端插件
    /// </summary>
    /// <remarks>
    /// 开发目的：用以学习简单账表插件的各种事件、属性
    /// 
    /// 案例：
    /// 按日期，搜索采购订单，
    /// 输出：编号、状态、物料、数量、单位、单价、价税合计
    /// 可以对物料汇总；
    /// 默认按编号排序
    /// 数量、价税合计需要合计
    /// 数量、单价、价税合计，需控制精度
    /// 相同的采购订单行，编号不重复显示
    /// </remarks>
    [Description("自定义简单账表")]
    public class MySysReport : SysReportBaseService
    {
        public override void Initialize()    //事件 1:初始化 包含账表类型,报表名称
        {
            base.Initialize();//调用基类方法
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;//


        }
    }
}
