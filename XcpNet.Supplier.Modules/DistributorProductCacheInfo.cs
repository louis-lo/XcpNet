using Cnaws.Data;
using System;
using System.Collections.Generic;

namespace XcpNet.Supplier.Modules
{
    [Serializable]
    public sealed class DistributorProductCacheInfo
    {
        public string Title = null;
        public string Image = null;
        public string Content = null;
        public string BarCode = null;
        public string Unit = null;
        public Dictionary<string, string> Series = new Dictionary<string, string>();
        public Dictionary<string, string> Attributes = new Dictionary<string, string>();

        public DistributorProductCacheInfo()
        { 

        }
        public DistributorProductCacheInfo(DataSource ds, Modules.DistributorProduct p)
        {
            Title = p.Title;
            Image = p.Image;
            Content = p.Content;
            BarCode = p.BarCode;
            Unit = p.Unit;

            IList<Modules.DistributorMapping> series = Modules.DistributorMapping.GetAllByProduct(ds, p.Id);
            foreach (Modules.DistributorMapping item in series)
                Series.Add(item.Name, item.Value);

            IList<DataJoin<Modules.DistributorAttribute, Modules.DistributorAttributeMapping>> attrs = Modules.DistributorAttribute.GetAllValuesByProduct(ds, p.Id);
            foreach (DataJoin<Modules.DistributorAttribute, Modules.DistributorAttributeMapping> item in attrs)
                Attributes.Add(item.A.Name, item.B.Value);
        }
    }
}
