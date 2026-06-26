namespace ExamPlatform.API.DTOs.Question
{
    // One entry in QuestionStatsDto.ByCategory — question count for a single category.
    public class CategoryQuestionCountDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
