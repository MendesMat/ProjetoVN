namespace ProjetoVN.Inventory
{
    public sealed class Item
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }

        public Item(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}
