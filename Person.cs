
namespace Nuri.MongoDB.Transactions
{
    public class Person
    {
        public int Id;
        public string Name;
        public int ToolCount;

        public override string ToString() => $"[{this.Id} name {this.Name} tool count {this.ToolCount}]";
    }
}
