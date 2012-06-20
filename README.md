Viteyka.ORM
--------------------------------
Viteyka.ORM is a micro ORM library. It supports such simple operations like:
1) fetch object by identifier, 
2) select and map collection of objects from table using predicate and simple paging capabilities,
3) CRUD ops,
4) join 2 tables, but map only one of them. 

How do I get started?
--------------------------------
First of all build a mapper instance (it's a heavy operation) and better save it to the cache:

	var storeMapper = Mapping<Store>
        .Create("Stores", "st")
        .PrimaryKey(it => it.Id)
        .Identity(it => it.Id)
        .ReadOnly(it => it.CreatedAt)
        .DoMap(it => it.CommentsCount, "(select count(1) from StoreComments sc where sc.StoreId = st.[Id])")
        .ReadOnly(it => it.CommentsCount)
        .Build();

Next create a SQL connection and use it for all operations with mapper:

	using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["connection"].ConnectionString))
        {
            conn.Open();

            var allStores = storeMapper.All(conn);
            var cnt = storeMapper.Count(conn);
            var cnt2 = storeMapper.Count(conn, it => it.CategoryId == 2);
            var store = storeMapper.GetById(conn, 831);
            var stores2 = storeMapper.Where(conn, (it => it.CategoryId == 5), (it => it.Title), 20, 30);
            var storesProc = storeMapper.ExecProc(conn, "GetStores", new { categoryId = 2, titleInclude = "bla-bla" });
        }

Viteyka.ORM is Copyright &copy; 2012 [Viktor Kozynskyi](mailto://viktor.kozynskyi@gmail.com) and other contributors under the [MIT license](LICENSE.txt).