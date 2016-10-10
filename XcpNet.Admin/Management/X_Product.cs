using Cnaws.Data.Query;
using Cnaws.Management;
using System;
using System.Collections.Generic;
using M = Cnaws.Product.Modules;
using U = Cnaws.Passport.Modules;
using C = Cnaws.Comment.Modules;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Data;
using System.Web;
using Cnaws.Web;

namespace XcpNet.Admin.Management
{
    public sealed class X_Product : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Admin"; }
        }
        public void Index()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (CheckPost("x_product"))
                        NotFound();
                }
            }
        }
        public void List(int categoryId, string q = "_", int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    int categorylevel = M.ProductCategory.GetAllParentsById(DataSource, categoryId).Count;
                    long count;
                    DbWhereQueue dw = null;
                    if (categoryId > 0)
                    {
                        dw = new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual);

                        if (categorylevel == 3)
                            dw &= new DbWhere<M.Product>("CategoryId", categoryId);
                        else if (categorylevel == 2)
                            dw &= (new DbWhere<M.Product>("CategoryId", categoryId) | new DbWhere<M.Product>("CategoryId").InSelect<M.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result());
                        else if (categorylevel == 1)
                            dw &= (new DbWhere<M.Product>("CategoryId", categoryId) | new DbWhere<M.Product>("CategoryId").InSelect<M.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result() |
                                new DbWhere<M.Product>("CategoryId").InSelect<M.ProductCategory>(new DbSelect("Id")).
                                Where(new DbWhere("ParentId").InSelect<M.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result()).Result());
                    }
                    else if (categoryId == 0)
                        dw = new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual);
                    else if (categoryId == -1)
                        dw = new DbWhere<M.Product>("State", M.ProductState.BeforeSale);
                    else if (categoryId == -2)
                        dw = new DbWhere<M.Product>("State", M.ProductState.BeforeSaved);
                    else if (categoryId == -3)
                        dw = new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.Product>("Recommend", true);
                    else if (categoryId == -4)
                        dw = new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.Product>("CategoryBest", 0, DbWhereType.GreaterThan);
                    else if (categoryId == -5)
                        dw = new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.Product>("DiscountState", M.DiscountState.Approval);
                    else if (categoryId == -6)
                        dw = new DbWhere<M.Product>("State", M.ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.Product>("DiscountState", M.DiscountState.Activated);
                    else if (categoryId == -7)
                        dw = new DbWhere<M.Product>("State", M.ProductState.Saved);
                    else if (categoryId == -8)
                        dw = new DbWhere<M.Product>("State", M.ProductState.Deleted);
                    if (!string.IsNullOrEmpty(q) && !"_".Equals(q))
                        dw &= new DbWhere<M.Product>("Title", HttpUtility.UrlDecode(q), DbWhereType.Like);
                    dw &= new DbWhere<M.Product>("ParentId", 0)& new DbWhere<M.Product>("ProductType", 2);
                    IList<DataJoin<M.Product, U.Member>> list = Db<M.Product>.Query(DataSource)
                        .Select(new DbSelect<M.Product>(), new DbSelect<U.Member>())
                        .InnerJoin(new DbColumn<M.Product>("SupplierId"), new DbColumn<U.Member>("Id"))
                        .Where(dw)
                        .OrderBy(new DbOrderBy<M.Product>("SortNum", DbOrderByType.Desc), new DbOrderBy<M.Product>("Id", DbOrderByType.Desc))
                        .ToList<DataJoin<M.Product, U.Member>>(10, page, out count);

                    SetResult(new SplitPageData<DataJoin<M.Product, U.Member>>(page, 10, list, count, 11));
                }

            }
        }

        public void AllCategory()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    IList<M.ProductCategory> list = M.ProductCategory.GetAll(DataSource, -1);
                    if (IsPost)
                        SetResult(list);
                    else
                        SetJavascript("allCategory", list);
                }
            }
        }

        public void Categories(int parentId)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                    SetResult(M.ProductCategory.GetAll(DataSource, parentId));
            }
        }
        public void Update()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.Product value = DbTable.Load<M.Product>(Request.Form);

                        if (Db<M.Product>.Query(DataSource).Update().Set("CategoryId", value.CategoryId).Set("CategoryBest", value.CategoryBest).Set("SortNum", value.SortNum).Set("Recommend", value.Recommend).Where(new DbWhere("ParentId", value.Id) | new DbWhere("Id", value.Id)).Execute() > 0)
                        {
                            SetResult(true, () =>
                            {
                                WritePostLog("MOD");
                            });
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        public void State()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.Product value = DbTable.Load<M.Product>(Request.Form);
                        if (value.State == M.ProductState.Sale)
                            value.SaleTime = DateTime.Now;

                        if (Db<M.Product>.Query(DataSource).Update().Set("State", value.State).Set("SaleTime", value.SaleTime).Where(new DbWhere("ParentId", value.Id) | new DbWhere("Id", value.Id)).Execute() > 0)
                        {
                            SetResult(true, () =>
                            {
                                WritePostLog("MOD");
                            });
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        public void DState()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.Product value = DbTable.Load<M.Product>(Request.Form);
                        if (Db<M.Product>.Query(DataSource).Update().Set("DiscountState", value.DiscountState).Where(new DbWhere("ParentId", value.Id) | new DbWhere("Id", value.Id)).Execute() > 0)
                        {
                            SetResult(true, () =>
                            {
                                WritePostLog("MOD");
                            });
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        public void Get(int id)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                    SetResult(M.Product.GetById(DataSource, id));
            }
        }
        public void Parents(int id)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    List<int> list = new List<int>();
                    M.ProductCategory ac = M.ProductCategory.GetById(DataSource, id);
                    if (ac == null)
                        ac = new M.ProductCategory();
                    list.Add(ac.Id);
                    while (ac.ParentId != 0)
                    {
                        ac = M.ProductCategory.GetById(DataSource, ac.ParentId);
                        list.Insert(0, ac.Id);
                    }
                    SetResult(list.ToArray());
                }
            }
        }

        public void GetAllKeyWord()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    SetResult(C.CommentKeyword.GetHotKeyWord(DataSource));
                }
            }
        }

        public void SetComment()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    U.Member member = U.Member.Get(DataSource, Request.Form["UserName"]);
                    if (member != null)
                    {
                        DataSource.Begin();
                        try
                        {
                            C.Comment comment = DbTable.Load<C.Comment>(Request.Form);

                            M.Product p = M.Product.GetById(DataSource, comment.TargetId);
                            if (p.ParentId != 0)
                                comment.TargetId = p.ParentId;
                            comment.UserId = member.Id;
                            comment.TargetType = 1;
                            comment.CreationDate = DateTime.Now;
                            if (comment.Insert(DataSource) != DataStatus.Success)
                            {
                                throw new Exception();
                            }
                            if (!string.IsNullOrEmpty(Request.Form["KeyWords"]))
                            {
                                string[] KeyWords = Request.Form["KeyWords"].Split('|');
                                foreach (string keyword in KeyWords)
                                {
                                    if ((new C.CommentKeyword() { Id = comment.Id, Keyword = keyword }).Insert(DataSource) != DataStatus.Success)
                                    {
                                        throw new Exception();
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(Request.Form["Images"]))
                            {
                                string[] Images = HttpUtility.UrlDecode(Request.Form["Images"]).Split('|');
                                foreach (string image in Images)
                                {
                                    if ((new C.CommentImage() { Id = comment.Id, Image = image }).Insert(DataSource) != DataStatus.Success)
                                    {
                                        throw new Exception();
                                    }
                                }
                            }

                            DataSource.Commit();
                            SetResult(true);
                        }
                        catch
                        {
                            DataSource.Rollback();
                            SetResult(false);
                        }
                    }
                    else
                    {
                        SetResult(-1);
                    }

                }
            }
        }

    }
}
