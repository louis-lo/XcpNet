using System;
using Cnaws.Data;
using P = Cnaws.Product.Modules;
using Cnaws.Web.Templates;
using M = Cnaws.Passport.Modules;
using Cnaws.Data.Query;
using System.Collections.Generic;
using D = XcpNet.Supplier.Modules.Modules;
using Cnaws.Web;
using Cnaws;

namespace XcpNet.Supplier.Controllers
{
    public class Programme : SupplierController
    {
        [Distributor]
        public void List(int type = -1, int categoryid = 0, int page = 0)
        {
            string title = Request["keyword"];
            this["Indutry"] = D.IndutryCategory.GetAll(DataSource, 0);
            this["ProgrammeList"] = D.DistributorProgramme.GetListbyDistributor(DataSource, User.Identity.Id, Math.Max(0, categoryid), title, (D.DistributorProgramme.EProgrammeType)type, Distributor.Province, Distributor.City, Distributor.County, Math.Max(1, page), 10, 8);
            this["Type"] = type;
            this["KeyWord"] = title;
            this["Category"] = categoryid;
            Render("programme_list.html");
        }
        [Distributor]
        public void Edit(long id = 0, int state = -1, int page = 1)
        {
            D.DistributorProgramme programme = D.DistributorProgramme.GetById(DataSource, id, User.Identity.Id);
            if (id > 0 && programme == null)
            {
                NotFound();
            }
            else
            {
                if (programme == null) programme = new Modules.Modules.DistributorProgramme();
                this["Programme"] = programme;
                this["State"] = state;
                this["Id"] = id;
                this["Indutry"] = D.IndutryCategory.GetAll(DataSource, 0);
                this["ProgrammeProduct"] = D.ProgrammeProductMapping.GetAllPageById(DataSource, id, state, Math.Max(1, page), 10);
                Render("programme_edit.html");
            }
        }
        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void Submit()
        {
            try
            {
                D.DistributorProgramme programme = DbTable.Load<D.DistributorProgramme>(Request.Form);
                programme.County = Distributor.County;
                programme.City = Distributor.City;
                programme.Province = Distributor.Province;
                programme.DistributorId = User.Identity.Id;
                if (programme.Id > 0)
                    SetResult((int)programme.Update(DataSource), programme.Id);
                else
                    SetResult((int)programme.Insert(DataSource), programme.Id);
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void Delete(long id)
        {
            SetResult(D.DistributorProgramme.DelByDistributor(DataSource, id, User.Identity.Id));
        }

        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void EditCount(long id, long productid)
        {
            SetResult(D.ProgrammeProductMapping.UpdataCount(DataSource, id, productid, int.Parse(Request["Count"])));
        }
        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void DelProduct(long id)
        {
            DataSource.Begin();
            try
            {
                string[] productids = Request.Form["ProductId"].Split(',');
                for (int i = 0; i < productids.Length; i++)
                {
                    if (D.ProgrammeProductMapping.Del(DataSource, id, long.Parse(productids[i])) != DataStatus.Success)
                        throw new Exception();
                    if (D.DistributorProgramme.UpdataCount(DataSource, id, -1) != DataStatus.Success)
                        throw new Exception();
                }
                DataSource.Commit();
                SetResult(true);
            }
            catch (Exception)
            {
                DataSource.Rollback();
                SetResult(false);
            }
        }

        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void AddProductToProgramme(long id)
        {
            DataSource.Begin();
            try
            {
                long productid = long.Parse(Request.Form["ProductId"]);
                int count = int.Parse(Request.Form["Count"]);
                if(D.ProgrammeProductMapping.Add(DataSource, id, productid, count) != DataStatus.Success)
                    throw new Exception();
                if (D.DistributorProgramme.UpdataCount(DataSource, id, 1) != DataStatus.Success)
                    throw new Exception();
                DataSource.Commit();
                SetResult(true);
            }
            catch (Exception)
            {
                DataSource.Rollback();
                SetResult(false);
            }
        }

        public void AddProduct(long id, int categoryId = 0, int page = 0)
        {
            if (id > 0)
            {
                string keyword = "";
                keyword = Request["KeyWord"];
                DbWhereQueue where = new DbWhereQueue();
                where = (new DbWhere<D.DistributorProduct>("State", P.ProductState.Sale) | new DbWhere<D.DistributorProduct>("State", P.ProductState.BeforeSaved));
                where &= (new DbWhere<D.DistributorSalesArea>("Province", Distributor.Province) | new DbWhere<D.DistributorSalesArea>("Province", 0));
                where &= (new DbWhere<D.DistributorSalesArea>("City", Distributor.City) | new DbWhere<D.DistributorSalesArea>("City", 0));
                where &= (new DbWhere<D.DistributorSalesArea>("County", Distributor.County) | new DbWhere<D.DistributorSalesArea>("County", 0));
                if (categoryId != 0)
                {
                    IList<D.DistributorCategory> cates = D.DistributorCategory.GetAllParentsById(DataSource, categoryId);
                    if (cates.Count == 3)
                        where &= new DbWhere<D.DistributorProduct>("CategoryId", categoryId);
                    else if (cates.Count == 2)
                        where &= (new DbWhere<D.DistributorProduct>("CategoryId", categoryId) | new DbWhere<D.DistributorProduct>("CategoryId").InSelect<D.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result());
                    else if (cates.Count == 1)
                        where &= (new DbWhere<D.DistributorProduct>("CategoryId", categoryId) | new DbWhere<D.DistributorProduct>("CategoryId").InSelect<D.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result() | new DbWhere<D.DistributorProduct>("CategoryId").InSelect<D.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId").InSelect<D.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result()).Result());
                }

                where &= new DbWhere<D.DistributorProduct>("ParentId", 0);
                if (!string.IsNullOrEmpty(keyword) && keyword != "_")
                    where = new DbWhere<D.DistributorProduct>("Title", keyword, DbWhereType.Like);
                long count;
                IList<dynamic> list;

                DbWhereQueue areawhere = new DbWhereQueue();
                areawhere = new DbWhere<D.DistributorAreaMapping>("Province", Distributor.Province);
                areawhere &= new DbWhere<D.DistributorAreaMapping>("City", Distributor.City);
                areawhere &= new DbWhere<D.DistributorAreaMapping>("County", Distributor.County);
                areawhere &= new DbWhere<D.DistributorAreaMapping>("Saled", false, DbWhereType.NotEqual);

                list = Db<D.DistributorProduct>.Query(DataSource)
                 .Select(new DbSelect<D.DistributorProduct>(), new DbSelect<M.Member>("Name"), new DbSelect<D.DistributorCategory>("Name"), new DbSelect<D.DistributorAreaMapping>("Saled"))
                 .InnerJoin(new DbColumn<D.DistributorProduct>("SupplierId"), new DbColumn<M.Member>("Id"))
                 .InnerJoin(new DbColumn<D.DistributorProduct>("CategoryId"), new DbColumn<D.DistributorCategory>("Id"))
                 .LeftJoin(new DbColumn<D.DistributorProduct>("Id"), new DbColumn<D.DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                 .InnerJoin(new DbColumn<D.DistributorProduct>("Id"), new DbColumn<D.DistributorSalesArea>("ProductId"))
                 .Where(where)
                 .OrderBy(new DbOrderBy<D.DistributorProduct>("CreationDate", DbOrderByType.Desc))
                 .ToList(10, Math.Max(1, page), out count);

                this["ProgrammeId"] = id;
                this["CategoryId"] = categoryId;
                this["ProductList"] = new SplitPageData<dynamic>(Math.Max(1, page), 20, list, count, 8);
                this["KeyWord"] = keyword;
                this["CategoryList"] = D.DistributorCategory.GetAll(DataSource, 0);
                this["GetImage"] = new FuncHandler((args) =>
                {
                    return Convert.ToString(args[0]).Split('|')[0];
                });
                this["ExistsProgramme"] = new FuncHandler((args) =>
                {
                    return D.ProgrammeProductMapping.ExistsParent(DataSource, id, Convert.ToInt64(args[0]));
                });
                Render("programme_addproduct.html");
                //Render("distributorproduct_" + state + ".html");
                //this["ProductList"]=D.DistributorProduct.get
            }
            else
            {
                NotFound();
            }

        }

        [HttpAjax]
        public void ProductInfo(long id, long productid)
        {
            this["ProductList"] = D.DistributorProduct.GetByParentId(DataSource, productid);
            this["ExistsProgramme"] = new FuncHandler((args) =>
            {
                return D.ProgrammeProductMapping.Exists(DataSource, id, Convert.ToInt64(args[0]));
            });
            Render("programme_productinfo.html");
        }

    }
}
