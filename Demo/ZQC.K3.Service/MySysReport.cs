using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZQC.K3.Service
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


        public override void Initialize()    //事件 1:修改账表属性中的报表名称、明细表属性、替代显示列信息、字段精度控制信息等
        {
            base.Initialize();//调用基类方法
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;//通过报表属性调用报表类型 简单账表类型：普通、树形、分页
            //报表名称
            this.ReportProperty.ReportName = new LocaleValue("日采购报表", base.Context.UserLocale.LCID);
            //
            this.IsCreateTempTableByPlugin = true;//通过插件创建临时表单
            //
            this.ReportProperty.IsGroupSummary = true;//是否分组合计
            //
            this.ReportProperty.SimpleAllCols = false;

            //单据主键:两个FID相同,则为同一条单据的两条分录,需要单据编号不重复显示
            this.ReportProperty.PrimaryKeyFieldName = "FID";
            //
            this.ReportProperty.IsDefaultOnlyDspSumAndDetailData = true;

            //报表主键字段名:默认为FIDENTITYID,可以修改
            //this.ReportProperty.IdentityFieldName = "FIDENTITYID";

            ///设置精度控制  小数控制
            List<DecimalControlField> list = new List<DecimalControlField>();
            DecimalControlField dcf = new DecimalControlField();
            //数量
            dcf.ByDecimalControlFieldName = "FQty";
            dcf.DecimalControlFieldName = "FUnitPrecision";
            list.Add(dcf);
            //单价
            dcf.ByDecimalControlFieldName = "FTAXPRICE";
            dcf.DecimalControlFieldName = "FPRICEDIGITS";
            list.Add(dcf);
            //金额
            dcf.ByDecimalControlFieldName = "FALLAMOUNT";
            dcf.DecimalControlFieldName = "FAMOUNTDIGITS";
            list.Add(dcf);
            this.ReportProperty.DecimalControlFieldList = list;
        }

        ///事件 2:获取表名
        public override string GetTableName()
        {

            return base.GetTableName();
        }

        ///事件 3: 向报表临时表，插入报表数据   把账表取数结果放到上一步创建的临时表中

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)//获取字段 和 表名
        {
            base.BuilderReportSqlAndTempTable(filter, tableName);
            //拼接过滤条件:filter

            //默认排序字段:需要从filter中取用户设置的排序字段  seq为顺序排序
            string seqFId = string.Format(base.KSQL_SEQ, "t0.FID");


            //取数SQL FID, FEntryId, 编号、状态、物料、数量、单位、单位精度、单价、价税合计
            string sql = string.Format(@"/*dialect*/
                                  select t0.FID,t1.FENTRYID,
                                        ,t0.FBILLNO
                                        ,t0.FDate
                                        ,t0.FDOCUMENTSTATUS
                                        ,t2.FLOCALCURRID
                                        ,ISNULL(t20.FPRICEDIGITS,4) AS FPRICEDIGITS
                                        ,ISNULL(t20.FAMOUNTDIGITS,2) AS FAMOUNTDIGITS
                                        ,t1.FMATERIALID
                                        ,t1M_L.FNAME as FMaterialName
                                        ,t1.FQTY
                                        ,t1u.FPRECISION as FUnitPrecision
                                        ,t1U_L.FNAME as FUnitName
                                        ,t1f.FTAXPRICE
                                        ,t1f.FALLAMOUNT
                                        ,{0}
                                     into {1}
                                   from T_PUR_POORDER t0   
                                  inner join T_PUR_POORDERFIN t2 on (t0.FID = t2.FID)
                                   left join T_BD_CURRENCY t20 on (t2.FLOCALCURRID = t20.FCURRENCYID)
                                  inner join T_PUR_POORDERENTRY t1 on (t0.FID = t1.FID)
                                   left join T_BD_MATERIAL_L t1M_L on (t1.FMATERIALID = t1m_l.FMATERIALID and t1M_L.FLOCALEID = 2052)
                                  inner join T_PUR_POORDERENTRY_F t1F on (t1.FENTRYID = t1f.FENTRYID)
                                   left join T_BD_UNIT t1U on (t1f.FPRICEUNITID = t1u.FUNITID)
                                   left join T_BD_UNIT_L t1U_L on (t1U.FUNITID = t1U_L.FUNITID and t1U_L.FLOCALEID = 2052) ",
                                                    seqFId,tableName);
            DBUtils.ExecuteDynamicObject(this.Context, sql);
        }
        /// 事件 4:此方法由插件平台报表基类已实现，插件根据索引情况可以自行决定是否重写 创建账表临时表索引sql
        protected override string GetIdentityFieldIndexSQL(string tableName)
        {
            return base.GetIdentityFieldIndexSQL(tableName);
        }

        /// 事件 5: 执行sql指令 不用关注
        protected override void ExecuteBatch(List<string> listSql)
        {
            base.ExecuteBatch(listSql);
        }


        /// <summary>
        /// 构建出报表列
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <remarks>
        /// // 如下代码，演示如何设置同一分组的分组头字段合并
        /// // 需配合Initialize事件，设置分组依据字段(PrimaryKeyFieldName)
        /// ReportHeader header = new ReportHeader();
        /// header.Mergeable = true;
        /// int width = 80;
        /// ListHeader headChild1 = header.AddChild("FBILLNO", new LocaleValue("应付单号"));
        /// headChild1.Width = width;
        /// headChild1.Mergeable = true;
        ///             
        /// ListHeader headChild2 = header.AddChild("FPURMAN", new LocaleValue("采购员"));
        /// headChild2.Width = width;
        /// headChild2.Mergeable = true;
        /// </remarks>

        //事件 6:设置账表列头字段信息
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            //FID,FEntryId,编号,状态,物料,数量,单位,单位精度,单价,价税合计

            ReportHeader header = new ReportHeader();
            var status=header.AddChild("FDocumentStatus", new LocaleValue("状态"));
            status.ColIndex = 0;

            var billNo=header.AddChild("FBillNo", new LocaleValue("单据编号"));
            billNo.ColIndex = 1;
            billNo.IsHyperlink = true;//支持超链接

            var material= header.AddChild("FMaterialName", new LocaleValue("物料"));
            material.ColIndex = 2;

            var qty = header.AddChild("FQty", new LocaleValue("数量"), SqlStorageType.SqlDecimal); //SqlStorageType.SqlDecimal 是精度控制 在初始化中已经进行了定义
            qty.ColIndex = 3;

            var unit = header.AddChild("FUnitName", new LocaleValue("单位"));
            unit.ColIndex = 4;

            var price = header.AddChild("FTAXPRICE", new LocaleValue("含税价"), SqlStorageType.SqlDecimal);
            price.ColIndex = 5;

            var amount = header.AddChild("FALLAMOUNT", new LocaleValue("价税合计"), SqlStorageType.SqlDecimal);
            amount.ColIndex = 6;
            return header;
        }
    }
}

