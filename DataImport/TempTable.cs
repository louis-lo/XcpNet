using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws;

namespace DataImport
{
    [Serializable]
    [DataTable("Sheet1")]
    public sealed class TempTable : LongIdentityModule
    {
        public string 商品名称 = null;
        public string 属性 = null;
        public string 规格 = null;
        public string 商品编码 = null;
        public Money 成本价 = 0;
        public Money 订货价 = 0;
        public Money 销售价 = 0;
        public int 库存 = 0;
        public string 商品组 = null;
        public string 品牌 = null;
        public string 供应商编码 = null;
        public string 供应商 = null;
    }
}
