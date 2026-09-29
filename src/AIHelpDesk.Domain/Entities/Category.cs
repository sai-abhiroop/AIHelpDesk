namespace AIHelpDesk.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public bool IsActive { get; private set; }

        private Category()
        {

        }

        public Category(string name,string description)
        {
            Id = Guid.NewGuid();

            Name = name;
            Description = description;

            IsActive = true;
        }

        public void Update(string name,string description)
        {
            Name = name;
            Description = description;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }
    }
}
