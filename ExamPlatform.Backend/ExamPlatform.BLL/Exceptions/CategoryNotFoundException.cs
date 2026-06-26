namespace ExamPlatform.BLL.Exceptions
{
    public class CategoryNotFoundException : BusinessException
    {
        public CategoryNotFoundException() : base("Category not found.") { }
        public CategoryNotFoundException(int catId) : base($"Category with id {catId} not found.") { }
    }
}