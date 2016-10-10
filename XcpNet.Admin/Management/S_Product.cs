using System;
using System.Web;
using System.Collections.Generic;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using M = Cnaws.Product.Modules;
using Cnaws.Data.Query;
using U = Cnaws.Passport.Modules;
using C = Cnaws.Comment.Modules;
using System.Collections.Specialized;
using Cnaws;
using Cnaws.Web.Templates;
using System.Text;

namespace XcpNet.Admin.Management
{
    public sealed class S_Product : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        private Dictionary<int, string> _menus;

        public S_Product()
        {
            _menus = new Dictionary<int, string>();
            _menus.Add(0, "所有商品");
            _menus.Add(1, "待上架商品");
            _menus.Add(2, "已上架商品");
            _menus.Add(3, "首页推荐商品");
            _menus.Add(4, "分类推荐商品");
            _menus.Add(5, "申请团购商品");
            _menus.Add(6, "团购商品");
            _menus.Add(7, "仓库中的商品");
            _menus.Add(8, "回收站");
        }

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Admin"; }
        }

        public void Index(int id = 0, long sid = 0, int type = 0)
        {
            if (CheckRight())
            {
                if (CheckPost("s_product", () =>
                {
                    StringBuilder sb = new StringBuilder();
                    IList<M.ProductCategory> parents = M.ProductCategory.GetAllParentsById(DataSource, id);
                    if (parents.Count > 0)
                    {
                        for (int i = 0; i < parents.Count; ++i)
                        {
                            if (i > 0) sb.Append("&nbsp;&nbsp;");
                            sb.Append(parents[i].Name);
                        }
                    }
                    else
                    {
                        sb.Append("所有分类");
                    }
                    this["Id"] = id;
                    this["SId"] = sid;
                    this["Type"] = type;
                    this["Parents"] = sb.ToString();
                    this["AllCategory"] = M.ProductCategory.GetAll(DataSource, -1);
                    this["Menus"] = _menus;
                    this["GetMenuName"] = new FuncHandler((args) =>
                    {
                        try { return _menus[(int)args[0]]; }
                        catch (Exception) { }
                        return _menus[0];
                    });
                }))
                    NotFound();
            }
        }
        public void List(int categoryId, long sid = 0, int type = 0, int page = 1, string q = "_")
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    long count;
                    DbWhereQueue dw = null;

                    if (categoryId > 0)
                    {
                        dw &= new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual);

                        int categorylevel = M.ProductCategory.GetAllParentsById(DataSource, categoryId).Count;
                        if (categorylevel == 3)
                            dw &= new DbWhere<M.Product>("CategoryId", categoryId);
                        else if (categorylevel == 2)
                            dw &= (new DbWhere<M.Product>("CategoryId", categoryId) | new DbWhere<M.Product>("CategoryId").InSelect<M.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result());
                        else if (categorylevel == 1)
                            dw &= (new DbWhere<M.Product>("CategoryId", categoryId) | new DbWhere<M.Product>("CategoryId").InSelect<M.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result() |
                                new DbWhere<M.Product>("CategoryId").InSelect<M.ProductCategory>(new DbSelect("Id")).
                                Where(new DbWhere("ParentId").InSelect<M.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result()).Result());
                    }

                    if (sid > 0)
                        dw &= new DbWhere<M.Product>("SupplierId", sid);

                    switch (type)
                    {
                        case 0:
                            dw &= new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual);
                            break;
                        case 1:
                            dw &= new DbWhere<M.Product>("State", M.ProductState.BeforeSale);
                            break;
                        case 2:
                            dw &= new DbWhere<M.Product>("State", M.ProductState.Sale);
                            break;
                        case 3:
                            dw &= new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.Product>("Recommend", true);
                            break;
                        case 4:
                            dw &= new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.Product>("CategoryBest", 0, DbWhereType.GreaterThan);
                            break;
                        case 5:
                            dw &= new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.Product>("DiscountState", M.DiscountState.Approval);
                            break;
                        case 6:
                            dw &= new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.Product>("DiscountState", M.DiscountState.Activated);
                            break;
                        case 7:
                            dw &= new DbWhere<M.Product>("State", M.ProductState.Saved);
                            break;
                        case 8:
                            dw &= new DbWhere<M.Product>("State", M.ProductState.Deleted);
                            break;
                    }

                    if ("_".Equals(q))
                        q = null;
                    if (!string.IsNullOrEmpty(q))
                        dw &= new DbWhere<M.Product>("Title", HttpUtility.UrlDecode(q), DbWhereType.Like);

                    dw &= new DbWhere<M.Product>("ParentId", 0) & new DbWhere<M.Product>("ProductType", 1);
                    
                    IList<DataJoin<M.Product, U.Member>> list = Db<M.Product>.Query(DataSource)
                        .Select(new DbSelect<M.Product>(), new DbSelect<U.Member>(), new DbSelectAs<M.Supplier>("Company"))
                        .LeftJoin(new DbColumn<M.Product>("SupplierId"), new DbColumn<U.Member>("Id"))
                        .LeftJoin(new DbColumn<M.Product>("SupplierId"), new DbColumn<M.Supplier>("UserId"))
                        .Where(dw)
                        .OrderBy(new DbOrderBy<M.Product>("SortNum", DbOrderByType.Desc), new DbOrderBy<M.Product>("Id", DbOrderByType.Desc))
                        .ToList<DataJoin<M.Product, U.Member>>(10, page, out count);

                    SetResult(new SplitPageData<DataJoin<M.Product, U.Member>>(page, 10, list, count, 11));
                }

            }
        }

        //public void AllCategory()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            IList<M.ProductCategory> list = M.ProductCategory.GetAll(DataSource, -1);
        //            if (IsPost)
        //                SetResult(list);
        //            else
        //                SetJavascript("allCategory", list);
        //        }
        //    }
        //}

        //public void Categories(int parentId)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //            SetResult(M.ProductCategory.GetAll(DataSource, parentId));
        //    }
        //}

        //public void Add()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                SetResult(M.Product.Insert(DataSource,
        //                    Request.Form["Title"],
        //                    Request.Form["Image"],
        //                    Request.Form["Content"],
        //                    int.Parse(Request.Form["Category"]),
        //                    DateTime.Parse(Request.Form["CreationDate"]),
        //                    Request.Form["Keywords"],
        //                    Request.Form["Description"],
        //                    "on".Equals(Request.Form["Visibility"]),
        //                    "on".Equals(Request.Form["Top"]),
        //                    int.Parse(Request.Form["Style"]),
        //                    Request.Form["Color"],
        //                    Request.Form["Author"],
        //                    Request.Form["Referer"]), () =>
        //                    {
        //                        WritePostLog("ADD");
        //                    });
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        //public void Update()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                M.Product value = DbTable.Load<M.Product>(Request.Form);

        //                if (Db<M.Product>.Query(DataSource).Update().Set("CategoryId", value.CategoryId).Set("CategoryBest", value.CategoryBest).Set("SortNum", value.SortNum).Set("Recommend", value.Recommend).Where(new DbWhere("ParentId", value.Id) | new DbWhere("Id", value.Id)).Execute() > 0)
        //                {
        //                    SetResult(true, () =>
        //                    {
        //                        WritePostLog("MOD");
        //                    });
        //                }
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        //public void State()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                M.Product value = DbTable.Load<M.Product>(Request.Form);
        //                if (value.State == M.ProductState.Sale)
        //                    value.SaleTime = DateTime.Now;

        //                if (Db<M.Product>.Query(DataSource).Update().Set("State", value.State).Set("SaleTime", value.SaleTime).Where(new DbWhere("ParentId", value.Id) | new DbWhere("Id", value.Id)).Execute() > 0)
        //                {
        //                    SetResult(true, () =>
        //                         {
        //                             WritePostLog("MOD");
        //                         });
        //                }
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        //public void DState()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                M.Product value = DbTable.Load<M.Product>(Request.Form);
        //                if (Db<M.Product>.Query(DataSource).Update().Set("DiscountState", value.DiscountState).Where(new DbWhere("ParentId", value.Id) | new DbWhere("Id", value.Id)).Execute() > 0)
        //                {
        //                    SetResult(true, () =>
        //                    {
        //                        WritePostLog("MOD");
        //                    });
        //                }
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        public void Mod()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.Product value = new M.Product()
                        {
                            Id = long.Parse(Request.Form["a_Id"]),
                            State = (M.ProductState)Enum.Parse(typeof(M.ProductState), Request.Form["a_State"]),
                            DiscountState = (M.DiscountState)Enum.Parse(typeof(M.DiscountState), Request.Form["a_DiscountState"]),
                            Recommend = Types.GetBooleanFromString(Request.Form["a_Recommend"]),
                            CategoryBest = int.Parse(Request.Form["a_CategoryBest"]),
                            CostPrice = Money.Parse(Request.Form["a_CostPrice"]),
                            CountyPrice = Money.Parse(Request.Form["a_CountyPrice"]),
                            DotPrice = Money.Parse(Request.Form["a_DotPrice"]),
                            Price = Money.Parse(Request.Form["a_Price"]),
                            SortNum = int.Parse(Request.Form["a_SortNum"])
                        };
                        List<DataColumn> columns = new List<DataColumn>();
                        M.Product old = M.Product.GetById(DataSource, value.Id);
                        if (value.State != old.State)
                        {
                            if (value.State == M.ProductState.Sale)
                            {
                                value.SaleTime = DateTime.Now;
                                columns.Add("SaleTime");
                            }
                            columns.Add("State");
                        }
                        if (value.DiscountState != old.DiscountState)
                            columns.Add("DiscountState");
                        if (value.Recommend != old.Recommend)
                            columns.Add("Recommend");
                        if (value.CategoryBest != old.CategoryBest)
                            columns.Add("CategoryBest");
                        if (value.CostPrice != old.CostPrice)
                            columns.Add("CostPrice");
                        if (value.CountyPrice != old.CountyPrice)
                            columns.Add("CountyPrice");
                        if (value.DotPrice != old.DotPrice)
                            columns.Add("DotPrice");
                        if (value.Price != old.Price)
                            columns.Add("Price");
                        if (value.SortNum != old.SortNum)
                            columns.Add("SortNum");
                        if (columns.Count > 0)
                        {
                            SetResult(value.Update(DataSource, ColumnMode.Include, columns.ToArray(), new DataWhere("Id", value.Id) | new DataWhere("ParentId", value.Id)), () =>
                                 {
                                     WritePostLog("MOD");
                                 });
                        }
                        else
                        {
                            SetResult(true);
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        public void Del()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.Product value = new M.Product()
                        {
                            Id = long.Parse(Request.Form["a_Id"]),
                            State = M.ProductState.Deleted
                        };
                        SetResult(value.Update(DataSource, ColumnMode.Include, new DataColumn[] { "State" }, new DataWhere("Id", value.Id) | new DataWhere("ParentId", value.Id)), () =>
                            {
                                WritePostLog("DEL");
                            });
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        public void Remove()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        long[] ids = Array.ConvertAll(Request.Form["a_Id"].Split(','), new Converter<string, long>((x) => long.Parse(x)));
                        SetResult(Db<M.Product>.Query(DataSource).Update().Set("State", M.ProductState.Deleted).Where(new DbWhere("ParentId", ids, DbWhereType.In) | new DbWhere("Id", ids, DbWhereType.In)).Execute() > 0, () =>
                        {
                            WritePostLog("DEL");
                        });
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        //public void Get(int id)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //            SetResult(M.Product.GetById(DataSource, id));
        //    }
        //}
        //public void Parents(int id)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            List<int> list = new List<int>();
        //            M.ProductCategory ac = M.ProductCategory.GetById(DataSource, id);
        //            if (ac == null)
        //                ac = new M.ProductCategory();
        //            list.Add(ac.Id);
        //            while (ac.ParentId != 0)
        //            {
        //                ac = M.ProductCategory.GetById(DataSource, ac.ParentId);
        //                list.Insert(0, ac.Id);
        //            }
        //            SetResult(list.ToArray());
        //        }
        //    }
        //}

        //public void GetAllKeyWord()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            SetResult(C.CommentKeyword.GetHotKeyWord(DataSource));
        //        }
        //    }
        //}

        //public void SetComment()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            U.Member member = U.Member.Get(DataSource, Request.Form["UserName"]);
        //            if (member != null)
        //            {
        //                DataSource.Begin();
        //                try
        //                {
        //                    C.Comment comment = DbTable.Load<C.Comment>(Request.Form);

        //                    M.Product p = M.Product.GetById(DataSource, comment.TargetId);
        //                    if (p.ParentId != 0)
        //                        comment.TargetId = p.ParentId;
        //                    comment.UserId = member.Id;
        //                    comment.TargetType = 1;
        //                    comment.CreationDate = DateTime.Now;
        //                    if (comment.Insert(DataSource) != DataStatus.Success)
        //                    {
        //                        throw new Exception();
        //                    }
        //                    if (!string.IsNullOrEmpty(Request.Form["KeyWords"]))
        //                    {
        //                        string[] KeyWords = Request.Form["KeyWords"].Split('|');
        //                        foreach (string keyword in KeyWords)
        //                        {
        //                            if ((new C.CommentKeyword() { Id = comment.Id, Keyword = keyword }).Insert(DataSource) != DataStatus.Success)
        //                            {
        //                                throw new Exception();
        //                            }
        //                        }
        //                    }
        //                    if (!string.IsNullOrEmpty(Request.Form["Images"]))
        //                    {
        //                        string[] Images = HttpUtility.UrlDecode(Request.Form["Images"]).Split('|');
        //                        foreach (string image in Images)
        //                        {
        //                            if ((new C.CommentImage() { Id = comment.Id, Image = image }).Insert(DataSource) != DataStatus.Success)
        //                            {
        //                                throw new Exception();
        //                            }
        //                        }
        //                    }

        //                    DataSource.Commit();
        //                    SetResult(true);
        //                }
        //                catch
        //                {
        //                    DataSource.Rollback();
        //                    SetResult(false);
        //                }
        //            }
        //            else
        //            {
        //                SetResult(-1);
        //            }

        //        }
        //    }
        //}

    }
}
