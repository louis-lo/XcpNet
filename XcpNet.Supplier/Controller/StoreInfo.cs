using Cnaws.Data;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using P = Cnaws.Product.Modules;
using A = XcpNet.Ad.Modules;
using System.Text.RegularExpressions;
using Cnaws;
using System.Collections;

namespace XcpNet.Supplier.Controllers
{
    public class StoreInfo : SupplierController
    {
        [SupplierOrDistributor(true)]
        public void Index()
        {
            this["StoreInfo"] = P.StoreInfo.GetStoreInfoByUserId(DataSource, User.Identity.Id);
            if (IsSupplier())
            {
                Render("supplier/store_info.html");
            }
            else
            {
                Render("store_info.html");
            }
        }

        public void Submit()
        {
            P.StoreInfo model = DbTable.Load<P.StoreInfo>(Request.Form);
            if (model != null)
            {
                model.UserId = User.Identity.Id;
                SetResult(P.StoreInfo.Update(DataSource, model));
            }
            else
            {
                SetResult(DataStatus.Failed);
            }
        }

        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void AdManager(int page = 1)
        {
            this["AdList"] = A.Advertisement.GetAllByUser(DataSource, User.Identity.Id);
            Render("admanager.html");
        }

        public void GetAdType()
        {
            if (IsAjax)
            {
                SetResult(A.AdType.GetAll(DataSource, (int)A.AdType.EAdType.TownshipStore));
            }
        }

        public void InsertAd()
        {
            if (IsPost)
            {
                A.Advertisement adv = DbTable.Load<A.Advertisement>(Request.Form);
                adv.UserId = User.Identity.Id;
                SetResult(adv.Insert(DataSource));
            }
        }
        public void EditAd()
        {
            if (IsPost)
            {
                A.Advertisement adv = DbTable.Load<A.Advertisement>(Request.Form);
                adv.UserId = User.Identity.Id;
                SetResult(adv.Update(DataSource, ColumnMode.Include, "Title", "LabelId", "Url", "ImgUrl", "Content", "IsEnable", "Height", "Width"));
            }
        }

        public void GetAdInfo(int id)
        {
            if (IsAjax)
            {
                SetResult(A.Advertisement.GetById(DataSource, id));
            }
        }

        public void DelAd(int id)
        {
            if (IsPost)
            {
                SetResult(new A.Advertisement() { Id = id }.Delete(DataSource));
            }
        }

        [SupplierOrDistributor]
        public void Serie()
        {
            this["SerieList"] = P.StoreSerie.GetByUser(DataSource, User.Identity.Id);
            if (IsSupplier())
                Render("supplier/store_serie.html");
            else
                Render("store_serie.html");
        }
        [SupplierOrDistributor]
        public void Attribute(long id = 0)
        {
            P.StoreSerie serie;
            if (id == 0)
                serie = new P.StoreSerie();
            else
                serie = P.StoreSerie.GetById(DataSource, id);
            this["Serie"] = serie;
            if (IsSupplier())
                Render("supplier/store_attribute.html");
            else
                Render("store_attribute.html");
        }
        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void AttrSubmit(int id = 0)
        {
            int code = -200;
            DataSource.Begin();
            try
            {
                P.StoreSerie SerieTemplate = DbTable.Load<P.StoreSerie>(Request.Form);
                SerieTemplate.CreationDate = DateTime.Now;
                SerieTemplate.UserId = User.Identity.Id;
                SerieTemplate.Id = id;
                if (SerieTemplate.Id <= 0)
                {
                    if (SerieTemplate.Insert(DataSource) != DataStatus.Success)
                    {
                        code = Common.CommUtility.PROGRAM_ERROR;
                        throw new AggregateException();
                    }
                }
                else
                {
                    if (SerieTemplate.ModByIdAndUserId(DataSource) != DataStatus.Success)
                    {
                        code = Common.CommUtility.PROGRAM_ERROR;
                        throw new AggregateException();
                    }
                }
                Regex reg = new Regex(@"SerieName_\d*");
                List<string> Keys = Request.Form.AllKeys.Where(name => reg.IsMatch(name)).ToList();
                foreach (string key in Keys)
                {
                    int attrid = 0;
                    string inputId = key.Replace("SerieName_", "");
                    int.TryParse(inputId, out attrid);
                    if (attrid != 0)
                    {
                        P.StoreAttribute attribute;
                        if (attrid < 0)
                        {
                            attribute = new P.StoreAttribute();
                        }
                        else
                        {
                            attribute = P.StoreAttribute.GetById(DataSource, attrid);
                        }
                        attribute.SerieId = SerieTemplate.Id;
                        attribute.SerieName = Request.Form["SerieName_" + attrid];
                        if (string.IsNullOrEmpty(attribute.SerieName))
                            break;
                        attribute.AttributorValue = Request.Form["AttrValue_" + attrid].Replace(",,", ",").Replace(",,", ",").Trim(',');
                        if (P.StoreAttribute.Exists(DataSource, attribute.Id, attribute.SerieId, attribute.SerieName))
                        {
                            code = Common.CommUtility.EXISTS;
                            throw new AggregateException();

                        }
                        string[] AttrValues = attribute.AttributorValue.Split(',');
                        Hashtable ht = new Hashtable();
                        for (int i = 0; i < AttrValues.Length; i++)
                        {
                            if (ht.Contains(AttrValues[i]))
                            {
                                code = Common.CommUtility.EXISTS;
                                throw new AggregateException();
                            }
                            else
                            {
                                ht.Add(AttrValues[i], AttrValues[i]);
                            }
                        }

                        if (attribute.InsertOrUpdate(DataSource) != DataStatus.Success)
                        {
                            code = Common.CommUtility.PROGRAM_ERROR;
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        code = Common.CommUtility.PROGRAM_ERROR;
                        throw new AggregateException();
                    }
                }
                DataSource.Commit();
                SetResult(true);
            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                SetResult(code);
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                SetResult(false, ex.ToString());
            };

        }

        public void DelSerie(int id)
        {
            SetResult(P.StoreSerie.DelbyId(DataSource, id));
        }

        public void DelAttribute(int id)
        {
            SetResult(P.StoreAttribute.DelbyId(DataSource, id));
        }
        [HttpAjax]
        [SupplierOrDistributor]
        public void GetAttributeByAjax(long id)
        {
            this["AttrList"] = P.StoreAttribute.GetBySerieId(DataSource, id);
            if (IsSupplier())
                Render("supplier/store_serie.html");
            else
                Render("store_serie.html");
        }
    }
}
