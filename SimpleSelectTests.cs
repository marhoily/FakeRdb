
namespace FakeRdb
{
    public sealed class SimpleSelectTests : ComparisonTests
    {
        [Fact]
        public void Table_Not_Found()
        {
            CompareReaders("SELECT * FROM Album");
        }
        
        [Fact]
        public void Select_EveryColumn()
        {
            Prototype.Seed3Albums();
            Sut.Seed3Albums();
            CompareReaders("SELECT * FROM Album");
        }
    }
}