using Antlr4.Runtime;
using FluentAssertions;

namespace FakeRdb
{
    public sealed class View : List<object[]>{}
    public sealed record Row(Table Table, object[] Data);

    public sealed class Table : List<Row>
    {
        public void Add(object[] oneRow) => Add(new Row(this, oneRow));
    }
    public sealed class UnitTest1
    {
        private readonly FakeDb _db = new();

        [Fact]
        public void Table_Not_Found()
        {
            Assert.Throws<KeyNotFoundException>(
                () => _db.Execute("""
                                 SELECT *
                                 FROM tracks
                                 """)).Message.Should()
                .Be("The given key 'tracks' was not present in the dictionary.");
        }
        [Fact]
        public void Select_EveryColumn()
        {
            _db["tracks"] = new Table
            {
                new object[] { 1 }
            };
            var result = _db.Execute(
                """
                SELECT *
                FROM tracks
                """);
            result.Should().BeEquivalentTo(
                new View
                {
                    new object[] { 1 }
                });
        }


    }
    public static class Ext
    {
        public static View Execute(this FakeDb db, string sql)
        {
            var inputStream = new AntlrInputStream(sql);
            var lexer = new SQLiteLexer(inputStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SQLiteParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new PanicErrorListener());
            var chatContext = parser.sql_stmt_list();
            var visitor = new MyVisitor(db);
            return visitor.Visit(chatContext);
        }
    }
    public class PanicErrorListener : BaseErrorListener
    {
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            throw new InvalidOperationException($"line {line}:{charPositionInLine} {msg}", e);
        }

    }
    public sealed class MyVisitor : SQLiteParserBaseVisitor<View>
    {
        private readonly FakeDb _db;
        protected override View DefaultResult { get; } = new();

        public MyVisitor(FakeDb db)
        {
            _db = db;
        }

        // public override Table VisitSelect_core(SQLiteParser.Select_coreContext context)
        // {
        //     Visit(context.table_or_subquery().Single());
        //     return base.VisitSelect_core(context);
        // }

        public override View VisitTable_or_subquery(SQLiteParser.Table_or_subqueryContext context)
        {
            var tableNameContext = context.table_name().GetText();
            var table = _db[tableNameContext];
            DefaultResult.AddRange(table.Select(r=> r.Data));
            return DefaultResult;
            //return base.VisitTable_or_subquery(context);
        }
    }

    public sealed class FakeDb : Dictionary<string, Table>
    {

        //public Table this[string tableName] =>
    }
}